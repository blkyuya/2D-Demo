using System;
using System.IO;
using UnityEngine;

// 旧版二进制存档读取（仅用于迁移）：文件头魔数 "P2DS" + 版本号，缺少的字段读档后由 SaveManager 补默认。
// 新存档一律写 JSON，不再调用 WriteToFile。
public static class BinarySaveIO
{
    private static readonly byte[] MagicBytes = { 0x50, 0x32, 0x44, 0x53 };

    private const int FileVersionV1 = 1;
    private const int CurrentFileVersion = 2;

    // 保留供极少数工具或单元测试使用；游戏内保存请走 SaveManager 的 JSON
    public static void WriteSaveData(BinaryWriter w, SaveData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        for (int i = 0; i < MagicBytes.Length; i++)
            w.Write(MagicBytes[i]);

        w.Write(CurrentFileVersion);

        w.Write(data.playerPosX);
        w.Write(data.playerPosY);
        w.Write(data.playerPosZ);

        w.Write(data.playerHealth);
        w.Write(data.playerHunger);

        w.Write(data.currentTimeNormalized);

        WriteInventory(w, data.inventory);

        CampfireSaveData[] arr = data.campfires;
        int n = arr != null ? arr.Length : 0;
        w.Write(n);
        for (int i = 0; i < n; i++)
        {
            CampfireSaveData c = arr[i];
            if (c == null) c = new CampfireSaveData();
            w.Write(c.posX);
            w.Write(c.posY);
            w.Write(c.posZ);
            w.Write(c.fuel);
        }

        w.Write(data.currentWoodCount);
        w.Write(data.woodQuestCompleted);
        w.Write(data.campfireQuestCompleted);
        w.Write(data.surviveNightQuestCompleted);
        w.Write(data.allQuestsCompleted);

        WriteEquippedSlots(w, data);
    }

    private static void WriteEquippedSlots(BinaryWriter w, SaveData data)
    {
        int[] slots = data.equippedItemTypeOrMinusOne;
        if (slots == null || slots.Length != 6)
            slots = new int[6] { -1, -1, -1, -1, -1, -1 };

        for (int i = 0; i < 6; i++)
            w.Write(slots[i]);
    }

    private static void WriteInventory(BinaryWriter w, InventorySaveData inv)
    {
        if (inv == null)
        {
            w.Write(false);
            return;
        }

        w.Write(true);
        w.Write(inv.woodCount);
        w.Write(inv.stoneCount);
        w.Write(inv.berryCount);
    }

    public static SaveData ReadSaveData(BinaryReader r)
    {
        byte b0 = r.ReadByte();
        byte b1 = r.ReadByte();
        byte b2 = r.ReadByte();
        byte b3 = r.ReadByte();
        if (b0 != MagicBytes[0] || b1 != MagicBytes[1] || b2 != MagicBytes[2] || b3 != MagicBytes[3])
            throw new InvalidDataException("存档魔数不匹配");

        int version = r.ReadInt32();
        if (version != FileVersionV1 && version != CurrentFileVersion)
            throw new InvalidDataException("不支持的存档版本: " + version);

        SaveData data = new SaveData();

        data.playerPosX = r.ReadSingle();
        data.playerPosY = r.ReadSingle();
        data.playerPosZ = r.ReadSingle();

        data.playerHealth = r.ReadInt32();
        data.playerHunger = r.ReadInt32();

        data.currentTimeNormalized = r.ReadSingle();

        data.inventory = ReadInventory(r);

        int campfireCount = r.ReadInt32();
        data.campfires = new CampfireSaveData[campfireCount];
        for (int i = 0; i < campfireCount; i++)
        {
            CampfireSaveData c = new CampfireSaveData();
            c.posX = r.ReadSingle();
            c.posY = r.ReadSingle();
            c.posZ = r.ReadSingle();
            c.fuel = r.ReadSingle();
            data.campfires[i] = c;
        }

        data.currentWoodCount = r.ReadInt32();
        data.woodQuestCompleted = r.ReadBoolean();
        data.campfireQuestCompleted = r.ReadBoolean();
        data.surviveNightQuestCompleted = r.ReadBoolean();
        data.allQuestsCompleted = r.ReadBoolean();

        if (version >= CurrentFileVersion)
            data.equippedItemTypeOrMinusOne = ReadEquippedSlots(r);
        else
            data.equippedItemTypeOrMinusOne = new int[6] { -1, -1, -1, -1, -1, -1 };

        data.saveFormatVersion = 1;
        return data;
    }

    private static int[] ReadEquippedSlots(BinaryReader r)
    {
        int[] slots = new int[6];
        for (int i = 0; i < 6; i++)
            slots[i] = r.ReadInt32();
        return slots;
    }

    private static InventorySaveData ReadInventory(BinaryReader r)
    {
        bool hasInv = r.ReadBoolean();
        if (!hasInv)
            return null;

        InventorySaveData inv = new InventorySaveData();
        inv.woodCount  = r.ReadInt32();
        inv.stoneCount = r.ReadInt32();
        inv.berryCount = r.ReadInt32();
        return inv;
    }

    public static void WriteToFile(string path, SaveData data)
    {
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var w = new BinaryWriter(fs))
        {
            WriteSaveData(w, data);
        }
    }

    public static SaveData ReadFromFile(string path)
    {
        try
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var r = new BinaryReader(fs))
            {
                return ReadSaveData(r);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[BinarySaveIO] 读取失败: " + ex.Message);
            return null;
        }
    }
}
