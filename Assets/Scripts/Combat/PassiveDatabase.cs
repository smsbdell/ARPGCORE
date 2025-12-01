using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class PassiveDefinition
{
    public string id;
    public string displayName;
    public string description;
    public string[] tags;
    public string iconSpriteName;

    [Tooltip("Optional: if set, this passive only appears when the player has this ability, and should only affect that ability.")]
    public string targetAbilityId;
}

[System.Serializable]
public class PassiveListWrapper
{
    public List<PassiveDefinition> passives;
}

public class PassiveDatabase : MonoBehaviour
{
    public static PassiveDatabase Instance { get; private set; }

    [Header("JSON File (in StreamingAssets)")]
    public string jsonFileName = "passives.json";

    [Header("Runtime")]
    public List<PassiveDefinition> passives = new List<PassiveDefinition>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadPassivesFromJson();
    }

    private void LoadPassivesFromJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, jsonFileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"PassiveDatabase: JSON file not found at {path}");
            return;
        }

        string jsonText = File.ReadAllText(path);
        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.LogWarning($"PassiveDatabase: JSON file {jsonFileName} is empty.");
            return;
        }

        PassiveListWrapper wrapper = JsonUtility.FromJson<PassiveListWrapper>(jsonText);
        if (wrapper == null || wrapper.passives == null)
        {
            Debug.LogWarning($"PassiveDatabase: JSON file {jsonFileName} did not deserialize correctly.");
            return;
        }

        passives = wrapper.passives;

        Debug.Log($"PassiveDatabase: loaded {passives.Count} passives from {jsonFileName}.");
    }

    public PassiveDefinition GetPassiveById(string id)
    {
        return passives.Find(p => p.id == id);
    }
}