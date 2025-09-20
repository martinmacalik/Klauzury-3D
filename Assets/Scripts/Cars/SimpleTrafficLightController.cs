using UnityEngine;
using System.Collections.Generic;

public class SimpleTrafficLightController : MonoBehaviour
{
    [Header("Approaches (2–4)")]
    public Transform[] lights;                // place at stop points
    public bool[] stopOnly;                   // true = STOP leg; false = normal traffic-light leg

    [Header("Cycle")]
    public float cycleTime = 5f;
    public Color redColor = Color.red;
    public Color greenColor = Color.green;

    [Header("Stopping / Occupancy")]
    public float stopRadius = 6f;             // also used to detect cars on approaches
    public float graceAfterGreenClear = 3f;   // used only when there are normal TL legs
    public LayerMask carLayer = ~0;

    [Header("Gizmos")]
    public float gizmoLightSize = 0.5f;
    public bool drawStopSpheres = true;

    private int currentIndex = 0;
    private float timer = 0f;
    private float lastGreenOccupiedTime = Mathf.NegativeInfinity;

    public static readonly List<SimpleTrafficLightController> All = new List<SimpleTrafficLightController>();
    void OnEnable()  { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        if (stopOnly == null || stopOnly.Length != (lights?.Length ?? 0))
            stopOnly = new bool[lights?.Length ?? 0];
        UpdateLights();
        lastGreenOccupiedTime = Time.time;
    }

    void Update()
    {
        if (lights == null || lights.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= cycleTime)
        {
            timer = 0f;
            currentIndex = (currentIndex + 1) % lights.Length;
            UpdateLights();
            lastGreenOccupiedTime = Time.time; // start/refresh grace on switch
        }

        // Track occupancy of the current green (for grace when TL legs exist)
        if (HasAnyTrafficLightLeg() && IsValidIndex(currentIndex) && IsApproachOccupied(currentIndex, null))
        {
            lastGreenOccupiedTime = Time.time;
        }
    }

    void UpdateLights()
    {
        for (int i = 0; i < (lights?.Length ?? 0); i++)
        {
            if (!lights[i]) continue;
            var r = lights[i].GetComponent<Renderer>();
            if (r) r.material.color = (i == currentIndex) ? greenColor : redColor;
        }
    }

    // ---------- NEW/UPDATED LOGIC BELOW ----------

    public bool ShouldStopForCar(Transform carTransform, float currentSpeed, float brakeAccel)
    {
        if (lights == null || lights.Length == 0) return false;

        // Which approach is the car at (within stopRadius)?
        int atIdx = -1;
        float bestDist = float.PositiveInfinity;
        Vector3 carPos = carTransform.position; carPos.y = 0f;

        for (int i = 0; i < lights.Length; i++)
        {
            var t = lights[i];
            if (!t) continue;
            Vector3 p = t.position; p.y = 0f;
            float d = Vector3.Distance(carPos, p);
            if (d <= stopRadius && d < bestDist)
            {
                bestDist = d;
                atIdx = i;
            }
        }

        if (atIdx < 0) return false;            // not at this junction

        bool anyTrafficLightLeg = HasAnyTrafficLightLeg();

        if (!anyTrafficLightLeg)
        {
            // -------- PURE STOP JUNCTION --------
            int occupied = CountOccupiedApproaches(carTransform);

            if (occupied <= 1)
                return false;                   // alone here -> go

            // 2+ approaches occupied -> use cycle to arbitrate
            return atIdx != currentIndex;       // stop unless it's your cycle turn
        }

        // -------- MIXED JUNCTION (STOP + TL) --------
        if (atIdx == currentIndex) return false; // green for this approach -> go

        bool isStopLeg = (stopOnly != null && atIdx < stopOnly.Length) ? stopOnly[atIdx] : false;

        if (!isStopLeg)
        {
            // Normal traffic-light leg → obey red with a bit of stopping-distance leniency
            float stoppingDistance = (currentSpeed * currentSpeed) / (2f * Mathf.Max(0.01f, brakeAccel)) + 1f;
            return bestDist <= Mathf.Max(stopRadius, stoppingDistance);
        }
        else
        {
            // STOP leg rules:
            // 1) If the GREEN is occupied now OR within grace → STOP.
            bool greenOccupiedNow   = IsValidIndex(currentIndex) && IsApproachOccupied(currentIndex, carTransform);
            bool withinGraceWindow  = (Time.time - lastGreenOccupiedTime) <= graceAfterGreenClear;
            if (greenOccupiedNow || withinGraceWindow)
                return true;

            // 2) If multiple approaches are present now → cycle arbitrates.
            int occupied = CountOccupiedApproaches(carTransform);
            if (occupied >= 2)
                return atIdx != currentIndex;

            // 3) Otherwise you're alone → go.
            return false;
        }
    }

    bool HasAnyTrafficLightLeg()
    {
        if (stopOnly == null || lights == null) return false;
        for (int i = 0; i < lights.Length && i < stopOnly.Length; i++)
            if (lights[i] && !stopOnly[i]) return true;
        return false;
    }

    bool IsApproachOccupied(int idx, Transform selfToIgnore)
    {
        if (!IsValidIndex(idx)) return false;

        const int Max = 16;
        Collider[] hits = new Collider[Max];
        int count = Physics.OverlapSphereNonAlloc(
            lights[idx].position, stopRadius, hits, carLayer, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            var h = hits[i];
            if (!h) continue;

            // ignore self if provided
            if (selfToIgnore && (h.transform == selfToIgnore || h.transform.IsChildOf(selfToIgnore)))
                continue;

            if (h.GetComponentInParent<CarAIDriver>()) // only count actual cars
                return true;
        }
        return false;
    }

    int CountOccupiedApproaches(Transform selfToIgnore)
    {
        int occupied = 0;
        if (lights == null) return 0;
        for (int i = 0; i < lights.Length; i++)
        {
            if (!lights[i]) continue;
            if (IsApproachOccupied(i, selfToIgnore))
                occupied++;
        }
        return occupied;
    }

    private bool IsValidIndex(int i) =>
        lights != null && i >= 0 && i < lights.Length && lights[i] != null;
    
    public bool IsCarAtThisJunction(Transform carTransform)
    {
        if (lights == null) return false;
        Vector3 carPos = carTransform.position; carPos.y = 0f;
        for (int i = 0; i < lights.Length; i++)
        {
            if (!lights[i]) continue;
            Vector3 p = lights[i].position; p.y = 0f;
            if (Vector3.Distance(carPos, p) <= stopRadius) return true;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        if (lights == null) return;

        for (int i = 0; i < lights.Length; i++)
        {
            var t = lights[i];
            if (!t) continue;

            bool isStopLeg = (stopOnly != null && i < stopOnly.Length) ? stopOnly[i] : false;
            Color bulb = (Application.isPlaying && i == currentIndex)
                ? greenColor
                : (isStopLeg ? new Color(1f, 0.5f, 0f) : redColor);

            Gizmos.color = bulb;
            Gizmos.DrawSphere(t.position, gizmoLightSize);

            if (drawStopSpheres)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.18f);
                Gizmos.DrawSphere(t.position, stopRadius);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(t.position, stopRadius);
            }
        }
    }
}
