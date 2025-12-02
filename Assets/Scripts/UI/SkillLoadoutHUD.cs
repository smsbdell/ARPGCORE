using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SkillLoadoutHUD : MonoBehaviour
{
    [Header("References")]
    public RectTransform panelRoot;
    public RectTransform contentRoot;
    public SkillLoadoutEntryUI entryPrefab;
    public float entrySpacing = 6f;

    [Header("Tooltip")]
    public CanvasGroup tooltipGroup;
    public TMP_Text tooltipTitle;
    public TMP_Text tooltipBody;
    public float tooltipFadeSpeed = 12f;

    private PlayerSkills _playerSkills;
    private CharacterStats _stats;
    private readonly List<SkillLoadoutEntryUI> _spawnedEntries = new List<SkillLoadoutEntryUI>();
    private float _targetTooltipAlpha;

    private void OnEnable()
    {
        PlayerSkills.OnPlayerSkillsRegistered += HandleSkillsRegistered;
        TryAttachToPlayer();
    }

    private void OnDisable()
    {
        PlayerSkills.OnPlayerSkillsRegistered -= HandleSkillsRegistered;

        if (_playerSkills != null)
        {
            _playerSkills.OnSkillsChanged -= RebuildList;
        }
    }

    private void Update()
    {
        if (tooltipGroup != null)
        {
            tooltipGroup.alpha = Mathf.Lerp(tooltipGroup.alpha, _targetTooltipAlpha, Time.deltaTime * tooltipFadeSpeed);
        }
    }

    private void TryAttachToPlayer()
    {
        if (_playerSkills != null)
            return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
            return;

        PlayerSkills skills = playerObj.GetComponent<PlayerSkills>();
        HandleSkillsRegistered(skills);
    }

    private void HandleSkillsRegistered(PlayerSkills skills)
    {
        if (skills == null)
            return;

        if (_playerSkills != null)
        {
            _playerSkills.OnSkillsChanged -= RebuildList;
        }

        _playerSkills = skills;
        _stats = skills != null ? skills.GetComponent<CharacterStats>() : null;

        if (_playerSkills != null)
        {
            _playerSkills.OnSkillsChanged += RebuildList;
        }

        RebuildList();
    }

    private void ClearEntries()
    {
        foreach (SkillLoadoutEntryUI entry in _spawnedEntries)
        {
            if (entry != null)
            {
                Destroy(entry.gameObject);
            }
        }

        _spawnedEntries.Clear();
    }

    private void RebuildList()
    {
        if (contentRoot == null || entryPrefab == null)
            return;

        ClearEntries();

        if (_playerSkills == null)
            return;

        if (AbilityDatabase.Instance != null)
        {
            foreach (AbilityData ability in AbilityDatabase.Instance.abilities)
            {
                int level = _playerSkills.GetActiveLevel(ability.id);
                if (level <= 0)
                    continue;

                CreateAbilityEntry(ability, level);
            }
        }

        if (PassiveDatabase.Instance != null)
        {
            foreach (PassiveDefinition passive in PassiveDatabase.Instance.passives)
            {
                int level = _playerSkills.GetPassiveLevel(passive.id);
                if (level <= 0)
                    continue;

                CreatePassiveEntry(passive, level);
            }
        }

        LayoutEntries();
    }

    private void CreateAbilityEntry(AbilityData ability, int level)
    {
        SkillLoadoutEntryUI entry = Instantiate(entryPrefab, contentRoot);
        entry.gameObject.SetActive(true);

        Sprite icon = LoadAbilityIcon(ability);
        entry.Configure(ability.displayName, level, icon, () => ShowAbilityTooltip(ability, level), HideTooltip);

        _spawnedEntries.Add(entry);
    }

    private void LayoutEntries()
    {
        if (contentRoot == null)
            return;

        float cursor = 0f;

        for (int i = 0; i < _spawnedEntries.Count; i++)
        {
            SkillLoadoutEntryUI entry = _spawnedEntries[i];
            if (entry == null)
                continue;

            RectTransform rt = entry.GetComponent<RectTransform>();
            if (rt == null)
                continue;

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);

            float height = rt.sizeDelta.y;
            rt.anchoredPosition = new Vector2(0f, -cursor);
            rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
            rt.offsetMax = new Vector2(0f, rt.offsetMax.y);

            cursor += height + entrySpacing;
        }

        contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cursor);
    }

    private void CreatePassiveEntry(PassiveDefinition passive, int level)
    {
        SkillLoadoutEntryUI entry = Instantiate(entryPrefab, contentRoot);
        entry.gameObject.SetActive(true);

        Sprite icon = LoadPassiveIcon(passive);
        entry.Configure(passive.displayName, level, icon, () => ShowPassiveTooltip(passive), HideTooltip);

        _spawnedEntries.Add(entry);
    }

    private void ShowAbilityTooltip(AbilityData ability, int level)
    {
        if (tooltipTitle != null)
        {
            tooltipTitle.text = ability.displayName;
        }

        if (tooltipBody != null)
        {
            tooltipBody.text = SkillDescriptionUtility.BuildAbilityTooltip(ability, level, _stats, _playerSkills);
        }

        SetTooltipVisible(true);
    }

    private void ShowPassiveTooltip(PassiveDefinition passive)
    {
        if (tooltipTitle != null)
        {
            tooltipTitle.text = passive.displayName;
        }

        if (tooltipBody != null)
        {
            tooltipBody.text = SkillDescriptionUtility.BuildPassiveDescription(passive.id, _playerSkills, _stats, false);
        }

        SetTooltipVisible(true);
    }

    private void HideTooltip()
    {
        SetTooltipVisible(false);
    }

    private void SetTooltipVisible(bool visible)
    {
        _targetTooltipAlpha = visible ? 1f : 0f;
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
}
