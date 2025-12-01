using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class PlayerProgression : MonoBehaviour
{
    [Header("Level & XP")]
    public int level = 1;
    public float currentXP = 0f;
    public float xpToNextLevel = 10f;
    public float xpGrowthFactor = 1.5f;

    [Header("Stat Growth Per Level")]
    public float healthPerLevel = 10f;
    public float damagePerLevel = 2f;

    private CharacterStats _stats;

    private void Awake()
    {
        _stats = GetComponent<CharacterStats>();
    }

    public void GainXP(float amount)
    {
        float effectiveAmount = amount;

        if (_stats != null)
        {
            // XP multiplier (e.g. 1.5f = 50% more XP)
            effectiveAmount *= Mathf.Max(0f, _stats.xpGainMultiplier);
        }

        currentXP += effectiveAmount;

        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        level++;

        _stats.maxHealth += healthPerLevel;
        _stats.currentHealth = _stats.maxHealth;
        _stats.baseDamage += damagePerLevel;

        xpToNextLevel *= xpGrowthFactor;

        Debug.Log($"Player leveled up to {level}");

        if (SkillSelectionManager.Instance != null)
        {
            SkillSelectionManager.Instance.ShowLevelUpChoices();
        }
    }
}
