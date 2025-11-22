using System.Collections;
using UnityEngine;

public class FloatingSnapTrigger : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Object that will be floated and snapped.")]
    [SerializeField] private Transform targetObject;
    [Tooltip("Transform that stores the final position/rotation.")]
    [SerializeField] private Transform destinationPose;

    [Header("Motion")]
    [Tooltip("Seconds it takes to travel from current pose to destination.")]
    [SerializeField] private float moveDuration = 1f;
    [Tooltip("Height of the floating arc during movement.")]
    [SerializeField] private float arcHeight = 0.3f;
    [Tooltip("Trigger can only be used once unless this is unchecked.")]
    [SerializeField] private bool triggerOnce = true;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    Coroutine moveRoutine;
    bool hasTriggered;
    Vector3 initialPosition;
    Quaternion initialRotation;
    bool isInitialized;

    void Start()
    {
        if (targetObject != null)
        {
            initialPosition = targetObject.position;
            initialRotation = targetObject.rotation;
            isInitialized = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce && hasTriggered) return;

        if (!targetObject || !destinationPose)
        {
            Debug.LogWarning("FloatingSnapTrigger missing refs", this);
            return;
        }

        if (!isInitialized)
        {
            initialPosition = targetObject.position;
            initialRotation = targetObject.rotation;
            isInitialized = true;
        }

        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveRoutine(true));
        hasTriggered = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce) return;

        if (!targetObject || !isInitialized)
        {
            return;
        }

        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveRoutine(false));
    }

    IEnumerator MoveRoutine(bool toDestination)
    {
        Vector3 startPos = targetObject.position;
        Quaternion startRot = targetObject.rotation;
        Vector3 endPos = toDestination ? destinationPose.position : initialPosition;
        Quaternion endRot = toDestination ? destinationPose.rotation : initialRotation;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveDuration > 0f ? Mathf.Clamp01(elapsed / moveDuration) : 1f;
            float easedT = EaseInOut(t);

            // Use sine wave with eased timing so arc matches the movement speed
            float arcT = Mathf.Sin(easedT * Mathf.PI);
            Vector3 arcOffset = Vector3.up * (arcT * arcHeight);
            targetObject.position = Vector3.Lerp(startPos, endPos, easedT) + arcOffset;
            targetObject.rotation = Quaternion.Slerp(startRot, endRot, easedT);

            yield return null;
        }

        targetObject.position = endPos;
        targetObject.rotation = endRot;
        moveRoutine = null;
    }

    static float EaseInOut(float t)
    {
        // Ease-in-out: starts slow, fast in middle, ends slow
        return t < 0.5f
            ? 2f * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }
}
