using System;
using System.Collections.Generic;
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

    [Tooltip("Skill level of the ability that spawned this projectile.")]
    public int abilityLevel = 1;

    [NonSerialized]
    public IAbilityLevelProvider abilityLevelProvider;

    [Tooltip("Collider of the owner (player, monster) so we can ignore it on collision.")]
    public Collider2D ownerCollider;

    [Header("Chain / Split")]
    [Tooltip("How many more times this projectile can chain to new targets.")]
    public int chainRemaining = 0;

    [Tooltip("How many more times this projectile can split into additional projectiles.")]
    public int splitRemaining = 0;

    [Tooltip("If false, this projectile will not perform chain or split follow-ups when it hits.")]
    public bool allowSplitAndChain = true;

    [Tooltip("Maximum distance a chain can jump to another target.")]
    public float maxChainDistance = 10f;

    [Tooltip("Layer mask used to search for enemies when chaining/splitting.")]
    public LayerMask enemyLayerMask;

    [Header("Lifetime")]
    public float lifetime = 5f;

    private bool _damageInitialized;
    private float _lifeTimer;
    private Rigidbody2D _rb;
    private int _resolvedEnemyMask;
    private PooledObject _pooledObject;
    private bool _pendingRelease;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _resolvedEnemyMask = ResolveEnemyMask();
        _pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        _lifeTimer = lifetime;
        _pendingRelease = false;
        _resolvedEnemyMask = ResolveEnemyMask();
    }

    private void Start()
    {
        _lifeTimer = lifetime;
        _resolvedEnemyMask = ResolveEnemyMask();

        InitializeDamageFromAbility();
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
                ReleaseProjectile();
            }
        }
    }

    private int ResolveEnemyMask()
    {
        if (enemyLayerMask.value != 0)
            return enemyLayerMask.value;

        int monsterLayer = LayerMask.NameToLayer("Monster");
        return monsterLayer >= 0 ? 1 << monsterLayer : 0;
    }

    public void ConfigureAbilityContext(string abilityId, int level, IAbilityLevelProvider levelProvider, bool recomputeDamage = true)
    {
        sourceAbilityId = abilityId;
        abilityLevel = level;
        abilityLevelProvider = levelProvider;

        if (recomputeDamage)
        {
            _damageInitialized = false;
            InitializeDamageFromAbility();
        }
        else
        {
            _damageInitialized = true;
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

        int skillLevel = abilityLevel;
        if (skillLevel <= 0 && abilityLevelProvider != null)
        {
            skillLevel = abilityLevelProvider.GetAbilityLevel(sourceAbilityId);
        }

        if (skillLevel <= 0)
            skillLevel = 1;

        float scalingPerLevel = ability.levelScalingPerLevel > 0f ? ability.levelScalingPerLevel : 0.25f;
        float skillMultiplier = 1f + (skillLevel - 1) * scalingPerLevel;

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

            primaryDamage = weaponRoll * ability.weaponDamageMultiplier * skillMultiplier;

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore collisions with the owner
        if (ownerCollider != null && other == ownerCollider)
            return;

        // Only process collisions against configured enemy layers
        if (_resolvedEnemyMask != 0 && (_resolvedEnemyMask & (1 << other.gameObject.layer)) == 0)
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
        if (allowSplitAndChain)
        {
            HandleSplitAndChain(other, targetStats);
        }

        ReleaseProjectile();
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

        var hits = Physics2D.OverlapCircleAll(center, radius, _resolvedEnemyMask);
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
                GameObject strike = GameObjectPool.Get(lightningStrikePrefab, h.transform.position, Quaternion.identity);
                if (strike != null && lightningStrikeLifetime > 0f)
                {
                    PooledObject pooledStrike = strike.GetComponent<PooledObject>();
                    if (pooledStrike != null)
                    {
                        pooledStrike.ReleaseAfter(lightningStrikeLifetime);
                    }
                    else
                    {
                        Destroy(strike, lightningStrikeLifetime);
                    }
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

        var hits = Physics2D.OverlapCircleAll(center, radius, _resolvedEnemyMask);
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
            GameObject shard = GameObjectPool.Get(iceShardProjectilePrefab, spawnPos, Quaternion.identity);
            ProjectileDamage shardDamage = shard.GetComponent<ProjectileDamage>();
            if (shardDamage != null)
            {
                shardDamage.damage = secondaryDamage;
                shardDamage.secondaryDamage = secondaryDamage;
                shardDamage.damageType = DamageType.Ice;
                shardDamage.direction = shardDir;
                shardDamage.projectileSpeed = projectileSpeed * 0.75f;
                shardDamage.ownerCollider = ownerCollider;
                shardDamage.chainRemaining = 0;
                shardDamage.splitRemaining = 0;
                shardDamage.allowSplitAndChain = false;
                shardDamage.enemyLayerMask = enemyLayerMask;

                shardDamage.ConfigureAbilityContext(sourceAbilityId, abilityLevel, abilityLevelProvider, recomputeDamage: false);
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
        Vector2 hitPosition = hitCollider != null ? (Vector2)hitCollider.bounds.center : (Vector2)transform.position;

        List<Collider2D> spawnedThisEvent = new List<Collider2D>();

        // Prioritize splitting until splits are exhausted, then allow chaining
        if (splitRemaining > 0)
        {
            HandleSplit(hitPosition, hitCollider, spawnedThisEvent);
        }
        else if (chainRemaining > 0)
        {
            HandleChain(hitPosition, hitCollider);
        }
    }

    #endregion

    private void HandleSplit(Vector2 hitPosition, Collider2D hitCollider, List<Collider2D> spawnedThisEvent)
    {
        Vector2 baseDir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        float splitAngle = 25f;
        Vector2[] dirs = new Vector2[]
        {
            Quaternion.Euler(0f, 0f, splitAngle * 0.5f) * baseDir,
            Quaternion.Euler(0f, 0f, -splitAngle * 0.5f) * baseDir
        };

        foreach (var dir in dirs)
        {
            ProjectileDamage spawned = SpawnFollowUpProjectile(hitPosition + dir * 0.25f, dir, chainRemaining, splitRemaining - 1, hitCollider, spawnedThisEvent);
            if (spawned == null)
                continue;

            Collider2D col = spawned.GetComponent<Collider2D>();
            if (col != null)
            {
                spawnedThisEvent.Add(col);
            }
        }

        // Prevent freshly split projectiles from consuming each other immediately
        for (int i = 0; i < spawnedThisEvent.Count; i++)
        {
            for (int j = i + 1; j < spawnedThisEvent.Count; j++)
            {
                var a = spawnedThisEvent[i];
                var b = spawnedThisEvent[j];
                if (a != null && b != null)
                {
                    Physics2D.IgnoreCollision(a, b);
                }
            }
        }
    }

    private void HandleChain(Vector2 hitPosition, Collider2D hitCollider)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitPosition, maxChainDistance, _resolvedEnemyMask);
        Collider2D best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            if (h == null || h == hitCollider)
                continue;

            if (ownerCollider != null && h == ownerCollider)
                continue;

            CharacterStats stats = h.GetComponentInParent<CharacterStats>();
            if (stats == null)
                continue;

            float dist = Vector2.SqrMagnitude((Vector2)h.bounds.center - hitPosition);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = h;
            }
        }

        if (best == null)
            return;

        Vector2 dir = ((Vector2)best.bounds.center - hitPosition).normalized;
        SpawnFollowUpProjectile(hitPosition + dir * 0.25f, dir, chainRemaining - 1, splitRemaining, hitCollider, null);
    }

    private ProjectileDamage SpawnFollowUpProjectile(Vector2 spawnPosition, Vector2 dir, int chainCount, int splitCount, Collider2D ignoreCollider, List<Collider2D> ignoreList)
    {
        GameObject prefab = _pooledObject != null ? _pooledObject.Prefab : gameObject;
        GameObject clone = GameObjectPool.Get(prefab, spawnPosition, Quaternion.identity);

        ProjectileDamage cloneDamage = clone.GetComponent<ProjectileDamage>();
        if (cloneDamage != null)
        {
            cloneDamage.damage = damage;
            cloneDamage.secondaryDamage = secondaryDamage;
            cloneDamage.damageType = damageType;
            cloneDamage.direction = dir;
            cloneDamage.projectileSpeed = projectileSpeed;
            cloneDamage.ownerCollider = ownerCollider;
            cloneDamage.chainRemaining = Mathf.Max(0, chainCount);
            cloneDamage.splitRemaining = Mathf.Max(0, splitCount);
            cloneDamage.allowSplitAndChain = allowSplitAndChain;
            cloneDamage.enemyLayerMask = enemyLayerMask;
            cloneDamage._resolvedEnemyMask = _resolvedEnemyMask;

            cloneDamage.ConfigureAbilityContext(sourceAbilityId, abilityLevel, abilityLevelProvider, recomputeDamage: false);
        }

        Collider2D cloneCollider = clone.GetComponent<Collider2D>();
        Collider2D selfCollider = GetComponent<Collider2D>();
        if (cloneCollider != null)
        {
            if (ownerCollider != null)
            {
                Physics2D.IgnoreCollision(cloneCollider, ownerCollider);
            }

            if (selfCollider != null)
            {
                Physics2D.IgnoreCollision(cloneCollider, selfCollider);
            }

            if (ignoreCollider != null)
            {
                Physics2D.IgnoreCollision(cloneCollider, ignoreCollider);
            }

            if (ignoreList != null)
            {
                for (int i = 0; i < ignoreList.Count; i++)
                {
                    if (ignoreList[i] != null)
                    {
                        Physics2D.IgnoreCollision(cloneCollider, ignoreList[i]);
                    }
                }
            }
        }

        Rigidbody2D rb = clone.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = dir.normalized * projectileSpeed;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        clone.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        return cloneDamage;
    }

    private void ReleaseProjectile()
    {
        if (_pendingRelease)
            return;

        _pendingRelease = true;

        if (_pooledObject != null)
        {
            _pooledObject.Release();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize chain radius if desired
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxChainDistance);
    }
}