using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EquipmentRecord
{
    public string id;
    public string displayName;
    [TextArea]
    public string description;
    public string iconResourcePath;
    public EquipmentSlot slot;
    public string[] tags;
    public StatModifier modifiers = new StatModifier();
}

[Serializable]
public class EquipmentRecordCollection
{
    public EquipmentRecord[] items;
}

[CreateAssetMenu(menuName = "ARPG/Equipment Database", fileName = "EquipmentDatabase")]
public class EquipmentDatabase : ScriptableObject
{
    [Tooltip("JSON TextAsset containing an EquipmentRecordCollection root object.")]
    public TextAsset dataFile;

    private readonly Dictionary<string, EquipmentItem> _lookup = new Dictionary<string, EquipmentItem>();
    private readonly List<EquipmentItem> _items = new List<EquipmentItem>();
    private bool _isLoaded;

    public IReadOnlyList<EquipmentItem> Items
    {
        get
        {
            EnsureLoaded();
            return _items;
        }
    }

    private void OnEnable()
    {
        _isLoaded = false;
        _lookup.Clear();
        _items.Clear();
    }

    private void EnsureLoaded()
    {
        if (_isLoaded)
            return;

        _isLoaded = true;

        if (dataFile == null)
        {
            dataFile = Resources.Load<TextAsset>("Data/equipment_database");
            if (dataFile == null)
            {
                Debug.LogWarning("EquipmentDatabase: no data file assigned.");
                return;
            }
        }

        EquipmentRecordCollection wrapper = JsonUtility.FromJson<EquipmentRecordCollection>(dataFile.text);
        if (wrapper == null || wrapper.items == null)
        {
            Debug.LogWarning("EquipmentDatabase: data file did not contain any items.");
            return;
        }

        foreach (EquipmentRecord record in wrapper.items)
        {
            if (string.IsNullOrEmpty(record.id))
            {
                Debug.LogWarning("EquipmentDatabase: encountered record with empty id.");
                continue;
            }

            if (_lookup.ContainsKey(record.id))
            {
                Debug.LogWarning($"EquipmentDatabase: duplicate id '{record.id}' detected. Skipping this entry.");
                continue;
            }

            EquipmentItem instance = ScriptableObject.CreateInstance<EquipmentItem>();
            instance.id = record.id;
            instance.displayName = record.displayName;
            instance.description = record.description;
            instance.slot = record.slot;
            instance.tags = record.tags ?? Array.Empty<string>();
            instance.modifiers = record.modifiers ?? new StatModifier();
            StatModifierMigrationUtility.MigrateLegacyValues(instance.modifiers);

            if (!string.IsNullOrEmpty(record.iconResourcePath))
            {
                instance.icon = Resources.Load<Sprite>(record.iconResourcePath);
            }

            _lookup.Add(instance.id, instance);
            _items.Add(instance);
        }
    }

    public bool TryGetItem(string id, out EquipmentItem item)
    {
        EnsureLoaded();
        return _lookup.TryGetValue(id, out item);
    }

    public EquipmentItem GetItemOrDefault(string id)
    {
        return TryGetItem(id, out EquipmentItem item) ? item : null;
    }
}
