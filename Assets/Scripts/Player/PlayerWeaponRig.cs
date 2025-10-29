// using System.Collections;
// using UnityEngine;
//
// [DisallowMultipleComponent]
// public class PlayerWeaponRig : MonoBehaviour
// {
//     [System.Serializable]
//     public class WeaponEntry
//     {
//         [Tooltip("Must match your Inventory/Equipped name (e.g. \"Pistol\")")]
//         public string weaponName;
//
//         [Header("Scene Refs")]
//         public GameObject gunRoot;       // child under the hand
//         [HideInInspector] public SimpleGun simpleGun; // auto-filled
//     }
//
//     [Header("Rig")]
//     public Animator playerAnimator;      // the PLAYER animator (has draw anim)
//     public WeaponEntry[] weapons;        // 4 entries, one per gun
//
//     [Header("Input")]
//     public KeyCode drawToggleKey = KeyCode.R; // press to Draw/Holster
//
//     [Header("Timings")]
//     [Tooltip("Seconds to wait after triggering the player's draw before the gun is 'ready'.")]
//     public float drawLockSeconds = 0.6f;
//
//     [Header("Animator Parameters (player)")]
//     public string drawTrigger = "Draw";     // trigger name on player animator
//     public string holsterTrigger = "Holster"; // optional, if you have one
//     public string readyBool = "GunReady";   // optional; set true after draw
//
//     EquipmentController _equipment;
//     WeaponEntry _currentEquipped;   // which model matches EquipmentController.Equipped
//     WeaponEntry _currentDrawn;      // which model is currently drawn/visible
//     Coroutine _drawCo;
//
//     void Awake()
//     {
//         _equipment = FindObjectOfType<EquipmentController>(true);
//         if (_equipment) _equipment.OnEquippedChanged += OnEquippedChanged;
//
//         // Cache SimpleGun and hide everything
//         if (weapons != null)
//         {
//             foreach (var w in weapons)
//             {
//                 if (!w || !w.gunRoot) continue;
//                 w.simpleGun = w.gunRoot.GetComponentInChildren<SimpleGun>(true);
//                 SetGunActive(w, false);
//             }
//         }
//
//         // Initialize from current equipped (if any)
//         OnEquippedChanged(_equipment ? _equipment.Equipped : null);
//     }
//
//     void OnDestroy()
//     {
//         if (_equipment) _equipment.OnEquippedChanged -= OnEquippedChanged;
//     }
//
//     void Update()
//     {
//         if (Input.GetKeyDown(drawToggleKey))
//         {
//             if (_currentDrawn == null)
//                 TryDrawEquipped();
//             else
//                 Holster();
//         }
//     }
//
//     void OnEquippedChanged(string equippedName)
//     {
//         // Find matching entry
//         _currentEquipped = null;
//         if (!string.IsNullOrEmpty(equippedName) && weapons != null)
//         {
//             foreach (var w in weapons)
//             {
//                 if (w != null && w.weaponName == equippedName)
//                 {
//                     _currentEquipped = w;
//                     break;
//                 }
//             }
//         }
//
//         // If we had something drawn that no longer matches, holster it
//         if (_currentDrawn != null && _currentDrawn != _currentEquipped)
//         {
//             Holster();
//         }
//
//         // Keep non-equipped guns hidden
//         foreach (var w in weapons)
//         {
//             if (w == null) continue;
//             if (w != _currentDrawn) SetGunActive(w, false);
//         }
//     }
//
//     public void TryDrawEquipped()
//     {
//         if (_currentEquipped == null)
//         {
//             Debug.Log("[WeaponRig] No equipped weapon to draw.");
//             return;
//         }
//
//         // Show that gun model
//         SetGunActive(_currentEquipped, true);
//         _currentDrawn = _currentEquipped;
//
//         // Kick player draw animation
//         if (playerAnimator && !string.IsNullOrEmpty(drawTrigger))
//             playerAnimator.SetTrigger(drawTrigger);
//
//         // Lock until draw finishes
//         if (_drawCo != null) StopCoroutine(_drawCo);
//         _drawCo = StartCoroutine(DrawLockThenReady());
//     }
//
//     IEnumerator DrawLockThenReady()
//     {
//         // Mark not ready during draw
//         if (playerAnimator && !string.IsNullOrEmpty(readyBool))
//             playerAnimator.SetBool(readyBool, false);
//
//         // Disable firing during draw
//         if (_currentDrawn?.simpleGun) _currentDrawn.simpleGun.enabled = false;
//
//         float wait = Mathf.Max(0, drawLockSeconds);
//         if (wait > 0) yield return new WaitForSeconds(wait);
//
//         // Now ready: enable shooting script and set animator bool (optional)
//         if (_currentDrawn?.simpleGun) _currentDrawn.simpleGun.enabled = true;
//         if (playerAnimator && !string.IsNullOrEmpty(readyBool))
//             playerAnimator.SetBool(readyBool, true);
//
//         _drawCo = null;
//     }
//
//     public void Holster()
//     {
//         if (_drawCo != null) { StopCoroutine(_drawCo); _drawCo = null; }
//
//         // Disable shooting and hide model
//         if (_currentDrawn?.simpleGun) _currentDrawn.simpleGun.enabled = false;
//         SetGunActive(_currentDrawn, false);
//
//         if (playerAnimator && !string.IsNullOrEmpty(readyBool))
//             playerAnimator.SetBool(readyBool, false);
//         if (playerAnimator && !string.IsNullOrEmpty(holsterTrigger))
//             playerAnimator.SetTrigger(holsterTrigger);
//
//         _currentDrawn = null;
//     }
//
//     void SetGunActive(WeaponEntry w, bool on)
//     {
//         if (w?.gunRoot) w.gunRoot.SetActive(on);
//         if (w?.simpleGun) w.simpleGun.enabled = on; // only runs when visible
//     }
// }
