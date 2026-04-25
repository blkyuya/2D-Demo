using System;
using System.IO;
using UnityEngine;

// 存档控制器：聚合各系统数据，主格式为 JSON（JsonUtility + persistentDataPath）。
// 仍可读旧版 save_game.bin（BinarySaveIO）与旧 save.json，便于迁移；保存成功后只保留 save_game.json。
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("引用")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerStats     playerStats;
    [SerializeField] private DayNightCycle   dayNightCycle;
    [SerializeField] private BuildingSystem buildingSystem;

    private const int CurrentSaveFormatVersion = 1;

    // 当前主存档路径（JSON）
    private static string JsonSavePath => Path.Combine(Application.persistentDataPath, "save_game.json");

    // 旧版二进制（仅读，用于迁移）
    private static string BinarySavePath => Path.Combine(Application.persistentDataPath, "save_game.bin");

    // 更早期的 JSON 文件名（仅读）
    private static string LegacyJsonSavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static bool ShouldLoadOnSceneStart = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (playerInventory == null) playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerStats     == null) playerStats     = FindObjectOfType<PlayerStats>();
        if (dayNightCycle   == null) dayNightCycle   = FindObjectOfType<DayNightCycle>();
        if (buildingSystem  == null) buildingSystem  = FindObjectOfType<BuildingSystem>();

        if (ShouldLoadOnSceneStart)
        {
            ShouldLoadOnSceneStart = false;
            LoadGame();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) SaveGame();
        if (Input.GetKeyDown(KeyCode.F9)) LoadGame();
    }

    // 收集数据并写入 save_game.json，成功后删除旧 bin/旧 json，避免重复入口
    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.saveFormatVersion = CurrentSaveFormatVersion;

        SavePlayerTransform(data);
        SavePlayerStats(data);
        SaveInventory(data);
        SaveTime(data);
        SaveQuestData(data);
        SaveEquipmentData(data);
        SaveCampfires(data);

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(JsonSavePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError("保存失败: " + ex.Message);
            return;
        }

        TryDeleteLegacySaveFiles();

        NotificationUI.Instance?.ShowMessage("游戏已保存");
    }

    // 删除已迁移的旧存档文件（主存档已是 save_game.json）
    private static void TryDeleteLegacySaveFiles()
    {
        try
        {
            if (File.Exists(BinarySavePath)) File.Delete(BinarySavePath);
            if (File.Exists(LegacyJsonSavePath)) File.Delete(LegacyJsonSavePath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("清理旧存档文件失败: " + ex.Message);
        }
    }

    // 读档顺序：新 JSON → 旧 JSON → 旧二进制
    public void LoadGame()
    {
        SaveData data = null;

        if (File.Exists(JsonSavePath))
            data = TryLoadFromJsonFile(JsonSavePath);

        if (data == null && File.Exists(LegacyJsonSavePath))
            data = TryLoadFromJsonFile(LegacyJsonSavePath);

        if (data == null && File.Exists(BinarySavePath))
            data = BinarySaveIO.ReadFromFile(BinarySavePath);

        if (data == null)
        {
            NotificationUI.Instance?.ShowMessage("无存档文件");
            return;
        }

        NormalizeSaveData(data);

        LoadPlayerTransform(data);
        LoadPlayerStats(data);
        LoadInventory(data);
        LoadEquipmentData(data);
        LoadTime(data);
        LoadQuestData(data);
        LoadCampfires(data);

        CommandInvoker.ClearUndoStack();

        NotificationUI.Instance?.ShowMessage("游戏已加载");
    }

    private static SaveData TryLoadFromJsonFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SaveManager] JSON 读档失败: " + ex.Message);
            return null;
        }
    }

    // 补齐 JsonUtility / 旧档可能缺失的字段
    private static void NormalizeSaveData(SaveData data)
    {
        if (data == null) return;
        if (data.saveFormatVersion <= 0) data.saveFormatVersion = 1;
        if (data.campfires == null) data.campfires = Array.Empty<CampfireSaveData>();
        if (data.equippedItemTypeOrMinusOne == null || data.equippedItemTypeOrMinusOne.Length != 6)
            data.equippedItemTypeOrMinusOne = new int[6] { -1, -1, -1, -1, -1, -1 };
    }

    private void SavePlayerTransform(SaveData data)
    {
        if (playerInventory == null) return;
        Transform t = playerInventory.transform;
        data.playerPosX = t.position.x;
        data.playerPosY = t.position.y;
        data.playerPosZ = t.position.z;
    }

    private void LoadPlayerTransform(SaveData data)
    {
        if (playerInventory == null) return;
        playerInventory.transform.position = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);
    }

    private void SavePlayerStats(SaveData data)
    {
        if (playerStats == null) return;
        data.playerHealth = playerStats.GetCurrentHealth();
        data.playerHunger = playerStats.GetCurrentHunger();
    }

    private void LoadPlayerStats(SaveData data)
    {
        if (playerStats == null) return;
        playerStats.LoadFromSaveData(data.playerHealth, data.playerHunger);
    }

    private void SaveInventory(SaveData data)
    {
        if (playerInventory == null) return;
        data.inventory = playerInventory.GetSaveData();
    }

    private void LoadInventory(SaveData data)
    {
        if (playerInventory == null || data.inventory == null) return;
        playerInventory.LoadFromSaveData(data.inventory);
    }

    private void SaveEquipmentData(SaveData data)
    {
        if (data.equippedItemTypeOrMinusOne == null || data.equippedItemTypeOrMinusOne.Length != 6)
            data.equippedItemTypeOrMinusOne = new int[6] { -1, -1, -1, -1, -1, -1 };

        InventoryUI ui = ResolveInventoryUI();
        if (ui == null) return;

        int[] saved = ui.GetEquippedSlotsForSave();
        for (int i = 0; i < 6; i++)
            data.equippedItemTypeOrMinusOne[i] = saved[i];
    }

    private void LoadEquipmentData(SaveData data)
    {
        int[] slots = data.equippedItemTypeOrMinusOne;
        if (slots == null || slots.Length != 6)
            slots = new int[6] { -1, -1, -1, -1, -1, -1 };

        InventoryUI ui = ResolveInventoryUI();
        if (ui == null) return;

        ui.LoadEquippedSlotsFromSave(slots);
    }

    private static InventoryUI ResolveInventoryUI()
    {
        if (InventoryUI.Instance != null) return InventoryUI.Instance;
        return FindObjectOfType<InventoryUI>(true);
    }

    private void SaveTime(SaveData data)
    {
        if (dayNightCycle == null) return;
        data.currentTimeNormalized = dayNightCycle.currentTimeNormalized;
    }

    private void LoadTime(SaveData data)
    {
        if (dayNightCycle == null) return;
        dayNightCycle.SetTimeNormalized(data.currentTimeNormalized);
    }

    private void SaveCampfires(SaveData data)
    {
        Campfire[] sceneCampfires = FindObjectsOfType<Campfire>();
        var arr = new CampfireSaveData[sceneCampfires.Length];
        for (int i = 0; i < sceneCampfires.Length; i++)
        {
            Campfire campfire = sceneCampfires[i];
            if (campfire == null) continue;
            arr[i] = new CampfireSaveData
            {
                posX = campfire.transform.position.x,
                posY = campfire.transform.position.y,
                posZ = campfire.transform.position.z,
                fuel = campfire.currentFuel
            };
        }
        data.campfires = arr;
    }

    private void LoadCampfires(SaveData data)
    {
        Campfire[] existing = FindObjectsOfType<Campfire>();
        for (int i = 0; i < existing.Length; i++)
            Destroy(existing[i].gameObject);

        if (data.campfires == null || data.campfires.Length == 0) return;
        if (buildingSystem == null || buildingSystem.recipes == null) return;

        GameObject campfirePrefab = FindCampfirePrefab();
        if (campfirePrefab == null)
        {
            Debug.LogWarning("没有找到营火 prefab，无法恢复营火");
            return;
        }

        for (int i = 0; i < data.campfires.Length; i++)
        {
            CampfireSaveData cd = data.campfires[i];
            if (cd == null) continue;
            Vector3 position = new Vector3(cd.posX, cd.posY, cd.posZ);
            GameObject obj = Instantiate(campfirePrefab, position, Quaternion.identity);
            ApplyEntitySortingLayer(obj);
            Campfire campfire = obj.GetComponent<Campfire>();
            if (campfire != null) campfire.SetFuel(cd.fuel);
        }
    }

    private GameObject FindCampfirePrefab()
    {
        if (buildingSystem == null || buildingSystem.recipes == null) return null;
        for (int i = 0; i < buildingSystem.recipes.Length; i++)
        {
            BuildingRecipeSO recipe = buildingSystem.recipes[i];
            if (recipe == null || recipe.buildingPrefab == null) continue;
            string name = recipe.buildingName.ToLower();
            if (name.Contains("campfire") || recipe.buildingName.Contains("营火"))
                return recipe.buildingPrefab;
        }
        return null;
    }

    private void SaveQuestData(SaveData data)
    {
        if (QuestManager.Instance == null) return;
        data.currentWoodCount              = QuestManager.Instance.CurrentWoodCount;
        data.woodQuestCompleted            = QuestManager.Instance.WoodQuestCompleted;
        data.campfireQuestCompleted        = QuestManager.Instance.CampfireQuestCompleted;
        data.surviveNightQuestCompleted    = QuestManager.Instance.SurviveNightQuestCompleted;
        data.allQuestsCompleted            = QuestManager.Instance.AllQuestsCompleted;
    }

    private void LoadQuestData(SaveData data)
    {
        if (QuestManager.Instance == null) return;
        QuestManager.Instance.LoadQuestData(
            data.currentWoodCount,
            data.woodQuestCompleted,
            data.campfireQuestCompleted,
            data.surviveNightQuestCompleted,
            data.allQuestsCompleted
        );
    }

    public static bool HasSaveFileOnDisk()
    {
        return File.Exists(JsonSavePath) || File.Exists(LegacyJsonSavePath) || File.Exists(BinarySavePath);
    }

    public bool HasSaveFile() => HasSaveFileOnDisk();

    private static void ApplyEntitySortingLayer(GameObject obj)
    {
        if (obj == null) return;
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
            sr.sortingLayerName = "Entity";
    }
}
