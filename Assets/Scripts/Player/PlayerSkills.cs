using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(AutoAttackController))]
public class PlayerSkills : MonoBehaviour
{
    private CharacterStats _stats;
    private AutoAttackController _autoAttack;

    private Dictionary<string, int> _activeLevels = new Dictionary<string, int>();
    private Dictionary<string, int> _passiveLevels = new Dictionary<string, int>();

    // Ability-specific modifiers, e.g. extra chains for fireball only.
    private Dictionary<string, int> _abilityChainBonus = new Dictionary<string, int>();

    public static event Action<PlayerSkills> OnPlayerSkillsRegistered;
    public event Action OnSkillsChanged;

    private void Awake()
    {
        _stats = GetComponent<CharacterStats>();
        _autoAttack = GetComponent<AutoAttackController>();
    }

    private void Start()
    {
        if (SkillSelectionManager.Instance != null)
        {
            SkillSelectionManager.Instance.RegisterPlayerSkills(this);
            SkillSelectionManager.Instance.ShowInitialChoices();
        }

        OnPlayerSkillsRegistered?.Invoke(this);
        NotifySkillsChanged();
    }

    #region Active skills

    public bool HasActive(string abilityId)
    {
        return _activeLevels.ContainsKey(abilityId);
    }

    public int GetActiveLevel(string abilityId)
    {
        if (_activeLevels.TryGetValue(abilityId, out int level))
            return level;
        return 0;
    }

    public void AddActiveSkill(string abilityId)
    {
        if (HasActive(abilityId))
        {
            Debug.LogWarning($"PlayerSkills: active ability '{abilityId}' already known.");
            return;
        }

        _activeLevels[abilityId] = 1;

        if (_autoAttack != null)
        {
            _autoAttack.AddEquippedAbility(abilityId);
        }

        Debug.Log($"PlayerSkills: learned new active ability '{abilityId}'.");

        NotifySkillsChanged();
    }

    public void LevelUpActiveSkill(string abilityId)
    {
        if (!HasActive(abilityId))
        {
            Debug.LogWarning($"PlayerSkills: can't level active '{abilityId}' that player doesn't have.");
            return;
        }

        _activeLevels[abilityId] += 1;
        Debug.Log($"PlayerSkills: leveled active ability '{abilityId}' to {_activeLevels[abilityId]}.");

        NotifySkillsChanged();
    }

    #endregion

    #region Passive skills

    public bool HasPassive(string passiveId)
    {
        return _passiveLevels.ContainsKey(passiveId);
    }

    public int GetPassiveLevel(string passiveId)
    {
        if (_passiveLevels.TryGetValue(passiveId, out int level))
            return level;
        return 0;
    }

    public void AddPassive(string passiveId)
    {
        if (HasPassive(passiveId))
        {
            Debug.LogWarning($"PlayerSkills: passive '{passiveId}' already known.");
            return;
        }

        _passiveLevels[passiveId] = 1;
        ApplyPassiveEffect(passiveId);

        Debug.Log($"PlayerSkills: learned new passive '{passiveId}'.");

        NotifySkillsChanged();
    }

    public void LevelUpPassive(string passiveId)
    {
        if (!HasPassive(passiveId))
        {
            Debug.LogWarning($"PlayerSkills: can't level passive '{passiveId}' that player doesn't have.");
            return;
        }

        _passiveLevels[passiveId] += 1;
        ApplyPassiveEffect(passiveId);

        Debug.Log($"PlayerSkills: leveled passive '{passiveId}' to {_passiveLevels[passiveId]}.");

        NotifySkillsChanged();
    }

    private void ApplyPassiveEffect(string passiveId)
    {
        switch (passiveId)
        {
            case "max_health_up":
                _stats.maxHealth += 20f;
                _stats.currentHealth += 20f;
                break;

            case "move_speed_up":
                _stats.moveSpeed += 0.5f;
                break;

            case "proj_count_up":
                _stats.projectileCount += 1;
                _stats.projectileSpreadAngle += 10f;
                break;

            case "xp_gain_up":
                _stats.xpGainMultiplier += 0.2f;
                break;

            case "cdr_up":
                _stats.cooldownReduction = Mathf.Clamp(_stats.cooldownReduction + 0.05f, 0f, 0.9f);
                break;

            case "chain_up":
                _stats.chainCount += 1;
                break;

            case "split_up":
                _stats.splitCount += 1;
                break;

            case "armor_up":
                _stats.armor += 10f;
                break;

            case "dodge_up":
                _stats.dodgeChance = Mathf.Clamp01(_stats.dodgeChance + 0.03f);
                break;

            case "fireball_chain":
                // ability-specific chain: Fireball only
                const string fireballId = "fireball";
                if (!_abilityChainBonus.ContainsKey(fireballId))
                    _abilityChainBonus[fireballId] = 0;
                _abilityChainBonus[fireballId] += 1;
                break;

            default:
                Debug.LogWarning($"PlayerSkills: no effect implemented for passive '{passiveId}'.");
                break;
        }
    }

    #endregion

    private void NotifySkillsChanged()
    {
        OnSkillsChanged?.Invoke();
    }

    #region Ability-specific queries

    public int GetAbilityChainBonus(string abilityId)
    {
        if (_abilityChainBonus.TryGetValue(abilityId, out int bonus))
            return bonus;
        return 0;
    }

    #endregion
}
