using UnityEngine;

public class VehicleGunLocker : MonoBehaviour
{
    [Header("Optional (nice to have)")]
    [SerializeField] Animator gunAnimator; // assign your weapon animator if you have it

    // Call the moment you ENTER a vehicle
    public void OnEnterVehicle()
    {
        WeaponHotkeys.GunIsReady = false;                           // hard lock
        if (gunAnimator)
        {
            gunAnimator.ResetTrigger("Fire");
            gunAnimator.SetBool("IsADS", false);
            gunAnimator.SetBool("IsReady", false);
        }
        // If you hide your weapon object on enter, keep doing it; SimpleGun will auto-holster while locked.
    }

    // Call the moment you EXIT a vehicle
    public void OnExitVehicle()
    {
        WeaponHotkeys.GunIsReady = false;                           // stay locked after exit
        if (gunAnimator)
        {
            gunAnimator.ResetTrigger("Fire");
            gunAnimator.SetBool("IsADS", false);
            gunAnimator.SetBool("IsReady", false);
        }
        // Player must press your draw key (2) to re-arm — your WeaponHotkeys handles the timed unlock. :contentReference[oaicite:0]{index=0}
    }
}