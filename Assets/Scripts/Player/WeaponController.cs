using UnityEngine;

public class WeaponHotkeys : MonoBehaviour
{
    [SerializeField] Animator anim;

    void Update()
    {
        // Triggers on the top-row "2" key on any layout (incl. CZ where it's "ě"/"2")
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            anim.SetTrigger("DrawGun");
        }
    }
}