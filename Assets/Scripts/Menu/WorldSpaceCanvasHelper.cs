using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WorldSpaceCanvasHelper : MonoBehaviour
{
    public Camera targetCamera;      // assign your MenuCamera
    public float distance = 5f;      // how far in front of the camera
    public Vector2 worldSize = new Vector2(2f, 1.1f); // width/height in world units
    public bool forceLayerRouting = true;
    public string canvasLayerName = "Default"; // or "Menu3D"

    [ContextMenu("SnapNow")]
    public void SnapNow()
    {
        if (!targetCamera) { Debug.LogWarning("[CanvasHelper] No targetCamera set."); return; }

        // Ensure layer/culling visibility
        if (forceLayerRouting)
        {
            int layer = LayerMask.NameToLayer(canvasLayerName);
            if (layer >= 0) gameObject.layer = layer;
            // (Children inherit if you want: uncomment to force)
            // SetLayerRecursively(transform, layer);
        }

        // Place in front of camera
        Transform ct = targetCamera.transform;
        Vector3 pos = ct.position + ct.forward * Mathf.Max(0.1f, distance);
        transform.position = pos;

        // Face the camera
        transform.rotation = Quaternion.LookRotation(transform.position - ct.position, Vector3.up);

        // World-space size
        var rt = GetComponent<RectTransform>();
        if (rt)
        {
            rt.sizeDelta = worldSize;   // world-space size in units
            transform.localScale = Vector3.one; // keep scale sane
        }

        // Clip plane sanity (so it doesn't get clipped out)
        targetCamera.nearClipPlane = Mathf.Min(targetCamera.nearClipPlane, 0.01f);
        targetCamera.farClipPlane  = Mathf.Max(targetCamera.farClipPlane, distance + 100f);
    }

    static void SetLayerRecursively(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        foreach (Transform c in t) SetLayerRecursively(c, layer);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(WorldSpaceCanvasHelper))]
public class WorldSpaceCanvasHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("SnapNow"))
        {
            (target as WorldSpaceCanvasHelper)?.SnapNow();
        }
    }
}
#endif
