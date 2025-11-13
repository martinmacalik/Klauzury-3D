using UnityEngine;

/// <summary>
/// Displays a flag sprite above an NPC's head that always faces the camera.
/// Attach this to a child object with a SpriteRenderer component.
/// </summary>
public class NPCFlag : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float heightOffset = 2.5f; // Distance above NPC's head
    [SerializeField] private Vector3 positionOffset = Vector3.zero; // Local offset adjustments

    [Header("Billboard")]
    [SerializeField] private bool billboardToCamera = true;
    [SerializeField] private Camera mainCamera;

    [Header("Sprite")]
    [SerializeField] private Sprite rainbowFlagSprite;
    [SerializeField] private float spriteScale = 1f;

    private Transform parentTransform;

    void Awake()
    {
        parentTransform = transform.parent;
        
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (!mainCamera)
            mainCamera = Camera.main;

        if (rainbowFlagSprite)
            spriteRenderer.sprite = rainbowFlagSprite;
    }

    void LateUpdate()
    {
        if (!parentTransform) return;

        // Position above parent
        transform.position = parentTransform.position + Vector3.up * heightOffset + positionOffset;

        // Billboard to camera
        if (billboardToCamera && mainCamera)
        {
            transform.rotation = Quaternion.LookRotation(mainCamera.transform.position - transform.position);
        }
    }

    /// <summary>Show or hide the flag</summary>
    public void SetActive(bool active)
    {
        spriteRenderer.enabled = active;
    }

    /// <summary>Change the flag sprite at runtime</summary>
    public void SetSprite(Sprite newSprite)
    {
        if (spriteRenderer)
            spriteRenderer.sprite = newSprite;
    }
}
