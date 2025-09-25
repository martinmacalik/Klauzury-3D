using System.Collections;
using UnityEngine;

public class WeaponHotkeys : MonoBehaviour
{
    // Global authoritative flag: false until draw finishes.
    public static bool GunIsReady = false;

    [SerializeField] Animator anim;
    [Tooltip("Seconds to wait after Draw before aiming/shooting is allowed.")]
    [SerializeField] float drawLockSeconds = 2f;

    Coroutine drawCoroutine;

    void Start()
    {
        // Start locked (weapon not ready when scene starts / when not drawn)
        GunIsReady = false;
        if (anim) anim.SetBool("IsReady", false);
    }

    void Update()
    {
        // While locked, force animator state to not aim and clear any fire triggers
        // (this prevents other scripts from toggling ADS/Fire on the animator).
        if (!GunIsReady && anim)
        {
            anim.SetBool("IsADS", false);
            anim.ResetTrigger("Fire");
        }

        // Trigger draw on hotkey
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            if (anim) anim.SetTrigger("DrawGun");

            // Restart the draw-lock coroutine so multiple presses behave sensibly
            if (drawCoroutine != null) StopCoroutine(drawCoroutine);
            drawCoroutine = StartCoroutine(DrawLockCoroutine());
        }
    }

    IEnumerator DrawLockCoroutine()
    {
        // Immediately mark not ready and update animator bool
        GunIsReady = false;
        if (anim) anim.SetBool("IsReady", false);

        // Wait exact draw duration
        yield return new WaitForSeconds(drawLockSeconds);

        // Now mark ready and update animator
        GunIsReady = true;
        if (anim) anim.SetBool("IsReady", true);

        drawCoroutine = null;
    }

    // Utility: allows other systems to force the weapon ready (useful for debugging)
    public void ForceReadyNow()
    {
        if (drawCoroutine != null) { StopCoroutine(drawCoroutine); drawCoroutine = null; }
        GunIsReady = true;
        if (anim) anim.SetBool("IsReady", true);
    }

    // Utility: re-lock the weapon from code (useful if you have an alternate draw flow)
    public void ForceLockNow()
    {
        if (drawCoroutine != null) { StopCoroutine(drawCoroutine); drawCoroutine = null; }
        GunIsReady = false;
        if (anim) anim.SetBool("IsReady", false);
        drawCoroutine = StartCoroutine(DrawLockCoroutine());
    }
}
