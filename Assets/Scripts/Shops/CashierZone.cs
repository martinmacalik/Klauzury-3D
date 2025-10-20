using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CashierZone : MonoBehaviour
{
    public string playerTag = "Player";

    void Reset() { var c = GetComponent<Collider>(); c.isTrigger = true; }

    // CashierZone.cs — add logs in triggers
    void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;
        var basket = other.GetComponentInParent<Basket>();
        if (basket) { basket.canPayHere = true; Debug.Log("[Cashier] canPayHere = TRUE"); }
    }

    void OnTriggerExit(Collider other)
    {
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;
        var basket = other.GetComponentInParent<Basket>();
        if (basket) { basket.canPayHere = false; Debug.Log("[Cashier] canPayHere = FALSE"); }
    }

}