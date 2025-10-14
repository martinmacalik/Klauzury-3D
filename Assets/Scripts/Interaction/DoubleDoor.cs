using UnityEngine;

public class DoubleDoor : MonoBehaviour
{
    [Header("Door Leaves (hinge pivots)")]
    public Transform leftDoor;   // pivot at left hinge
    public Transform rightDoor;  // pivot at right hinge

    [Header("Open Angles (relative to closed)")]
    public float leftOpenAngle = -90f;   // left swings negative Y by default
    public float rightOpenAngle = 90f;   // right swings positive Y by default

    [Header("Motion")]
    public float openSpeed = 6f;         // higher = snappier
    public float autoCloseDelay = 0f;    // 0 = don't auto close

    [Header("Interaction")]
    public bool isLocked = false;

    Quaternion _leftClosed, _rightClosed;
    Quaternion _leftOpen, _rightOpen;
    bool _isOpen = false;
    float _autoCloseTimer = 0f;

    void Awake()
    {
        if (leftDoor == null || rightDoor == null)
        {
            Debug.LogError("[DoubleDoor] Assign both door leaf transforms.");
            enabled = false; return;
        }

        _leftClosed  = leftDoor.localRotation;
        _rightClosed = rightDoor.localRotation;

        // Build target open rotations relative to CLOSED pose
        _leftOpen  = _leftClosed  * Quaternion.Euler(0f, leftOpenAngle, 0f);
        _rightOpen = _rightClosed * Quaternion.Euler(0f, rightOpenAngle, 0f);
    }

    void Update()
    {
        // Smoothly move toward target pose
        var t = Time.deltaTime * openSpeed;
        if (_isOpen)
        {
            leftDoor.localRotation  = Quaternion.Slerp(leftDoor.localRotation,  _leftOpen,  t);
            rightDoor.localRotation = Quaternion.Slerp(rightDoor.localRotation, _rightOpen, t);

            if (autoCloseDelay > 0f)
            {
                _autoCloseTimer += Time.deltaTime;
                if (_autoCloseTimer >= autoCloseDelay)
                {
                    _autoCloseTimer = 0f;
                    _isOpen = false; // start closing
                }
            }
        }
        else
        {
            leftDoor.localRotation  = Quaternion.Slerp(leftDoor.localRotation,  _leftClosed,  t);
            rightDoor.localRotation = Quaternion.Slerp(rightDoor.localRotation, _rightClosed, t);
        }
    }

    public void Toggle()
    {
        if (isLocked) return;
        _isOpen = !_isOpen;
        if (_isOpen) _autoCloseTimer = 0f;
    }

    // Optional helpers
    public void Open()  { if (!isLocked) { _isOpen = true;  _autoCloseTimer = 0f; } }
    public void Close() { _isOpen = false; }
    public bool IsOpen => _isOpen;
}
