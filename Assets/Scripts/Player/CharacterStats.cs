using System;
using UnityEngine;

/// <summary>
/// Core stats for any damageable character (player, monsters, etc.).
/// Includes health, movement, offense/defense, XP, and weapon damage rolls.
/// </summary>
public partial class CharacterStats : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Defense")]
    [Tooltip("Flat damage reduction applied before health loss.")]
    public float armor = 0f;

    [Tooltip("Chance (0-1) to completely avoid incoming damage.")]
    [Range(0f, 1f)] public float dodgeChance = 0f;

    [Header("Offense")]
    [Tooltip("Legacy base damage field (no longer used for automatic level scaling).")]
    public float baseDamage = 0f;

    [Tooltip("Number of projectiles fired for projectile-based abilities.")]
    public int projectileCount = 1;

    [Tooltip("Spread angle in degrees for multi-projectile attacks.")]
    public float projectileSpreadAngle = 0f;

    [Tooltip("Global split count for abilities that support splitting.")]
    public int splitCount = 0;

    [Tooltip("Global chain count for abilities that support chaining.")]
    public int chainCount = 0;

    [Tooltip("Global cooldown reduction (0.1 = 10% faster cooldowns).")]
    public float cooldownReduction = 0f;

    [Tooltip("Global attack speed multiplier for weapon-based skills (1 = normal).")]
    public float attackSpeedMultiplier = 1f;

    [Header("Weapon Damage (used by weapon skills)")]
    [Tooltip("Minimum weapon damage. Set this based on the equipped weapon.")]
    public float weaponDamageMin = 0f;

    [Tooltip("Maximum weapon damage. Set this based on the equipped weapon.")]
    public float weaponDamageMax = 0f;

    [Header("Experience / Leveling")]
    public int level = 1;
    public float currentXP = 0f;
    public float xpToNextLevel = 100f;

    [Tooltip("Multiplier for XP gained (1 = normal).")]
    public float xpGainMultiplier = 1f;

    [Tooltip("How much xpToNextLevel scales per level (1.2 = +20% each level).")]
    public float xpGrowthFactor = 1.2f;

    // Events used by health bars, XP UI, death handlers, etc.
    public event Action<float, float> OnHealthChanged; // current, max
    public event Action OnDied;
    public event Action<int> OnLevelUp;

    private void Awake()
    {
        if (currentHealth <= 0f)
            currentHealth = maxHealth;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    #region Health / Damage

    public void TakeDamage(float amount)
    {
        if (amount <= 0f)
            return;

        // Dodge check
        if (UnityEngine.Random.value < dodgeChance)
        {
            // Dodged; no damage taken
            return;
        }

        // Apply armor as flat reduction, clamped to not heal
        float effectiveDamage = Mathf.Max(0f, amount - armor);

        currentHealth -= effectiveDamage;
        currentHealth = Mathf.Max(0f, currentHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        OnDied?.Invoke();
        // Actual destruction / death behavior is handled by listeners (e.g. enemy controller)
    }

    #endregion

    #region Experience / Level

    public void AddXP(float amount)
    {
        if (amount <= 0f)
            return;

        float effectiveAmount = amount * xpGainMultiplier;
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

        OnLevelUp?.Invoke(level);

        // Increase XP requirement for next level
        xpToNextLevel *= xpGrowthFactor;

        // Simple example: level gives more health, but NOT more damage.
        maxHealth += 10f;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    #endregion

    #region Weapon Damage

    /// <summary>
    /// Returns a random roll between weaponDamageMin and weaponDamageMax.
    /// If no weapon damage is configured, returns 0.
    /// </summary>
    public float GetRandomWeaponDamage()
    {
        if (weaponDamageMin <= 0f && weaponDamageMax <= 0f)
            return 0f;

        float min = Mathf.Min(weaponDamageMin, weaponDamageMax);
        float max = Mathf.Max(weaponDamageMin, weaponDamageMax);

        return UnityEngine.Random.Range(min, max);
    }

    #endregion
}
