using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Mini3DSlot : MonoBehaviour
{
    [Header("UI")]
    public RawImage targetImage;         // assign a RawImage on your slot
    public Vector3 modelOffset = new Vector3(0, 0, 0);
    public Vector3 modelEuler = new Vector3(0, 30, 0);
    public float modelScale = 1f;

    [Header("Camera")]
    public float distance = 2.2f;
    public float fov = 25f;

    [Header("Optional layer for models & camera culling")]
    public string modelLayerName = "UI3D"; // create this layer in Project Settings > Tags & Layers

    GameObject _spawned;
    Camera _cam;
    RenderTexture _rt;
    Transform _pivot;

    void Awake()
    {
        if (!targetImage) targetImage = GetComponentInChildren<RawImage>(true);

        _pivot = new GameObject("Pivot").transform;
        _pivot.SetParent(transform, false);
        _pivot.localPosition = Vector3.zero;

        var camGO = new GameObject("MiniCam");
        _cam = camGO.AddComponent<Camera>();
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = new Color(0,0,0,0); // transparent
        _cam.orthographic = false;
        _cam.fieldOfView = fov;
        _cam.nearClipPlane = 0.05f;
        _cam.farClipPlane = 50f;
        _cam.enabled = true;

        int layer = LayerMask.NameToLayer(modelLayerName);
        if (layer >= 0) { _cam.cullingMask = 1 << layer; } // only render that layer

        _rt = new RenderTexture(256, 256, 16, RenderTextureFormat.Default);
        _rt.Create();
        _cam.targetTexture = _rt;
        if (targetImage) targetImage.texture = _rt;

        // place camera in world space pointing at the pivot (we don't need it under UI)
        _cam.transform.position = new Vector3(9999, 9999, 9999); // keep it out of the scene
    }

    void LateUpdate()
    {
        if (_spawned)
        {
            // re-aim the camera each frame in case RT size/aspect changed
            var bb = new Bounds(_spawned.transform.position, Vector3.one * 0.5f);
            var rends = _spawned.GetComponentsInChildren<Renderer>();
            foreach (var r in rends) bb.Encapsulate(r.bounds);

            var size = Mathf.Max(bb.size.x, bb.size.y, bb.size.z);
            float dist = distance * (size / 1.0f);

            var lookPos = bb.center;
            var camPos  = lookPos + new Vector3(0, 0, dist);
            _cam.transform.position = camPos;
            _cam.transform.LookAt(lookPos);
        }
    }

    public void Clear()
    {
        if (_spawned) Destroy(_spawned);
        if (_pivot) _pivot.localRotation = Quaternion.identity;
    }

    public void ShowPrefab(GameObject prefab)
    {
        Clear();
        if (!prefab) return;

        _spawned = Instantiate(prefab, _pivot);
        _spawned.transform.localPosition = modelOffset;
        _spawned.transform.localEulerAngles = modelEuler;
        _spawned.transform.localScale = Vector3.one * modelScale;

        // put on UI3D layer so only our camera sees it
        int layer = LayerMask.NameToLayer(modelLayerName);
        if (layer >= 0) SetLayerRecursively(_spawned, layer);
    }

    void SetLayerRecursively(GameObject go, int layer)
    {
        var t = go.transform;
        var stack = new System.Collections.Generic.Stack<Transform>();
        stack.Push(t);
        while (stack.Count > 0)
        {
            var x = stack.Pop();
            x.gameObject.layer = layer;
            for (int i = 0; i < x.childCount; i++) stack.Push(x.GetChild(i));
        }
    }
}
