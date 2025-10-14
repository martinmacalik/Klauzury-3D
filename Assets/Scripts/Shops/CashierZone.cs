using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CashierZone : MonoBehaviour
{
    public string playerTag = "Player";

    void Reset() { var c = GetComponent<Collider>(); c.isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;
        var basket = other.GetComponentInParent<Basket>();
        if (basket) basket.canPayHere = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;
        var basket = other.GetComponentInParent<Basket>();
        if (basket) basket.canPayHere = false;
    }
}