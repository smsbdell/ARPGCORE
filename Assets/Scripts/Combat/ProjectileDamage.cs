using System;
using UnityEngine;

/// <summary>
/// Handles damage application, chain/split logic, and special secondary effects for projectiles.
/// Now computes damage using AbilityDatabase + CharacterStats weapon damage,
/// instead of relying on player level.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ProjectileDamage : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Primary hit damage. This will be overridden at runtime based on the ability and weapon.")]
    public float damage = 10f;

    [Tooltip("Damage used by secondary effects (explosions, arcs, shards).")]
    public float secondaryDamage = 10f;

    public DamageType damageType = DamageType.Physical;

    [Header("Movement")]
    [Tooltip("Speed the projectile should travel at if driven by code instead of physics.")]
    public float projectileSpeed = 10f;

    [Header("Source & Ownership")]
    [Tooltip("Ability id that spawned this projectile.")]
    public string sourceAbilityId;

    [Tooltip("Collider of the owner (player, monster) so we can ignore it on collision.")]
    public Collider2D ownerCollider;

    [Header("Chain / Split")]
    [Tooltip("How many more times this projectile can chain to new targets.")]
    public int chainRemaining = 0;

    [Tooltip("How many more times this projectile can split into additional projectiles.")]
    public int splitRemaining = 0;

    [Tooltip("Maximum distance a chain can jump to another target.")]
    public float maxChainDistance = 10f;

    [Tooltip("Layer mask used to search for enemies when chaining/splitting.")]
    public LayerMask enemyLayerMask;

    [Header("Lifetime")]
    public float lifetime = 5f;

    private bool _damageInitialized;
    private float _lifeTimer;

    private void Awake()
    {
        InitializeDamageFromAbility();
    }

    private void Start()
    {
        _lifeTimer = lifetime;
    }

    private void Update()
    {
        if (lifetime > 0f)
        {
            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    private void InitializeDamageFromAbility()
    {
        if (_damageInitialized)
            return;

        _damageInitialized = true;

        if (string.IsNullOrEmpty(sourceAbilityId))
            return;

        if (AbilityDatabase.Instance == null)
            return;

        AbilityData ability = AbilityDatabase.Instance.GetAbilityById(sourceAbilityId);
        if (ability == null)
            return;

        // Find owner CharacterStats from ownerCollider, if available
        CharacterStats ownerStats = null;
        if (ownerCollider != null)
        {
            ownerStats = ownerCollider.GetComponentInParent<CharacterStats>();
        }

        // Determine skill level (if any system exposes GetAbilityLevel(string))
        int skillLevel = GetAbilityLevelViaReflection(sourceAbilityId);
        if (skillLevel <= 0)
            skillLevel = 1;

        float skillMultiplier = 1f + (skillLevel - 1) * ability.levelScalingPerLevel;

        float primaryDamage;
        float secondary = 0f;

        if (ability.usesWeaponDamage && ownerStats != null)
        {
            float weaponRoll = ownerStats.GetRandomWeaponDamage();
            if (weaponRoll <= 0f)
            {
                // Fallback to baseDamage if no weapon damage configured
                weaponRoll = ability.baseDamage;
            }

            primaryDamage = weaponRoll * ability.weaponDamageMultiplier;

            // Secondary effects scale purely with ability baseDamage + skill level
            secondary = ability.baseDamage * skillMultiplier;
        }
        else
        {
            // Non-weapon skills: scale ability baseDamage with skill level only
            primaryDamage = ability.baseDamage * skillMultiplier;
            secondary = primaryDamage;
        }

        damage = primaryDamage;
        secondaryDamage = secondary;
        damageType = ability.damageType;
    }

    /// <summary>
    /// Looks for any MonoBehaviour in the scene that exposes int GetAbilityLevel(string abilityId).
    /// This keeps ProjectileDamage decoupled from your concrete SkillSystem implementation.
    /// </summary>
    private int GetAbilityLevelViaReflection(string abilityId)
    {
        try
        {
            MonoBehaviour[] allMBs = FindObjectsOfType<MonoBehaviour>();
            for (int i = 0; i < allMBs.Length; i++)
            {
                var mb = allMBs[i];
                if (mb == null) continue;

                var type = mb.GetType();
                var method = type.GetMethod("GetAbilityLevel", new Type[] { typeof(string) });
                if (method == null)
                    continue;

                object result = method.Invoke(mb, new object[] { abilityId });
                if (result is int level && level > 0)
                {
                    return level;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"ProjectileDamage: error trying to get ability level via reflection: {ex.Message}");
        }

        return 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore collisions with the owner
        if (ownerCollider != null && other == ownerCollider)
            return;

        CharacterStats targetStats = other.GetComponentInParent<CharacterStats>();
        if (targetStats != null)
        {
            // Apply primary hit damage
            targetStats.TakeDamage(damage);

            if (DamagePopupManager.Instance != null)
            {
                DamagePopupManager.Instance.SpawnPopup(damage, transform.position);
            }
        }

        HandleSecondaryEffects(other, targetStats);
        HandleSplitAndChain(other, targetStats);

        Destroy(gameObject);
    }

    #region Secondary Effects (per-ability special logic)

    private void HandleSecondaryEffects(Collider2D hitCollider, CharacterStats hitStats)
    {
        if (string.IsNullOrEmpty(sourceAbilityId))
            return;

        // Implement ability-specific secondary logic here.
        // Examples (you already have logic for these; wire secondaryDamage into it):

        if (sourceAbilityId == "lightning_shot")
        {
            HandleLightningArcs(hitCollider);
        }
        else if (sourceAbilityId == "fire_shot")
        {
            HandleFireExplosion(hitCollider);
        }
        else if (sourceAbilityId == "ice_arrow")
        {
            HandleIceShards(hitCollider);
        }
    }

    private void HandleLightningArcs(Collider2D hitCollider)
    {
        // Example: find up to N nearby enemies around hit point and apply secondaryDamage.
        // You can adapt this to match your existing implementation.
        Vector2 center = hitCollider.transform.position;
        float radius = maxChainDistance;

        var hits = Physics2D.OverlapCircleAll(center, radius, enemyLayerMask);
        int arcs = 3;
        int count = 0;

        foreach (var h in hits)
        {
            if (h == hitCollider)
                continue;

            CharacterStats stats = h.GetComponentInParent<CharacterStats>();
            if (stats == null)
                continue;

            stats.TakeDamage(secondaryDamage);
            if (DamagePopupManager.Instance != null)
            {
                DamagePopupManager.Instance.SpawnPopup(secondaryDamage, h.transform.position);
            }

            count++;
            if (count >= arcs)
                break;
        }
    }

    private void HandleFireExplosion(Collider2D hitCollider)
    {
        Vector2 center = hitCollider.transform.position;
        float radius = 1.5f; // adjust as needed

        var hits = Physics2D.OverlapCircleAll(center, radius, enemyLayerMask);
        foreach (var h in hits)
        {
            CharacterStats stats = h.GetComponentInParent<CharacterStats>();
            if (stats == null)
                continue;

            stats.TakeDamage(secondaryDamage);
            if (DamagePopupManager.Instance != null)
            {
                DamagePopupManager.Instance.SpawnPopup(secondaryDamage, h.transform.position);
            }
        }
    }

    private void HandleIceShards(Collider2D hitCollider)
    {
        // This assumes you already have shard spawning logic elsewhere.
        // Here you'd spawn shard projectiles and set their ProjectileDamage.damage = secondaryDamage.
        // Left as a hook to integrate with your existing shard spawning code.
    }

    #endregion

    #region Chain / Split

    private void HandleSplitAndChain(Collider2D hitCollider, CharacterStats hitStats)
    {
        // You already have fairly involved split/chain logic.
        // This hook keeps the interface: chainRemaining / splitRemaining / maxChainDistance / enemyLayerMask.
        // If you want, you can re-inject your existing implementation here
        // and it will use the new damage values computed in InitializeDamageFromAbility().
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        // Visualize chain radius if desired
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxChainDistance);
    }
}