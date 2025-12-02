using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum SkillChoiceKind
{
    NewActive,
    LevelUpActive,
    NewPassive,
    LevelUpPassive
}

public class SkillChoice
{
    public SkillChoiceKind kind;
    public string id;
    public string displayName;
    public string description;
    public string[] tags;
    public Sprite icon;
}

public class SkillSelectionManager : MonoBehaviour
{
    public static SkillSelectionManager Instance { get; private set; }

    [Header("UI")]
    public GameObject panelRoot;
    public TMP_Text[] optionNameTexts;
    public TMP_Text[] optionDescriptionTexts;
    public Button[] optionButtons;
    public Image[] optionIconImages;

    private PlayerSkills _playerSkills;
    private CharacterStats _stats;
    private PlayerEquipment _playerEquipment;

    private readonly List<SkillChoice> _currentChoices = new List<SkillChoice>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void RegisterPlayerSkills(PlayerSkills skills)
    {
        _playerSkills = skills;
        if (skills != null)
        {
            _stats = skills.GetComponent<CharacterStats>();
            _playerEquipment = skills.GetComponent<PlayerEquipment>();
        }
    }

    public void ShowInitialChoices()
    {
        if (AbilityDatabase.Instance == null)
        {
            Debug.LogError("SkillSelectionManager: AbilityDatabase.Instance is null.");
            return;
        }

        if (_playerSkills == null)
        {
            Debug.LogWarning("SkillSelectionManager: no PlayerSkills registered; cannot show initial choices.");
            return;
        }

        List<SkillChoice> pool = new List<SkillChoice>();

        foreach (AbilityData ability in AbilityDatabase.Instance.abilities)
        {
            if (_playerSkills.HasActive(ability.id))
                continue;

            if (!IsAbilityAllowedByEquipmentForNewAcquisition(ability))
                continue;

            Sprite icon = LoadAbilityIcon(ability);

            pool.Add(new SkillChoice
            {
                kind = SkillChoiceKind.NewActive,
                id = ability.id,
                displayName = ability.displayName,
                description = $"New skill: {ability.displayName}",
                tags = ability.tags,
                icon = icon
            });
        }

        if (pool.Count == 0)
        {
            Debug.LogWarning("SkillSelectionManager: no abilities passed equipment filter; falling back to unfiltered pool.");

            foreach (AbilityData ability in AbilityDatabase.Instance.abilities)
            {
                if (_playerSkills.HasActive(ability.id))
                    continue;

                Sprite icon = LoadAbilityIcon(ability);

                pool.Add(new SkillChoice
                {
                    kind = SkillChoiceKind.NewActive,
                    id = ability.id,
                    displayName = ability.displayName,
                    description = $"New skill: {ability.displayName}",
                    tags = ability.tags,
                    icon = icon
                });
            }
        }

        if (pool.Count == 0)
        {
            Debug.LogWarning("SkillSelectionManager: no abilities available at all for initial selection.");
            return;
        }

        ShowChoicesFromPool(pool);
    }

    public void ShowLevelUpChoices()
    {
        if (_playerSkills == null)
        {
            Debug.LogWarning("SkillSelectionManager: no PlayerSkills registered; cannot show level-up choices.");
            return;
        }

        List<SkillChoice> pool = new List<SkillChoice>();

        if (AbilityDatabase.Instance != null)
        {
            foreach (AbilityData ability in AbilityDatabase.Instance.abilities)
            {
                bool has = _playerSkills.HasActive(ability.id);
                int level = _playerSkills.GetActiveLevel(ability.id);

                Sprite icon = LoadAbilityIcon(ability);

                if (!has)
                {
                    if (!IsAbilityAllowedByEquipmentForNewAcquisition(ability))
                        continue;

                    pool.Add(new SkillChoice
                    {
                        kind = SkillChoiceKind.NewActive,
                        id = ability.id,
                        displayName = ability.displayName,
                        description = $"Learn new skill: {ability.displayName}",
                        tags = ability.tags,
                        icon = icon
                    });
                }
                else if (ability.maxLevel <= 0 || level < ability.maxLevel)
                {
                    pool.Add(new SkillChoice
                    {
                        kind = SkillChoiceKind.LevelUpActive,
                        id = ability.id,
                        displayName = ability.displayName + " +",
                        description = $"Level up {ability.displayName}",
                        tags = ability.tags,
                        icon = icon
                    });
                }
            }
        }

        if (PassiveDatabase.Instance != null)
        {
            foreach (PassiveDefinition passive in PassiveDatabase.Instance.passives)
            {
                bool hasPassive = _playerSkills.HasPassive(passive.id);
                int level = _playerSkills.GetPassiveLevel(passive.id);

                bool requiresAbility = !string.IsNullOrEmpty(passive.targetAbilityId);
                if (requiresAbility && !_playerSkills.HasActive(passive.targetAbilityId))
                    continue;

                Sprite icon = LoadPassiveIcon(passive);

                if (!hasPassive)
                {
                    pool.Add(new SkillChoice
                    {
                        kind = SkillChoiceKind.NewPassive,
                        id = passive.id,
                        displayName = passive.displayName,
                        description = $"New passive: {passive.description}",
                        tags = passive.tags,
                        icon = icon
                    });
                }
                else
                {
                    pool.Add(new SkillChoice
                    {
                        kind = SkillChoiceKind.LevelUpPassive,
                        id = passive.id,
                        displayName = passive.displayName + " +",
                        description = $"Level up {passive.displayName}",
                        tags = passive.tags,
                        icon = icon
                    });
                }
            }
        }

        if (pool.Count == 0)
        {
            Debug.LogWarning("SkillSelectionManager: no skills/passives available to offer at level up.");
            return;
        }

        ShowChoicesFromPool(pool);
    }

    private void ShowChoicesFromPool(List<SkillChoice> pool)
    {
        if (panelRoot == null || optionButtons == null || optionButtons.Length == 0)
        {
            Debug.LogError("SkillSelectionManager: UI references not set up.");
            return;
        }

        _currentChoices.Clear();

        int choicesToShow = Mathf.Min(3, pool.Count);

        List<int> availableIndexes = new List<int>();
        for (int i = 0; i < pool.Count; i++)
            availableIndexes.Add(i);

        for (int i = 0; i < choicesToShow; i++)
        {
            int pickIndex = UnityEngine.Random.Range(0, availableIndexes.Count);
            int poolIndex = availableIndexes[pickIndex];
            availableIndexes.RemoveAt(pickIndex);

            _currentChoices.Add(pool[poolIndex]);
        }

        Time.timeScale = 0f;
        panelRoot.SetActive(true);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < _currentChoices.Count)
            {
                SkillChoice choice = _currentChoices[i];

                if (optionNameTexts != null && i < optionNameTexts.Length && optionNameTexts[i] != null)
                {
                    optionNameTexts[i].text = choice.displayName;
                }

                if (optionDescriptionTexts != null && i < optionDescriptionTexts.Length && optionDescriptionTexts[i] != null)
                {
                    string tagsText = "";
                    if (choice.tags != null && choice.tags.Length > 0)
                    {
                        tagsText = "\n[" + string.Join(", ", choice.tags) + "]";
                    }

                    optionDescriptionTexts[i].text = BuildChoiceDescription(choice) + tagsText;
                }

                if (optionIconImages != null && i < optionIconImages.Length && optionIconImages[i] != null)
                {
                    if (choice.icon != null)
                    {
                        optionIconImages[i].sprite = choice.icon;
                        optionIconImages[i].enabled = true;
                    }
                    else
                    {
                        optionIconImages[i].sprite = null;
                        optionIconImages[i].enabled = false;
                    }
                }

                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].onClick.RemoveAllListeners();
                int capturedIndex = i;
                optionButtons[i].onClick.AddListener(() => OnChoiceSelected(capturedIndex));
            }
            else
            {
                if (optionButtons[i] != null)
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
                if (optionIconImages != null && i < optionIconImages.Length && optionIconImages[i] != null)
                {
                    optionIconImages[i].enabled = false;
                }
            }
        }
    }

    private void OnChoiceSelected(int index)
    {
        if (index < 0 || index >= _currentChoices.Count)
            return;

        SkillChoice choice = _currentChoices[index];
        ApplyChoice(choice);
        ClosePanel();
    }

    private void ApplyChoice(SkillChoice choice)
    {
        if (_playerSkills == null)
        {
            Debug.LogWarning("SkillSelectionManager: no PlayerSkills registered; cannot apply choice.");
            return;
        }

        switch (choice.kind)
        {
            case SkillChoiceKind.NewActive:
                _playerSkills.AddActiveSkill(choice.id);
                break;
            case SkillChoiceKind.LevelUpActive:
                _playerSkills.LevelUpActiveSkill(choice.id);
                break;
            case SkillChoiceKind.NewPassive:
                _playerSkills.AddPassive(choice.id);
                break;
            case SkillChoiceKind.LevelUpPassive:
                _playerSkills.LevelUpPassive(choice.id);
                break;
        }
    }

    private void ClosePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    private Sprite LoadAbilityIcon(AbilityData ability)
    {
        if (ability == null || string.IsNullOrEmpty(ability.iconSpriteName))
            return null;

        Sprite icon = Resources.Load<Sprite>("UI/SkillIcons/" + ability.iconSpriteName);

        if (icon == null)
        {
            icon = Resources.Load<Sprite>("Projectiles/" + ability.iconSpriteName);
        }

        return icon;
    }

    private Sprite LoadPassiveIcon(PassiveDefinition passive)
    {
        if (passive == null || string.IsNullOrEmpty(passive.iconSpriteName))
            return null;

        Sprite icon = Resources.Load<Sprite>("UI/SkillIcons/" + passive.iconSpriteName);
        if (icon == null)
        {
            icon = Resources.Load<Sprite>("Projectiles/" + passive.iconSpriteName);
        }

        return icon;
    }

    private string BuildChoiceDescription(SkillChoice choice)
    {
        switch (choice.kind)
        {
            case SkillChoiceKind.NewActive:
                return BuildNewActiveDescription(choice);
            case SkillChoiceKind.LevelUpActive:
                return BuildLevelUpActiveDescription(choice);
            case SkillChoiceKind.NewPassive:
                return BuildNewPassiveDescription(choice);
            case SkillChoiceKind.LevelUpPassive:
                return BuildLevelUpPassiveDescription(choice);
            default:
                return choice.description;
        }
    }

    private string BuildNewActiveDescription(SkillChoice choice)
    {
        if (AbilityDatabase.Instance == null)
            return choice.description;

        AbilityData ability = AbilityDatabase.Instance.GetAbilityById(choice.id);
        if (ability == null || _stats == null)
            return choice.description;

        SkillEvaluationResult next = SkillDescriptionUtility.EvaluateAbility(ability, 1, _stats, _playerSkills);

        string secondaryLine = Mathf.Abs(next.secondaryDamage - next.primaryDamage) > 0.01f
            ? $"Secondary: {next.secondaryDamage:F0}\n"
            : string.Empty;

        return $"New skill: {ability.displayName}\n" +
               $"Damage (Lv1): {next.primaryDamage:F0}\n" +
               secondaryLine +
               $"Effective CD: {next.effectiveCooldown:F2}s";
    }

    private string BuildLevelUpActiveDescription(SkillChoice choice)
    {
        if (AbilityDatabase.Instance == null || _playerSkills == null)
            return choice.description;

        AbilityData ability = AbilityDatabase.Instance.GetAbilityById(choice.id);
        if (ability == null || _stats == null)
            return choice.description;

        int currentLevel = Mathf.Max(1, _playerSkills.GetActiveLevel(ability.id));
        int nextLevel = currentLevel + 1;

        SkillEvaluationResult cur = SkillDescriptionUtility.EvaluateAbility(ability, currentLevel, _stats, _playerSkills);
        SkillEvaluationResult nxt = SkillDescriptionUtility.EvaluateAbility(ability, nextLevel, _stats, _playerSkills);

        return $"Level up {ability.displayName} (Lv {currentLevel} → {nextLevel})\n" +
               $"Damage: {cur.primaryDamage:F0} → {nxt.primaryDamage:F0}\n" +
               $"Effective CD: {cur.effectiveCooldown:F2}s";
    }

    private string BuildNewPassiveDescription(SkillChoice choice)
    {
        return BuildPassiveDescription(choice.id, true);
    }

    private string BuildLevelUpPassiveDescription(SkillChoice choice)
    {
        return BuildPassiveDescription(choice.id, true);
    }

    private string BuildPassiveDescription(string passiveId, bool isLevelUp)
    {
        return SkillDescriptionUtility.BuildPassiveDescription(passiveId, _playerSkills, _stats, isLevelUp);
    }

    private bool IsAbilityAllowedByEquipmentForNewAcquisition(AbilityData ability)
    {
        if (ability == null)
            return false;

        if (_playerEquipment == null || _playerEquipment.mainHand == null)
            return true;

        string[] weaponTags = _playerEquipment.mainHand.tags;
        if (weaponTags == null || weaponTags.Length == 0)
            return true;

        if (ability.tags == null || ability.tags.Length == 0)
            return false;

        // Normalize tags once to avoid repeated trimming and to allow case-insensitive lookup.
        List<string> abilityTags = new List<string>();
        foreach (string at in ability.tags)
        {
            if (string.IsNullOrWhiteSpace(at))
                continue;

            abilityTags.Add(at.Trim());
        }

        if (abilityTags.Count == 0)
            return false;

        // Weapon-specific requirement tags (e.g., bow) should gate selection.
        List<string> weaponRequirementTags = new List<string>();
        foreach (string wt in weaponTags)
        {
            if (string.IsNullOrWhiteSpace(wt))
                continue;

            string weaponTag = wt.Trim();
            if (_weaponRequirementTags.Contains(weaponTag))
                weaponRequirementTags.Add(weaponTag);
        }

        // If we found explicit weapon requirement tags, require at least one match.
        if (weaponRequirementTags.Count > 0)
        {
            foreach (string requiredTag in weaponRequirementTags)
            {
                foreach (string abilityTag in abilityTags)
                {
                    if (string.Equals(requiredTag, abilityTag, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        // Fallback: if weapon has no explicit requirement tags, allow any overlap.
        foreach (string wt in weaponTags)
        {
            if (string.IsNullOrWhiteSpace(wt))
                continue;

            string weaponTag = wt.Trim();

            foreach (string abilityTag in abilityTags)
            {
                if (string.Equals(weaponTag, abilityTag, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private static readonly HashSet<string> _weaponRequirementTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "bow", "ranged", "melee", "sword", "axe", "mace", "dagger", "staff", "wand", "spear", "crossbow", "gun", "unarmed"
    };
}