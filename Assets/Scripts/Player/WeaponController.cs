using System.Collections;
using UnityEngine;

public class WeaponHotkeys : MonoBehaviour
{
    [System.Serializable]
    public class GunEntry
    {
        public string weaponName;

        [Header("Scene Refs")]
        public GameObject gunRoot;      // enable/disable this under the hand
        public Transform muzzle;        // <-- ADD: assign the barrel tip for THIS gun in the Inspector

        [Header("Draw")]
        public float drawLockSeconds = 0.75f;

        [Header("Hotkey (if not using single-key mode)")]
        public KeyCode hotkey = KeyCode.None;
    }

    // Global: SimpleGun reads this to decide if it can shoot/ADS
    public static bool GunIsReady = false;

    [Header("Configured Guns (under the hand)")]
    public GunEntry[] guns = new GunEntry[4];

    [Header("Player-level components")]
    [SerializeField] Animator playerAnimator;
    [SerializeField] SimpleGun simpleGun;  // <-- ADD: drag your player’s SimpleGun here
    
    [SerializeField] string drawTrigger = "DrawGun";
    [SerializeField] string readyBool   = "IsReady";
    [SerializeField] string adsBool     = "IsADS";
    [SerializeField] string fireTrigger = "Fire";

    [Header("Rules")]
    public bool requireEquipped = false;          // if true, must match EquipmentController.Equipped

    [Header("Keys & SFX")]
    public KeyCode holsterKey = KeyCode.H;
    public AudioSource sfx;
    public AudioClip deniedSfx;
    public AudioClip drawSfx;

    int _currentIndex = -1;
    Coroutine _drawCo;
    EquipmentController _equipment;

    void Awake()
    {
        _equipment = FindObjectOfType<EquipmentController>(true);

        // Auto-assign 1..4
        var defaults = new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };
        for (int i = 0; i < guns.Length && i < defaults.Length; i++)
            if (guns[i] != null && guns[i].hotkey == KeyCode.None)
                guns[i].hotkey = defaults[i];

        // Hide all guns on start
        for (int i = 0; i < guns.Length; i++)
            if (guns[i]?.gunRoot) guns[i].gunRoot.SetActive(false);

        GunIsReady = false;
        if (playerAnimator && !string.IsNullOrEmpty(readyBool))
            playerAnimator.SetBool(readyBool, false);
    }

    void Update()
    {
        if (!GunIsReady && playerAnimator)
        {
            if (!string.IsNullOrEmpty(adsBool))     playerAnimator.SetBool(adsBool, false);
            if (!string.IsNullOrEmpty(fireTrigger)) playerAnimator.ResetTrigger(fireTrigger);
            if (!string.IsNullOrEmpty(readyBool))   playerAnimator.SetBool(readyBool, false);
        }

        // Single-key cycle on "2"
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            int start = Mathf.Max(-1, _currentIndex);
            for (int step = 1; step <= guns.Length; step++)
            {
                int i = (start + step) % guns.Length;
                var g = guns[i];
                if (g == null) continue;

                bool owned = WeaponInventory.Instance && WeaponInventory.Instance.CountOf(g.weaponName) > 0;
                if (!owned) continue;

                TrySelectAndDraw(i);
                break;
            }
        }

        // Holster on H (optional — keep if you want)
        if (Input.GetKeyDown(holsterKey))
            Holster();
    }


    void TrySelectAndDraw(int index)
    {
        if (index < 0 || index >= guns.Length) return;
        var g = guns[index]; if (g == null) return;

        // Ownership gate
        var wInv = WeaponInventory.Instance;
        bool owned = (wInv && wInv.CountOf(g.weaponName) > 0);
        if (!owned)
        {
            if (sfx && deniedSfx) sfx.PlayOneShot(deniedSfx);
            Debug.Log($"[Weapons] '{g.weaponName}' not owned — draw blocked.");
            return;
        }

        // Must be equipped? (optional)
        if (requireEquipped && _equipment && _equipment.Equipped != g.weaponName)
        {
            if (sfx && deniedSfx) sfx.PlayOneShot(deniedSfx);
            Debug.Log($"[Weapons] '{g.weaponName}' owned but not equipped — blocked.");
            return;
        }

        SetActiveGun(index);

        // Player draw animation + SFX
        if (playerAnimator && !string.IsNullOrEmpty(drawTrigger))
            playerAnimator.SetTrigger(drawTrigger);
        if (sfx && drawSfx) sfx.PlayOneShot(drawSfx);

        // Timed lock
        if (_drawCo != null) StopCoroutine(_drawCo);
        _drawCo = StartCoroutine(DrawLockCoroutine(g.drawLockSeconds));
    }

    void SetActiveGun(int index)
    {
        // Disable all first (simple + safe)
        for (int i = 0; i < guns.Length; i++)
            if (guns[i]?.gunRoot) guns[i].gunRoot.SetActive(false);

        _currentIndex = index;
        var cur = guns[_currentIndex];

        if (cur?.gunRoot) cur.gunRoot.SetActive(true);

        // Tell SimpleGun which muzzle to use now
        if (simpleGun) simpleGun.SetMuzzle(cur != null ? cur.muzzle : null);

        GunIsReady = false;
        if (playerAnimator) playerAnimator.SetBool(readyBool, false);
    }


    IEnumerator DrawLockCoroutine(float seconds)
    {
        GunIsReady = false;
        if (playerAnimator && !string.IsNullOrEmpty(readyBool))
            playerAnimator.SetBool(readyBool, false);

        float wait = Mathf.Max(0f, seconds);
        if (wait > 0f) yield return new WaitForSeconds(wait);

        GunIsReady = true;
        if (playerAnimator && !string.IsNullOrEmpty(readyBool))
            playerAnimator.SetBool(readyBool, true);
        _drawCo = null;
    }

    public void Holster()
    {
        GunIsReady = false;
        if (_drawCo != null) { StopCoroutine(_drawCo); _drawCo = null; }

        // Hide all guns
        for (int i = 0; i < guns.Length; i++)
            if (guns[i]?.gunRoot) guns[i].gunRoot.SetActive(false);

        if (playerAnimator && !string.IsNullOrEmpty(readyBool))
            playerAnimator.SetBool(readyBool, false);
        _currentIndex = -1;
    }
}
