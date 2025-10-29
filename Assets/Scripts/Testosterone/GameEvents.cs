using System;

public static class GameEvents
{
    // Progress-style events
    public static event Action<string> OnWeaponPurchased;   // arg = item name
    public static event Action<int>    OnKillsChanged;      // total kills
    public static event Action<int>    OnMoneyChanged;      // current money
    public static event Action<int>    OnGemsChanged;       // current gems

    // Raise helpers (optional but nice)
    public static void RaiseWeaponPurchased(string itemName) => OnWeaponPurchased?.Invoke(itemName);
    public static void RaiseKillsChanged(int total)          => OnKillsChanged?.Invoke(total);
    public static void RaiseMoneyChanged(int amount)         => OnMoneyChanged?.Invoke(amount);
    public static void RaiseGemsChanged(int amount)          => OnGemsChanged?.Invoke(amount);
}