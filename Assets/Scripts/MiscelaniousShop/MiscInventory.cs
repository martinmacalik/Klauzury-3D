using System;
using System.Collections.Generic;
using UnityEngine;

public class MiscInventory : MonoBehaviour
{
    public static MiscInventory Instance { get; private set; }

    // One entry = one copy (duplicates allowed).
    [SerializeField] private List<string> owned = new List<string>();
    public IReadOnlyList<string> Owned => owned;

    public event Action OnChanged;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AddItem(string name, int count = 1)
    {
        if (string.IsNullOrWhiteSpace(name) || count <= 0) return;
        for (int i = 0; i < count; i++) owned.Add(name);
        OnChanged?.Invoke();
    }

    public int CountOf(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;
        int c = 0; foreach (var n in owned) if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) c++;
        return c;
    }

    public bool RemoveOne(string name)
    {
        int idx = owned.FindIndex(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            owned.RemoveAt(idx);
            OnChanged?.Invoke();
            return true;
        }
        return false;
    }

    public void ClearAll()
    {
        owned.Clear();
        OnChanged?.Invoke();
    }
}