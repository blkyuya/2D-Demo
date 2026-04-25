using System;
using System.Collections.Generic;

// 早期版本存档结构（历史遗留字段布局）；当前游戏统一用 SaveData + save_game.json（JsonUtility）
[Serializable]
public class GameSaveData
{
    public float playerPosX;
    public float playerPosY;

    public int playerHealth;
    public int playerHunger;

    public int woodCount;
    public int stoneCount;
    public int berryCount;

    public float timeNormalized;

    public int currentWoodQuestCount;
    public bool woodQuestCompleted;
    public bool campfireQuestCompleted;
    public bool surviveNightQuestCompleted;
    public bool allQuestsCompleted;

    public List<CampfireSaveData> campfires = new List<CampfireSaveData>();
}
