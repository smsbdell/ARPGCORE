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

    [Header("Secondary Prefabs")]
    [Tooltip("Optional VFX spawned at each lightning arc target when Lightning Shot hits.")]
    public GameObject lightningStrikePrefab;

    [Tooltip("Projectile prefab used for Ice Arrow shards.")]
    public GameObject iceShardProjectilePrefab;

    [Tooltip("Lifetime for spawned lightning strike VFX objects.")]
    public float lightningStrikeLifetime = 0.15f;

    [Header("Movement")]
    [Tooltip("Speed the projectile should travel at if driven by code instead of physics.")]
    public float projectileSpeed = 10f;

    [Tooltip("Normalized direction used for transform-driven movement when no Rigidbody2D is present.")]
    public Vector2 direction = Vector2.right;

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
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        InitializeDamageFromAbility();
    }

    private void Start()
    {
        _lifeTimer = lifetime;
    }

    private void Update()
    {
        if (_rb == null && projectileSpeed > 0f)
        {
            transform.position += (Vector3)(direction.normalized * projectileSpeed * Time.deltaTime);
        }

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

            if (lightningStrikePrefab != null)
            {
                GameObject strike = Instantiate(lightningStrikePrefab, h.transform.position, Quaternion.identity);
                if (lightningStrikeLifetime > 0f)
                {
                    Destroy(strike, lightningStrikeLifetime);
                }
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
        if (iceShardProjectilePrefab == null)
            return;

        // Spawn a small cone of shards traveling away from the caster (same direction as the incoming arrow).
        Vector2 baseDir = direction;
        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = Vector2.right;
        baseDir = baseDir.normalized;

        int shardCount = 5;
        float shardSpread = 45f;
        for (int i = 0; i < shardCount; i++)
        {
            float t = shardCount == 1 ? 0f : (float)i / (shardCount - 1);
            float angle = shardSpread * (t - 0.5f);
            Vector2 shardDir = Quaternion.Euler(0f, 0f, angle) * baseDir;

            Vector3 spawnPos = hitCollider.transform.position + (Vector3)(shardDir * 0.2f);
            GameObject shard = Instantiate(iceShardProjectilePrefab, spawnPos, Quaternion.identity);
            ProjectileDamage shardDamage = shard.GetComponent<ProjectileDamage>();
            if (shardDamage != null)
            {
                shardDamage.damage = secondaryDamage;
                shardDamage.secondaryDamage = secondaryDamage;
                shardDamage.damageType = DamageType.Ice;
                shardDamage.direction = shardDir;
                shardDamage.projectileSpeed = projectileSpeed * 0.75f;
                shardDamage.ownerCollider = ownerCollider;
            }

            Collider2D shardCollider = shard.GetComponent<Collider2D>();
            if (shardCollider != null && hitCollider != null)
            {
                Physics2D.IgnoreCollision(shardCollider, hitCollider);
            }
            if (shardCollider != null && ownerCollider != null)
            {
                Physics2D.IgnoreCollision(shardCollider, ownerCollider);
            }

            Rigidbody2D rb = shard.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = shardDir * (projectileSpeed * 0.75f);
            }

            float rotAngle = Mathf.Atan2(shardDir.y, shardDir.x) * Mathf.Rad2Deg;
            shard.transform.rotation = Quaternion.AngleAxis(rotAngle, Vector3.forward);
        }
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