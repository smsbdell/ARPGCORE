using System.Text;
using UnityEngine;

public struct SkillEvaluationResult
{
    public float primaryDamage;
    public float secondaryDamage;
    public float effectiveCooldown;
    public float attacksPerSecond;
    public int projectileCount;
    public float projectileSpread;
    public int chainCount;
    public int splitCount;
}

public static class SkillDescriptionUtility
{
    private const float DefaultLevelScaling = 0.25f;

    public static SkillEvaluationResult EvaluateAbility(AbilityData ability, int level, CharacterStats stats, PlayerSkills playerSkills)
    {
        SkillEvaluationResult result = new SkillEvaluationResult();

        if (ability == null)
            return result;

        float scalingPerLevel = ability.levelScalingPerLevel > 0f ? ability.levelScalingPerLevel : DefaultLevelScaling;
        float levelMultiplier = 1f + (level - 1) * scalingPerLevel;

        float primaryDamage = ability.baseDamage;
        float secondaryDamage = ability.baseDamage * levelMultiplier;

        if (stats != null)
        {
            if (ability.usesWeaponDamage)
            {
                float weaponAverage = GetAverageWeaponDamage(stats);
                if (weaponAverage <= 0f)
                {
                    weaponAverage = ability.baseDamage + stats.baseDamage;
                }

                primaryDamage = weaponAverage * ability.weaponDamageMultiplier * levelMultiplier;
            }
            else
            {
                primaryDamage = (ability.baseDamage + stats.baseDamage) * levelMultiplier;
            }
        }
        else
        {
            primaryDamage = ability.baseDamage * levelMultiplier;
        }

        result.primaryDamage = primaryDamage;
        result.secondaryDamage = secondaryDamage;

        float hasteFactor = stats != null ? Mathf.Max(stats.attackSpeedMultiplier, 0.01f) : 1f;
        float cdrFactor = stats != null ? 1f - Mathf.Clamp01(stats.cooldownReduction) : 1f;

        result.effectiveCooldown = ability.cooldown * cdrFactor / hasteFactor;
        result.attacksPerSecond = result.effectiveCooldown > 0.0001f ? 1f / result.effectiveCooldown : 0f;

        result.projectileCount = stats != null ? Mathf.Max(1, stats.projectileCount) : 1;
        result.projectileSpread = stats != null ? stats.projectileSpreadAngle : 0f;
        result.splitCount = stats != null ? Mathf.Max(0, stats.splitCount) : 0;
        result.chainCount = stats != null ? Mathf.Max(0, stats.chainCount) : 0;

        if (playerSkills != null)
        {
            result.chainCount += playerSkills.GetAbilityChainBonus(ability.id);
        }

        return result;
    }

    public static string BuildPassiveDescription(string passiveId, PlayerSkills playerSkills, CharacterStats stats, bool showNextLevel)
    {
        if (stats == null || playerSkills == null)
            return passiveId;

        int currentLevel = Mathf.Max(0, playerSkills.GetPassiveLevel(passiveId));
        int targetLevel = showNextLevel ? currentLevel + 1 : Mathf.Max(1, currentLevel);

        switch (passiveId)
        {
            case "max_health_up":
            {
                float cur = stats.maxHealth;
                float nxt = cur + 20f;
                return showNextLevel
                    ? $"Vitality (Lv {targetLevel})\\nMax Health: {cur:F0} → {nxt:F0}"
                    : $"Vitality (Lv {targetLevel})\\nMax Health: {cur:F0}";
            }

            case "move_speed_up":
            {
                float cur = stats.moveSpeed;
                float nxt = cur + 0.5f;
                return showNextLevel
                    ? $"Haste (Lv {targetLevel})\\nMove Speed: {cur:F2} → {nxt:F2}"
                    : $"Haste (Lv {targetLevel})\\nMove Speed: {cur:F2}";
            }

            case "proj_count_up":
            {
                int curCount = stats.projectileCount;
                int nxtCount = curCount + 1;
                float curSpread = stats.projectileSpreadAngle;
                float nxtSpread = curSpread + 10f;
                return showNextLevel
                    ? $"Multi-Shot (Lv {targetLevel})\\nProjectiles: {curCount} → {nxtCount}\\nSpread: {curSpread:F0}° → {nxtSpread:F0}°"
                    : $"Multi-Shot (Lv {targetLevel})\\nProjectiles: {curCount}\\nSpread: {curSpread:F0}°";
            }

            case "xp_gain_up":
            {
                float curMul = stats.xpGainMultiplier;
                float nxtMul = curMul + 0.2f;
                return showNextLevel
                    ? $"Scholar (Lv {targetLevel})\\nXP Gain: {curMul * 100f:F0}% → {nxtMul * 100f:F0}%"
                    : $"Scholar (Lv {targetLevel})\\nXP Gain: {curMul * 100f:F0}%";
            }

            case "cdr_up":
            {
                float cur = stats.cooldownReduction;
                float nxt = Mathf.Clamp01(cur + 0.05f);
                return showNextLevel
                    ? $"Arcane Focus (Lv {targetLevel})\\nCooldown Reduction: {cur * 100f:F0}% → {nxt * 100f:F0}%"
                    : $"Arcane Focus (Lv {targetLevel})\\nCooldown Reduction: {cur * 100f:F0}%";
            }

            case "chain_up":
            {
                int cur = stats.chainCount;
                int nxt = cur + 1;
                return showNextLevel
                    ? $"Chain Mastery (Lv {targetLevel})\\nChains: {cur} → {nxt}"
                    : $"Chain Mastery (Lv {targetLevel})\\nChains: {cur}";
            }

            case "split_up":
            {
                int cur = stats.splitCount;
                int nxt = cur + 1;
                return showNextLevel
                    ? $"Split Mastery (Lv {targetLevel})\\nSplits: {cur} → {nxt}"
                    : $"Split Mastery (Lv {targetLevel})\\nSplits: {cur}";
            }

            case "armor_up":
            {
                float cur = stats.armor;
                float nxt = cur + 10f;
                return showNextLevel
                    ? $"Fortified (Lv {targetLevel})\\nArmor: {cur:F0} → {nxt:F0}"
                    : $"Fortified (Lv {targetLevel})\\nArmor: {cur:F0}";
            }

            case "dodge_up":
            {
                float cur = stats.dodgeChance;
                float nxt = Mathf.Clamp01(cur + 0.03f);
                return showNextLevel
                    ? $"Evasion (Lv {targetLevel})\\nDodge Chance: {cur * 100f:F1}% → {nxt * 100f:F1}%"
                    : $"Evasion (Lv {targetLevel})\\nDodge Chance: {cur * 100f:F1}%";
            }

            case "fireball_chain":
            {
                int cur = playerSkills.GetAbilityChainBonus("fireball");
                int nxt = cur + 1;
                return showNextLevel
                    ? $"Fireball Chain (Lv {targetLevel})\\nFireball extra chains: {cur} → {nxt}"
                    : $"Fireball Chain (Lv {targetLevel})\\nFireball extra chains: {cur}";
            }

            default:
                return passiveId;
        }
    }

    public static string BuildAbilityTooltip(AbilityData ability, int level, CharacterStats stats, PlayerSkills playerSkills)
    {
        if (ability == null)
            return string.Empty;

        SkillEvaluationResult eval = EvaluateAbility(ability, level, stats, playerSkills);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{ability.displayName} (Lv {level})");
        sb.AppendLine($"Damage: {eval.primaryDamage:F0}");

        if (Mathf.Abs(eval.secondaryDamage - eval.primaryDamage) > 0.01f)
        {
            sb.AppendLine($"Secondary: {eval.secondaryDamage:F0}");
        }

        sb.AppendLine($"Cooldown: {eval.effectiveCooldown:F2}s (≈ {eval.attacksPerSecond:F2}/s)");
        sb.AppendLine($"Projectiles: {eval.projectileCount}  Chains: {eval.chainCount}  Splits: {eval.splitCount}");

        if (!string.IsNullOrWhiteSpace(ability.description))
        {
            sb.AppendLine();
            sb.Append(ability.description.Trim());
        }

        return sb.ToString();
    }

    private static float GetAverageWeaponDamage(CharacterStats stats)
    {
        if (stats == null)
            return 0f;

        float min = stats.weaponDamageMin;
        float max = stats.weaponDamageMax;

        if (min <= 0f && max <= 0f)
            return 0f;

        float low = Mathf.Min(min, max);
        float high = Mathf.Max(min, max);
        return (low + high) * 0.5f;
    }
}
