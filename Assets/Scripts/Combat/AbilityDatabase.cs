using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Data for a single ability. DamageType is defined elsewhere (e.g. DamageType.cs),
/// so we just reference it here.
/// </summary>
[System.Serializable]
public class AbilityData
{
    public string id;
    public string displayName;
    [TextArea] public string description;

    [Header("Core")]
    public float baseDamage = 10f;
    public float cooldown = 1f;
    public float projectileSpeed = 10f;
    public string projectilePrefabName;
    public DamageType damageType = DamageType.Physical;

    [Header("Behavior")]
    public float duration = 0f;        // lifetime for projectiles, etc.
    public int maxChains = 0;          // base chain count for this ability
    public int maxSplits = 0;          // base split count for this ability

    [Header("Casting")]
    [Tooltip("If true, this ability can be auto-cast by AutoAttackController or similar systems.")]
    public bool autoCast = true;

    [Header("Tags & Icon")]
    [Tooltip("Tags used for filtering/selection (e.g. 'bow', 'fire', 'aoe').")]
    public string[] tags;              // NOTE: string[] to match existing code expectations
    public string iconSpriteName;

    [Header("Weapon Scaling")]
    [Tooltip("If true, this ability's primary hit will use the wielder's weapon damage instead of baseDamage.")]
    public bool usesWeaponDamage = false;

    [Tooltip("Multiplier applied to the rolled weapon damage (1 = 100% weapon damage).")]
    public float weaponDamageMultiplier = 1f;

    [Header("Skill Level Scaling")]
    [Tooltip("Per-skill-level multiplier for baseDamage. 0.25 = +25% per level.")]
    public float levelScalingPerLevel = 0.25f;

    [Tooltip("Maximum number of times this ability can be leveled. 0 or less = unlimited.")]
    public int maxLevel = 5;
}

[System.Serializable]
public class AbilityListWrapper
{
    public List<AbilityData> abilities;
}

/// <summary>
/// Loads ability data from JSON and provides lookup helpers.
/// JSON root should look like:
/// { "abilities": [ { ... }, { ... } ] }
/// </summary>
public class AbilityDatabase : MonoBehaviour
{
    public static AbilityDatabase Instance { get; private set; }

    [Tooltip("JSON file describing all abilities.")]
    public TextAsset abilityJson;

    [Header("Fallback JSON files (StreamingAssets)")]
    [Tooltip("If abilityJson is not assigned, the database will attempt to load these files from StreamingAssets.")]
    public string[] streamingJsonFiles = { "abilities.json", "abilities_generic.json", "abilities_bow.json" };

    // Public list to match existing code that accesses AbilityDatabase.Instance.abilities
    [Tooltip("All loaded abilities from the JSON definition.")]
    public List<AbilityData> abilities = new List<AbilityData>();

    private Dictionary<string, AbilityData> _abilitiesById = new Dictionary<string, AbilityData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAbilitiesFromJson();
    }

    private void LoadAbilitiesFromJson()
    {
        _abilitiesById.Clear();
        abilities.Clear();

        bool loadedAny = false;

        if (abilityJson != null && !string.IsNullOrWhiteSpace(abilityJson.text))
        {
            loadedAny |= TryAddAbilitiesFromJson(abilityJson.text, "abilityJson TextAsset");
        }
        else
        {
            Debug.LogWarning("AbilityDatabase: No abilityJson assigned. Attempting to load from StreamingAssets.");
        }

        if (!loadedAny)
        {
            loadedAny |= LoadAbilitiesFromStreamingAssets();
        }

        if (!loadedAny)
        {
            Debug.LogError("AbilityDatabase: Failed to load any abilities. Check JSON configuration.");
            return;
        }

        Debug.Log($"AbilityDatabase: Loaded {_abilitiesById.Count} abilities from JSON.");
    }

    private bool LoadAbilitiesFromStreamingAssets()
    {
        bool anyLoaded = false;

        if (streamingJsonFiles == null || streamingJsonFiles.Length == 0)
            return false;

        foreach (string fileName in streamingJsonFiles)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                continue;

            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"AbilityDatabase: StreamingAssets file not found at {path}");
                continue;
            }

            string jsonText = System.IO.File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                Debug.LogWarning($"AbilityDatabase: StreamingAssets file {fileName} is empty.");
                continue;
            }

            if (TryAddAbilitiesFromJson(jsonText, fileName))
            {
                anyLoaded = true;
            }
        }

        return anyLoaded;
    }

    private bool TryAddAbilitiesFromJson(string jsonText, string sourceLabel)
    {
        AbilityListWrapper wrapper = JsonUtility.FromJson<AbilityListWrapper>(jsonText);
        if (wrapper == null || wrapper.abilities == null)
        {
            Debug.LogError($"AbilityDatabase: Failed to parse ability JSON from {sourceLabel}. Check the root object and field names.");
            return false;
        }

        bool addedAny = false;

        foreach (var ability in wrapper.abilities)
        {
            if (!TryRegisterAbility(ability, sourceLabel))
                continue;

            abilities.Add(ability);
            addedAny = true;
        }

        return addedAny;
    }

    private bool TryRegisterAbility(AbilityData ability, string sourceLabel)
    {
        if (ability == null)
            return false;

        if (string.IsNullOrEmpty(ability.id))
        {
            Debug.LogWarning($"AbilityDatabase: Found ability with empty id in {sourceLabel}, skipping.");
            return false;
        }

        if (_abilitiesById.ContainsKey(ability.id))
        {
            Debug.LogWarning($"AbilityDatabase: Duplicate ability id '{ability.id}' encountered in {sourceLabel}, skipping.");
            return false;
        }

        _abilitiesById.Add(ability.id, ability);
        return true;
    }

    public AbilityData GetAbilityById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        _abilitiesById.TryGetValue(id, out var ability);
        return ability;
    }

    public List<AbilityData> GetAllAbilities()
    {
        return new List<AbilityData>(abilities);
    }

    public List<AbilityData> GetAbilitiesWithTag(string tag)
    {
        List<AbilityData> result = new List<AbilityData>();
        if (string.IsNullOrEmpty(tag))
            return result;

        foreach (var ability in abilities)
        {
            if (ability == null || ability.tags == null)
                continue;

            // tags is string[]; use LINQ Any or simple loop
            if (ability.tags.Any(t => t == tag))
            {
                result.Add(ability);
            }
        }

        return result;
    }

    /// <summary>
    /// Loads the projectile prefab for a given ability from Resources/Projectiles.
    /// The ability's projectilePrefabName is appended to that path.
    /// </summary>
    public GameObject GetProjectilePrefab(AbilityData ability)
    {
        if (ability == null || string.IsNullOrEmpty(ability.projectilePrefabName))
            return null;

        string path = "Projectiles/" + ability.projectilePrefabName;
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"AbilityDatabase: Could not load projectile prefab at Resources/{path}");
        }

        return prefab;
    }
}