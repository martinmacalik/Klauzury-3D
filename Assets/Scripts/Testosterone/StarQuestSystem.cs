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
        PunchPeople,         // complete when Punches >= target
        FindMiscItem,        // complete when a specific misc item is found
    }

    [Serializable]
    public class Quest
    {
        public string name = "Buy a weapon";
        public QuestType type = QuestType.BuyAnyWeapon;
        public int target = 1;           // threshold for progress-based quests
        public string miscItemName = ""; // for FindMiscItem type quests
        [NonSerialized] public bool completed;
    }

    [Header("Config")]
    public List<Quest> quests = new() {
        new Quest { name = "Buy a weapon", type = QuestType.BuyAnyWeapon },
        new Quest { name = "Get 10 kills",   type = QuestType.ReachKills, target = 10 },
        new Quest { name = "Reach $500",     type = QuestType.ReachMoney, target = 500 },
        new Quest { name = "Punch 5 people", type = QuestType.PunchPeople, target = 5 },
        new Quest { name = "Find misc item", type = QuestType.FindMiscItem, miscItemName = "TestBottle" },
    };

    [Header("Wiring")]
    public PlayerMenuController menu; // assign in Inspector (or we auto-find)
    
    [Header("Debug - Toggle to Complete Quests")]
    [Tooltip("Check to manually complete Quest 1: Buy a weapon")]
    public bool debugCompleteQuest1;
    [Tooltip("Check to manually complete Quest 2: Get 10 kills")]
    public bool debugCompleteQuest2;
    [Tooltip("Check to manually complete Quest 3: Reach $500")]
    public bool debugCompleteQuest3;
    [Tooltip("Check to manually complete Quest 4: Punch 5 people")]
    public bool debugCompleteQuest4;
    [Tooltip("Check to manually complete Quest 5: Find misc item")]
    public bool debugCompleteQuest5;
    
    // Debug methods to manually complete quests (accessible via right-click menu in Inspector)
    [ContextMenu("Complete Quest 1: Buy a weapon")]
    void DebugCompleteQuest1()
    {
        if (quests.Count > 0)
        {
            quests[0].completed = true;
            RefreshStars();
            Debug.Log("[StarQuestSystem] DEBUG: Manually completed Quest 1 - " + quests[0].name);
        }
    }
    
    [ContextMenu("Complete Quest 2: Get 10 kills")]
    void DebugCompleteQuest2()
    {
        if (quests.Count > 1)
        {
            quests[1].completed = true;
            RefreshStars();
            Debug.Log("[StarQuestSystem] DEBUG: Manually completed Quest 2 - " + quests[1].name);
        }
    }
    
    [ContextMenu("Complete Quest 3: Reach $500")]
    void DebugCompleteQuest3()
    {
        if (quests.Count > 2)
        {
            quests[2].completed = true;
            RefreshStars();
            Debug.Log("[StarQuestSystem] DEBUG: Manually completed Quest 3 - " + quests[2].name);
        }
    }
    
    [ContextMenu("Complete Quest 4: Punch 5 people")]
    void DebugCompleteQuest4()
    {
        if (quests.Count > 3)
        {
            quests[3].completed = true;
            RefreshStars();
            Debug.Log("[StarQuestSystem] DEBUG: Manually completed Quest 4 - " + quests[3].name);
        }
    }
    
    [ContextMenu("Complete Quest 5: Find misc item")]
    void DebugCompleteQuest5()
    {
        if (quests.Count > 4)
        {
            quests[4].completed = true;
            RefreshStars();
            Debug.Log("[StarQuestSystem] DEBUG: Manually completed Quest 5 - " + quests[4].name);
        }
    }
    
    [ContextMenu("Complete All Quests")]
    void DebugCompleteAllQuests()
    {
        for (int i = 0; i < quests.Count; i++)
        {
            quests[i].completed = true;
        }
        RefreshStars();
        Debug.Log("[StarQuestSystem] DEBUG: Manually completed ALL quests");
    }
    
    [ContextMenu("Reset All Quests")]
    void DebugResetAllQuests()
    {
        for (int i = 0; i < quests.Count; i++)
        {
            quests[i].completed = false;
        }
        RefreshStars();
        Debug.Log("[StarQuestSystem] DEBUG: Reset all quests to incomplete");
    }

    void Awake()
    {
        if (!menu) menu = PlayerMenuController.Instance; // your existing singleton :contentReference[oaicite:1]{index=1}
    }
    
    void Update()
    {
        // Check debug booleans and complete quests when toggled
        if (debugCompleteQuest1 && quests.Count > 0 && !quests[0].completed)
        {
            quests[0].completed = true;
            RefreshStars();
            Debug.Log("[StarQuestSystem] DEBUG: Quest 1 completed - " + quests[0].name);
        }
        
        if (debugCompleteQuest2 && quests.Count > 1 && !quests[1].completed)
        {
            quests[1].completed = true;
            RefreshStars();
            Debug.Log("[StarQuestSystem] DEBUG: Quest 2 completed - " + quests[1].name);
        }
        
        if (debugCompleteQuest3 && quests.Count > 2 && !quests[2].completed)
        {
            quests[2].completed = true;
            RefreshStars();
            Debug.Log("[StarQuestSystem] DEBUG: Quest 3 completed - " + quests[2].name);
        }
        
        if (debugCompleteQuest4 && quests.Count > 3 && !quests[3].completed)
        {
            quests[3].completed = true;
            RefreshStars();
            Debug.Log("[StarQuestSystem] DEBUG: Quest 4 completed - " + quests[3].name);
        }
        
        if (debugCompleteQuest5 && quests.Count > 4 && !quests[4].completed)
        {
            quests[4].completed = true;
            RefreshStars();
            Debug.Log("[StarQuestSystem] DEBUG: Quest 5 completed - " + quests[4].name);
        }
    }

    void OnEnable()
    {
        // Hook global events
        GameEvents.OnWeaponPurchased += HandleWeaponPurchased;
        GameEvents.OnKillsChanged    += HandleKillsChanged;
        GameEvents.OnMoneyChanged    += HandleMoneyChanged;
        GameEvents.OnPunchesChanged  += HandlePunchesChanged;
        GameEvents.OnMiscQuestItemFound += HandleMiscQuestItemFound;

        RefreshStars();
    }

    void OnDisable()
    {
        GameEvents.OnWeaponPurchased -= HandleWeaponPurchased;
        GameEvents.OnKillsChanged    -= HandleKillsChanged;
        GameEvents.OnMoneyChanged    -= HandleMoneyChanged;
        GameEvents.OnPunchesChanged  -= HandlePunchesChanged;
        GameEvents.OnMiscQuestItemFound -= HandleMiscQuestItemFound;
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
    void HandlePunchesChanged(int punches)
    {
        CompleteAllWhere(q => q.type == QuestType.PunchPeople && punches >= q.target);
    }
    void HandleMiscQuestItemFound(string itemName)
    {
        Debug.Log($"[StarQuestSystem] Misc quest item found: '{itemName}'");
        CompleteAllWhere(q => q.type == QuestType.FindMiscItem && 
                             string.Equals(q.miscItemName, itemName, StringComparison.OrdinalIgnoreCase));
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
