using UnityEngine;

public interface IConsumable
{
    // Return true if the item was consumed (so we remove it from inventory).
    bool Consume(GameObject user);
}