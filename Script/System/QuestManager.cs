using System;
using UnityEngine;

// 任务管理器：追踪三个教程任务（收集木材、建造营火、夜晚在营火旁存活）。
// 任务完成时派发奖励材料并触发 UI 通知，所有任务完成后弹出教程完成面板。
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public event Action OnQuestUpdated;

    [Header("木头收集目标")]
    [SerializeField] private int targetWoodCount = 3;

    [Header("各任务奖励（完成时加入背包并触发拾取提示）")]
    [SerializeField] private int rewardWoodOnCollectWoodQuest  = 5;
    [SerializeField] private int rewardStoneOnCollectWoodQuest = 2;
    [SerializeField] private int rewardWoodOnCampfireQuest     = 3;
    [SerializeField] private int rewardStoneOnCampfireQuest    = 5;
    [SerializeField] private int rewardWoodOnSurviveNightQuest  = 2;
    [SerializeField] private int rewardStoneOnSurviveNightQuest = 3;
    [SerializeField] private int rewardWoodOnAllQuestsComplete  = 10;
    [SerializeField] private int rewardStoneOnAllQuestsComplete = 10;

    public int  CurrentWoodCount              { get; private set; }
    public bool WoodQuestCompleted            { get; private set; }
    public bool CampfireQuestCompleted        { get; private set; }
    public bool SurviveNightQuestCompleted    { get; private set; }
    public bool AllQuestsCompleted            { get; private set; }

    // 缓存引用，避免 Update 中每帧调用 FindObjectOfType
    private PlayerController _playerController;
    private PlayerInventory  _playerInventory;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnItemPickedUp   += HandleItemPickedUp;
        GameEvents.OnBuildingPlaced += HandleBuildingPlaced;
    }

    private void OnDisable()
    {
        GameEvents.OnItemPickedUp   -= HandleItemPickedUp;
        GameEvents.OnBuildingPlaced -= HandleBuildingPlaced;
    }

    private void Start()
    {
        _playerController = FindObjectOfType<PlayerController>();
        _playerInventory  = FindObjectOfType<PlayerInventory>();
        NotifyQuestUpdated();
    }

    private void Update()
    {
        // 每帧检测夜间在营火旁存活任务（营火安全范围判断）
        CheckSurviveNearFireQuest();
    }

    // 拾取物品时：累计木材数量，达标则完成收集任务
    private void HandleItemPickedUp(ItemType itemType, int amount)
    {
        if (WoodQuestCompleted || itemType != ItemType.Wood) return;

        CurrentWoodCount += amount;
        if (CurrentWoodCount >= targetWoodCount)
        {
            CurrentWoodCount = targetWoodCount;
            WoodQuestCompleted = true;
            NotificationUI.Instance?.ShowMessage("任务完成：拾取木材");
            GrantQuestItemReward(rewardWoodOnCollectWoodQuest, rewardStoneOnCollectWoodQuest);
        }

        NotifyQuestUpdated();
    }

    // 建筑放置时：名称中含"campfire/营火"则完成建造任务
    private void HandleBuildingPlaced(BuildingRecipeSO recipe)
    {
        if (CampfireQuestCompleted || recipe == null) return;

        string nameL = recipe.buildingName.ToLower();
        if (nameL.Contains("campfire") || recipe.buildingName.Contains("营火"))
        {
            CampfireQuestCompleted = true;
            NotificationUI.Instance?.ShowMessage("任务完成：建造营火");
            GrantQuestItemReward(rewardWoodOnCampfireQuest, rewardStoneOnCampfireQuest);
            NotifyQuestUpdated();
        }
    }

    // 每帧：夜间且玩家在任意营火安全范围内，则完成存活任务
    private void CheckSurviveNearFireQuest()
    {
        if (SurviveNightQuestCompleted) return;
        if (DayNightCycle.Instance == null) return;
        if (!DayNightCycle.Instance.IsNight()) return;

        if (IsPlayerSafeNearCampfire())
        {
            SurviveNightQuestCompleted = true;
            NotificationUI.Instance?.ShowMessage("任务完成：入夜时在营火旁存活");
            GrantQuestItemReward(rewardWoodOnSurviveNightQuest, rewardStoneOnSurviveNightQuest);
            NotifyQuestUpdated();
        }
    }

    // 判断玩家是否在任意营火的安全范围内
    private bool IsPlayerSafeNearCampfire()
    {
        if (_playerController == null)
            _playerController = FindObjectOfType<PlayerController>();

        if (_playerController != null)
            return CheckCampfireSafe(_playerController.transform.position);

        // 找不到 PlayerController 时用 Tag 兜底
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            return CheckCampfireSafe(playerObject.transform.position);

        return false;
    }

    // 遍历所有已注册营火实例，检查玩家坐标是否在安全范围内
    private bool CheckCampfireSafe(Vector3 playerPosition)
    {
        for (int i = 0; i < Campfire.RegisteredInstances.Count; i++)
        {
            Campfire campfire = Campfire.RegisteredInstances[i];
            if (campfire == null) continue;
            if (campfire.IsInSafeRange(playerPosition)) return true;
        }
        return false;
    }

    // 检查是否全部任务完成，若是则弹出教程完成面板
    private void CheckAllQuestsCompleted()
    {
        if (AllQuestsCompleted) return;
        if (!WoodQuestCompleted || !CampfireQuestCompleted || !SurviveNightQuestCompleted) return;

        AllQuestsCompleted = true;
        NotificationUI.Instance?.ShowMessage("全部任务完成");
        GrantQuestItemReward(rewardWoodOnAllQuestsComplete, rewardStoneOnAllQuestsComplete);

        TutorialCompleteUI.Instance?.ShowCompletePanel(
            "教程完成",
            "你已度过第一个夜晚，演示流程结束。"
        );
    }

    // 通知所有 UI 订阅者任务数据已更新
    private void NotifyQuestUpdated()
    {
        CheckAllQuestsCompleted();
        OnQuestUpdated?.Invoke();
    }

    public int GetTargetWoodCount() => targetWoodCount;

    // 将奖励材料加入背包并广播拾取事件（与捡起地上物品一致，会刷新 UI 和飘字）
    private void GrantQuestItemReward(int woodAmount, int stoneAmount)
    {
        if (woodAmount <= 0 && stoneAmount <= 0) return;

        if (_playerInventory == null)
            _playerInventory = FindObjectOfType<PlayerInventory>();
        if (_playerInventory == null) return;

        if (woodAmount > 0)
        {
            _playerInventory.AddItem(ItemType.Wood, woodAmount);
            GameEvents.RaiseItemPickedUp(ItemType.Wood, woodAmount);
        }

        if (stoneAmount > 0)
        {
            _playerInventory.AddItem(ItemType.Stone, stoneAmount);
            GameEvents.RaiseItemPickedUp(ItemType.Stone, stoneAmount);
        }
    }

    // 读档时恢复任务进度（SaveManager 调用）
    public void LoadQuestData(int woodCount, bool woodCompleted, bool campfireCompleted, bool surviveCompleted, bool allCompleted)
    {
        CurrentWoodCount           = woodCount;
        WoodQuestCompleted         = woodCompleted;
        CampfireQuestCompleted     = campfireCompleted;
        SurviveNightQuestCompleted = surviveCompleted;
        AllQuestsCompleted         = allCompleted;
        NotifyQuestUpdated();
    }
}
