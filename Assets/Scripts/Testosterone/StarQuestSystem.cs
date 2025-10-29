using System;
using System.Collections.Generic;
using UnityEngine;

public class StarQuestSystem : MonoBehaviour
{
    public enum QuestType
    {
        BuyAnyWeapon,        // completes the first time a weapon is purchased
        ReachKills,          // complete when Kills >= target
        ReachMoney,          // complete when Money >= target
        ReachGems,           // complete when Gems >= target
        TestosteronePercent, // complete when Testosterone normalized*100 >= target
    }

    [Serializable]
    public class Quest
    {
        public string name = "Buy a weapon";
        public QuestType type = QuestType.BuyAnyWeapon;
        public int target = 1;           // threshold for progress-based quests
        [NonSerialized] public bool completed;
    }

    [Header("Config")]
    public List<Quest> quests = new() {
        new Quest { name = "Buy any weapon", type = QuestType.BuyAnyWeapon },
        new Quest { name = "Get 10 kills",   type = QuestType.ReachKills, target = 10 },
        new Quest { name = "Have $500",      type = QuestType.ReachMoney, target = 500 },
        new Quest { name = "Own 5 gems",     type = QuestType.ReachGems,  target = 5 },
        new Quest { name = "T at 80%",       type = QuestType.TestosteronePercent, target = 80 },
    };

    [Header("Wiring")]
    public PlayerMenuController menu; // assign in Inspector (or we auto-find)

    void Awake()
    {
        if (!menu) menu = PlayerMenuController.Instance; // your existing singleton :contentReference[oaicite:1]{index=1}
    }

    void OnEnable()
    {
        // Hook global events
        GameEvents.OnWeaponPurchased += HandleWeaponPurchased;
        GameEvents.OnKillsChanged    += HandleKillsChanged;
        GameEvents.OnMoneyChanged    += HandleMoneyChanged;
        GameEvents.OnGemsChanged     += HandleGemsChanged;

        // Optional: live testosterone
        var T = TestosteroneSystem.Instance; // your existing system :contentReference[oaicite:2]{index=2}
        if (T != null)
        {
            T.OnValueChanged.AddListener(OnTestosteroneChanged);
            OnTestosteroneChanged(T.Normalized);
        }

        RefreshStars();
    }

    void OnDisable()
    {
        GameEvents.OnWeaponPurchased -= HandleWeaponPurchased;
        GameEvents.OnKillsChanged    -= HandleKillsChanged;
        GameEvents.OnMoneyChanged    -= HandleMoneyChanged;
        GameEvents.OnGemsChanged     -= HandleGemsChanged;

        var T = TestosteroneSystem.Instance;
        if (T != null) T.OnValueChanged.RemoveListener(OnTestosteroneChanged);
    }

    // ----- Event handlers -----
    void HandleWeaponPurchased(string itemName)
    {
        CompleteFirst(QuestType.BuyAnyWeapon);
    }
    void HandleKillsChanged(int total)
    {
        CompleteAllWhere(q => q.type == QuestType.ReachKills && total >= q.target);
    }
    void HandleMoneyChanged(int money)
    {
        CompleteAllWhere(q => q.type == QuestType.ReachMoney && money >= q.target);
    }
    void HandleGemsChanged(int gems)
    {
        CompleteAllWhere(q => q.type == QuestType.ReachGems && gems >= q.target);
    }
    void OnTestosteroneChanged(float normalized)
    {
        int pct = Mathf.RoundToInt(normalized * 100f);
        CompleteAllWhere(q => q.type == QuestType.TestosteronePercent && pct >= q.target);
    }

    // ----- Helpers -----
    void CompleteFirst(QuestType t)
    {
        for (int i = 0; i < quests.Count; i++)
        {
            var q = quests[i];
            if (q.type == t && !q.completed)
            {
                q.completed = true;
                RefreshStars();
                return;
            }
        }
    }

    void CompleteAllWhere(Func<Quest, bool> predicate)
    {
        bool changed = false;
        for (int i = 0; i < quests.Count; i++)
        {
            if (!quests[i].completed && predicate(quests[i]))
            {
                quests[i].completed = true;
                changed = true;
            }
        }
        if (changed) RefreshStars();
    }

    void RefreshStars()
    {
        int completed = 0;
        for (int i = 0; i < quests.Count; i++) if (quests[i].completed) completed++;

        // drive your existing star UI
        if (menu != null) menu.SetStarLevel(Mathf.Clamp(completed, 0, quests.Count)); // lights N stars :contentReference[oaicite:3]{index=3}
        // Debug.Log($"[StarQuests] {completed}/{quests.Count} stars lit.");
    }
}
