using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WeaponInventory : MonoBehaviour
{
    // --- Singleton ---
    public static WeaponInventory Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Uncomment if you want the inventory to persist across scene loads:
        // DontDestroyOnLoad(gameObject);
    }

    // --- Data + events ---
    public event Action OnChanged;

    // Keep duplicates: each string entry is ONE owned weapon
    [SerializeField] private List<string> owned = new();
    public IReadOnlyList<string> Owned => owned;

    public void AddWeapon(string name, int count = 1)
    {
        if (string.IsNullOrEmpty(name) || count <= 0) return;
        for (int i = 0; i < count; i++) owned.Add(name);
        // Debug.Log($"[WeaponInventory] Added '{name}' x{count}. Total now {owned.Count}");
        OnChanged?.Invoke();
    }

    public int CountOf(string name)
    {
        int c = 0;
        for (int i = 0; i < owned.Count; i++) if (owned[i] == name) c++;
        return c;
    }

    public void ClearAll()
    {
        owned.Clear();
        OnChanged?.Invoke();
    }

    public bool RemoveOne(string name)
    {
        int idx = owned.IndexOf(name);
        if (idx >= 0)
        {
            owned.RemoveAt(idx);
            OnChanged?.Invoke();
            return true;
        }
        return false;
    }
}