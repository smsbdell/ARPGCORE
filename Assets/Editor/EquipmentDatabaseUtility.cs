#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EquipmentDatabaseUtility
{
    private static readonly string[] TagSeparators = { "|" };

    [MenuItem("ARPG/Equipment/Generate JSON From CSV")]
    public static void GenerateJsonFromCsv()
    {
        string csvPath = EditorUtility.OpenFilePanel("Select equipment CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            Debug.LogWarning("Equipment CSV is empty or missing data rows.");
            return;
        }

        string[] headers = SplitCsvLine(lines[0]);
        List<EquipmentRecord> records = new List<EquipmentRecord>();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] columns = SplitCsvLine(lines[i]);
            EquipmentRecord record = new EquipmentRecord();
            StatModifier modifier = new StatModifier();

            for (int c = 0; c < headers.Length && c < columns.Length; c++)
            {
                string header = headers[c].Trim().ToLowerInvariant();
                string value = columns[c].Trim();

                switch (header)
                {
                    case "id":
                        record.id = value;
                        break;
                    case "displayname":
                        record.displayName = value;
                        break;
                    case "description":
                        record.description = value;
                        break;
                    case "slot":
                        if (Enum.TryParse(value, true, out EquipmentSlot slot))
                        {
                            record.slot = slot;
                        }
                        break;
                    case "tags":
                        record.tags = value.Split(TagSeparators, StringSplitOptions.RemoveEmptyEntries);
                        break;
                    case "iconresourcepath":
                        record.iconResourcePath = value;
                        break;

                    case "maxhealth":
                        modifier.maxHealth = ParseFloat(value);
                        break;
                    case "movespeed":
                        modifier.moveSpeed = ParseFloat(value);
                        break;
                    case "basedamage":
                        modifier.baseDamage = ParseFloat(value);
                        break;
                    case "critchance":
                        modifier.critChance = ParseFloat(value);
                        break;
                    case "critmultiplier":
                        modifier.critMultiplier = ParseFloat(value);
                        break;
                    case "attackspeedmultiplier":
                        modifier.attackSpeedMultiplier = ParseFloat(value);
                        break;
                    case "projectilecount":
                        modifier.projectileCount = ParseInt(value);
                        break;
                    case "projectilespreadangle":
                        modifier.projectileSpreadAngle = ParseFloat(value);
                        break;
                    case "weaponattackspeed":
                        modifier.weaponAttackSpeed = ParseFloat(value);
                        break;
                    case "chaincount":
                        modifier.chainCount = ParseInt(value);
                        break;
                    case "splitcount":
                        modifier.splitCount = ParseInt(value);
                        break;
                    case "armor":
                        modifier.armor = ParseFloat(value);
                        break;
                    case "dodgechance":
                        modifier.dodgeChance = ParseFloat(value);
                        break;
                    case "xpgainmultiplier":
                        modifier.xpGainMultiplier = ParseFloat(value);
                        break;
                    case "cooldownreduction":
                        modifier.cooldownReduction = ParseFloat(value);
                        break;
                }
            }

            record.modifiers = modifier;
            records.Add(record);
        }

        EquipmentRecordCollection wrapper = new EquipmentRecordCollection { items = records.ToArray() };

        string savePath = EditorUtility.SaveFilePanelInProject(
            "Save Equipment Database JSON",
            "equipment_database",
            "json",
            "Choose where to save the generated JSON file." );

        if (string.IsNullOrEmpty(savePath))
            return;

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(savePath, json);
        AssetDatabase.Refresh();
        Debug.Log($"Equipment database exported to {savePath}.");
    }

    private static string[] SplitCsvLine(string line)
    {
        return line.Split(',');
    }

    private static float ParseFloat(string value)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
            ? result
            : 0f;
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
            ? result
            : 0;
    }
}
#endif
