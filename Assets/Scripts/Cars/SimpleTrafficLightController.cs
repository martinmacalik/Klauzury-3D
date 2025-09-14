using UnityEngine;
using System.Collections.Generic;

public class SimpleTrafficLightController : MonoBehaviour
{
    [Header("Approaches (2–4)")]
    public Transform[] lights;                // place at stop points
    [Tooltip("Same length as 'lights'. True = STOP leg (may go on red if green is clear long enough). False = normal traffic-light leg.")]
    public bool[] stopOnly;

    [Header("Cycle")]
    public float cycleTime = 5f;
    public Color redColor = Color.red;
    public Color greenColor = Color.green;

    [Header("Stopping / Occupancy")]
    [Tooltip("Cars are considered 'at' an approach if within this radius of its Transform. Also used to detect cars on the GREEN approach.")]
    public float stopRadius = 6f;
    [Tooltip("After the green leg becomes empty or switches, STOP legs still wait this long before going.")]
    public float graceAfterGreenClear = 3f;

    [Tooltip("Layer(s) containing car colliders. Put your cars on this layer.")]
    public LayerMask carLayer = ~0; // default: everything

    [Header("Gizmos")]
    public float gizmoLightSize = 0.5f;
    public bool drawStopSpheres = true;

    private int currentIndex = 0;
    private float timer = 0f;
    private float lastGreenOccupiedTime = Mathf.NegativeInfinity;

    // Registry
    public static readonly List<SimpleTrafficLightController> All = new List<SimpleTrafficLightController>();
    void OnEnable()  { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        if (stopOnly == null || stopOnly.Length != (lights?.Length ?? 0))
            stopOnly = new bool[lights?.Length ?? 0];
        UpdateLights();
        lastGreenOccupiedTime = Time.time; // start grace at scene load
    }

    void Update()
    {
        if (lights == null || lights.Length == 0) return;

        // Cycle
        timer += Time.deltaTime;
        if (timer >= cycleTime)
        {
            timer = 0f;
            currentIndex = (currentIndex + 1) % lights.Length;
            UpdateLights();
            lastGreenOccupiedTime = Time.time; // grace starts on every switch
        }

        // GREEN OCCUPANCY (uses stopRadius)
        if (IsValidIndex(currentIndex))
        {
            const int Max = 16;
            Collider[] hits = new Collider[Max];
            int count = Physics.OverlapSphereNonAlloc(
                lights[currentIndex].position,
                stopRadius,
                hits,
                carLayer,
                QueryTriggerInteraction.Collide
            );
            if (count > 0)
            {
                lastGreenOccupiedTime = Time.time; // refresh grace while occupied
            }
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

    /// Returns true if THIS controller wants the car to stop.
    public bool ShouldStopForCar(Transform carTransform, float currentSpeed, float brakeAccel)
    {
        if (lights == null || lights.Length == 0) return false;

        // Which approach are we at? (closest within stopRadius)
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

        if (atIdx < 0) return false;           // not at this junction
        if (atIdx == currentIndex) return false; // green for this approach -> go

        bool isStopLeg = (stopOnly != null && atIdx < stopOnly.Length) ? stopOnly[atIdx] : false;

        if (!isStopLeg)
        {
            // Normal traffic-light leg → obey red
            float stoppingDistance = (currentSpeed * currentSpeed) / (2f * Mathf.Max(0.01f, brakeAccel)) + 1f;
            return bestDist <= Mathf.Max(stopRadius, stoppingDistance);
        }

        // STOP leg logic with grace
        bool withinGrace = (Time.time - lastGreenOccupiedTime) <= graceAfterGreenClear;

        // Must stop if green was occupied recently
        if (withinGrace) return true;

        // Otherwise (green empty for longer than grace), STOP leg may go
        return false;
    }

    private bool IsValidIndex(int i) => lights != null && i >= 0 && i < lights.Length && lights[i] != null;

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
