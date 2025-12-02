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

                if (TryProcessCoreField(header, value, record))
                    continue;

                TryProcessStatField(header, value, modifier);
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

    [MenuItem("ARPG/Equipment/Migrate Legacy Equipment JSON")]
    public static void MigrateLegacyJson()
    {
        string jsonPath = EditorUtility.OpenFilePanel("Select legacy equipment JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(jsonPath))
            return;

        string json = File.ReadAllText(jsonPath);
        EquipmentRecordCollection collection = JsonUtility.FromJson<EquipmentRecordCollection>(json);
        if (collection?.items == null)
        {
            Debug.LogWarning("Selected file did not contain any equipment items.");
            return;
        }

        foreach (EquipmentRecord record in collection.items)
        {
            if (record.modifiers == null)
                record.modifiers = new StatModifier();

            StatModifierMigrationUtility.MigrateLegacyValues(record.modifiers);
            StatModifierMigrationUtility.ClearLegacyFields(record.modifiers);
        }

        string savePath = EditorUtility.SaveFilePanelInProject(
            "Save Migrated Equipment JSON",
            "equipment_database_migrated",
            "json",
            "Choose where to save the migrated JSON file.");

        if (string.IsNullOrEmpty(savePath))
            return;

        string migratedJson = JsonUtility.ToJson(collection, true);
        File.WriteAllText(savePath, migratedJson);
        AssetDatabase.Refresh();
        Debug.Log($"Migrated equipment JSON saved to {savePath}.");
    }

    private static string[] SplitCsvLine(string line)
    {
        return line.Split(',');
    }

    private static bool TryProcessCoreField(string header, string value, EquipmentRecord record)
    {
        switch (header)
        {
            case "id":
                record.id = value;
                return true;
            case "displayname":
                record.displayName = value;
                return true;
            case "description":
                record.description = value;
                return true;
            case "slot":
                if (Enum.TryParse(value, true, out EquipmentSlot slot))
                {
                    record.slot = slot;
                }
                return true;
            case "tags":
                record.tags = value.Split(TagSeparators, StringSplitOptions.RemoveEmptyEntries);
                return true;
            case "iconresourcepath":
                record.iconResourcePath = value;
                return true;
            default:
                return false;
        }
    }

    private static void TryProcessStatField(string header, string rawValue, StatModifier modifier)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return;

        string statId = header;
        StatOperation operation = StatOperation.Default;

        string[] parts = header.Split(':');
        if (parts.Length > 1)
        {
            statId = parts[0];
            if (Enum.TryParse(parts[1], true, out StatOperation parsedOp))
            {
                operation = parsedOp;
            }
        }

        float value = ParseFloat(rawValue);
        if (Mathf.Approximately(value, 0f))
            return;

        modifier.AddEntry(statId, value, operation);
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
