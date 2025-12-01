using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterStats))]
public class AutoAttackController : MonoBehaviour
{
    [Header("Ability Settings")]
    [Tooltip("IDs of abilities to load from AbilityDatabase for this character at start.")]
    public List<string> equippedAbilityIds = new List<string>();

    private CharacterStats _stats;
    private PlayerSkills _playerSkills;
    private Collider2D _ownerCollider;

    private readonly List<AbilityData> _equippedAbilities = new List<AbilityData>();
    private readonly Dictionary<string, float> _cooldownTimers = new Dictionary<string, float>();

    private void Awake()
    {
        _stats = GetComponent<CharacterStats>();
        _playerSkills = GetComponent<PlayerSkills>();
        _ownerCollider = GetComponent<Collider2D>();

        // Some player prefabs keep the hitbox on a child; grab that if no collider exists on the root.
        if (_ownerCollider == null)
        {
            _ownerCollider = GetComponentInChildren<Collider2D>();
        }
    }

    private void Start()
    {
        if (AbilityDatabase.Instance == null)
        {
            Debug.LogError("AbilityDatabase not found in scene.");
            return;
        }

        foreach (string id in equippedAbilityIds)
        {
            AddEquippedAbility(id);
        }
    }

    private void Update()
    {
        float delta = Time.deltaTime;

        foreach (AbilityData ability in _equippedAbilities)
        {
            if (!_cooldownTimers.ContainsKey(ability.id))
                _cooldownTimers[ability.id] = 0f;

            _cooldownTimers[ability.id] -= delta;

            if (ability.autoCast && _cooldownTimers[ability.id] <= 0f)
            {
                CastAbility(ability);

                float hasteFactor = 1f;
                float cdrFactor = 1f;

                if (_stats != null)
                {
                    hasteFactor = Mathf.Max(_stats.attackSpeedMultiplier, 0.01f);
                    cdrFactor = 1f - Mathf.Clamp01(_stats.cooldownReduction);
                }

                float adjustedCooldown = ability.cooldown * cdrFactor / hasteFactor;
                _cooldownTimers[ability.id] = adjustedCooldown;
            }
        }
    }

    public void AddEquippedAbility(string abilityId)
    {
        if (AbilityDatabase.Instance == null)
        {
            Debug.LogError("AutoAttackController: AbilityDatabase.Instance is null.");
            return;
        }

        if (_equippedAbilities.Exists(a => a.id == abilityId))
        {
            return;
        }

        AbilityData ability = AbilityDatabase.Instance.GetAbilityById(abilityId);
        if (ability != null)
        {
            _equippedAbilities.Add(ability);
            _cooldownTimers[ability.id] = 0f;

            if (!equippedAbilityIds.Contains(abilityId))
            {
                equippedAbilityIds.Add(abilityId);
            }

            Debug.Log($"AutoAttackController: added equipped ability '{abilityId}'.");
        }
        else
        {
            Debug.LogWarning($"AutoAttackController: ability '{abilityId}' not found in database.");
        }
    }

    private void CastAbility(AbilityData ability)
    {
        int level = 1;
        if (_playerSkills != null)
        {
            int storedLevel = _playerSkills.GetActiveLevel(ability.id);
            if (storedLevel > 0)
                level = storedLevel;
        }

        float levelMultiplier = 1f + (level - 1) * 0.25f;
        float damage = (ability.baseDamage + _stats.baseDamage) * levelMultiplier;

        bool isCrit = Random.value < _stats.critChance;
        if (isCrit)
        {
            damage *= _stats.critMultiplier;
        }

        Debug.Log($"Casting ability: {ability.displayName} (Lv {level}) for {damage} damage (crit: {isCrit})");

        if (!string.IsNullOrEmpty(ability.projectilePrefabName))
        {
            string resourcePath = "Projectiles/" + ability.projectilePrefabName;
            GameObject prefab = Resources.Load<GameObject>(resourcePath);

            if (prefab != null)
            {
                int projectileCount = 1;
                float spreadAngle = 0f;
                int splitCount = 0;
                int chainCount = 0;

                if (_stats != null)
                {
                    // All projectile-based abilities scale with projectileCount for their PRIMARY projectiles
                    projectileCount = Mathf.Max(1, _stats.projectileCount);
                    spreadAngle = _stats.projectileSpreadAngle;
                    splitCount = Mathf.Max(0, _stats.splitCount);
                    chainCount = Mathf.Max(0, _stats.chainCount);
                }

                if (_playerSkills != null)
                {
                    chainCount += _playerSkills.GetAbilityChainBonus(ability.id);
                }

                Vector2 baseDirection = GetAimDirection();
                if (baseDirection.sqrMagnitude < 0.0001f)
                    baseDirection = Vector2.right;
                baseDirection = baseDirection.normalized;

                for (int i = 0; i < projectileCount; i++)
                {
                    Vector2 projDir = baseDirection;

                    if (projectileCount > 1 && spreadAngle > 0f)
                    {
                        float step = spreadAngle / (projectileCount - 1);
                        float offset = -spreadAngle * 0.5f + step * i;
                        projDir = RotateVector(baseDirection, offset);
                    }

                    GameObject projectile = Object.Instantiate(prefab, transform.position, Quaternion.identity);

                    ProjectileDamage projDamage = projectile.GetComponent<ProjectileDamage>();
                    if (projDamage != null)
                    {
                        projDamage.damage = damage;
                        projDamage.secondaryDamage = damage;
                        projDamage.splitRemaining = splitCount;
                        projDamage.chainRemaining = chainCount;
                        projDamage.projectileSpeed = ability.projectileSpeed;
                        projDamage.direction = projDir;

                        projDamage.damageType = ability.damageType;
                        projDamage.sourceAbilityId = ability.id;
                        projDamage.ownerCollider = _ownerCollider;
                    }

                    Collider2D projCollider = projectile.GetComponent<Collider2D>();
                    if (projCollider != null && _ownerCollider != null)
                    {
                        Physics2D.IgnoreCollision(projCollider, _ownerCollider);
                    }

                    Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.linearVelocity = projDir * ability.projectileSpeed;
                    }

                    float angle = Mathf.Atan2(projDir.y, projDir.x) * Mathf.Rad2Deg;
                    projectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

                    if (ability.duration > 0f)
                    {
                        Object.Destroy(projectile, ability.duration);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Projectile prefab not found at Resources/" + resourcePath);
            }
        }
        else
        {
            // Future: non-projectile abilities.
        }
    }

    private Vector2 GetAimDirection()
    {
        if (Camera.main == null || Mouse.current == null)
        {
            return Vector2.right;
        }

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        float zDistance = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        Vector3 screenPoint = new Vector3(mouseScreenPos.x, mouseScreenPos.y, zDistance);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPoint);
        Vector2 dir = (worldPos - transform.position);
        return dir.normalized;
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }
}