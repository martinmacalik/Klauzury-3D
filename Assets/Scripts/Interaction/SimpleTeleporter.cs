using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class SimpleTeleporter : MonoBehaviour
{
    [Header("Teleporter")]
    [Tooltip("Where the player will be moved to (position + rotation).")]
    public Transform destination;

    [Tooltip("Leave empty to auto-detect a Transform on the object tagged 'Player'.")]
    public Transform player;

    [Header("Prompt (TMP)")]
    [Tooltip("TMP text used as the 'Press E to enter' prompt.")]
    public TMP_Text promptText;
    [Tooltip("What the prompt should say.")]
    public string promptMessage = "Press E to enter";
    [Tooltip("Key used to activate the teleporter.")]
    public KeyCode activateKey = KeyCode.E;

    bool playerInside;

    void Reset()
    {
        // Make the collider a trigger by default
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void Awake()
    {
        if (promptText != null)
        {
            promptText.text = promptMessage;
            promptText.gameObject.SetActive(false);
        }

        if (player == null)
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged) player = tagged.transform;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsThePlayer(other.transform)) return;

        playerInside = true;
        SetPromptVisible(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsThePlayer(other.transform)) return;

        playerInside = false;
        SetPromptVisible(false);
    }

    void Update()
    {
        if (!playerInside) return;
        if (Input.GetKeyDown(activateKey))
        {
            TryTeleport();
        }
    }

    bool IsThePlayer(Transform t)
    {
        if (player != null) return t == player;
        // Fallback: tagged Player
        return t.CompareTag("Player");
    }

    void SetPromptVisible(bool visible)
    {
        if (promptText != null)
            promptText.gameObject.SetActive(visible);
    }

    void TryTeleport()
    {
        if (destination == null)
        {
            Debug.LogWarning("[SimpleTeleporter] Destination is not assigned.");
            return;
        }
        if (player == null)
        {
            Debug.LogWarning("[SimpleTeleporter] Player Transform not found/assigned.");
            return;
        }

        // Handle CharacterController cleanly
        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            player.SetPositionAndRotation(destination.position, destination.rotation);
            cc.enabled = true;
        }
        else
        {
            // Handle Rigidbody or plain Transform
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = destination.position;
                rb.rotation = destination.rotation;
            }
            else
            {
                player.SetPositionAndRotation(destination.position, destination.rotation);
            }
        }

        // Hide prompt after teleport
        SetPromptVisible(false);
        playerInside = false;
    }
}
