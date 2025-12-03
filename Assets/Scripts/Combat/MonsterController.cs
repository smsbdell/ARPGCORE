using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CharacterStats))]
public class MonsterController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;

    [Header("Combat")]
    public float contactDamage = 10f;
    public float contactDamageCooldown = 0.5f;
    public float xpReward = 5f;

    private Rigidbody2D _rb;
    private CharacterStats _stats;
    private Transform _target;
    private PlayerProgression _playerProgression;
    private Action<MonsterController> _returnToPool;
    private float _nextDamageTime = 0f;
    private bool _baseStatsCached;
    private float _baseMoveSpeed;
    private float _baseContactDamage;
    private float _baseXpReward;
    private float _baseMaxHealth;
    private float _baseArmor;
    private float _baseDodgeChance;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _stats = GetComponent<CharacterStats>();
        CacheBaseStats();
    }

    private void Start()
    {
        CachePlayerReferences();
    }

    private void OnEnable()
    {
        if (_stats == null)
            _stats = GetComponent<CharacterStats>();

        if (_stats != null)
        {
            _stats.OnDied += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (_stats != null)
        {
            _stats.OnDied -= HandleDeath;
        }
    }

    private void FixedUpdate()
    {
        if (_target == null)
            CachePlayerReferences();

        if (_target == null)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir = (_target.position - transform.position).normalized;
        _rb.linearVelocity = dir * moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDealDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDealDamage(other);
    }

    private void TryDealDamage(Collider2D other)
    {
        if (Time.time < _nextDamageTime)
            return;

        if (!other.CompareTag("Player"))
            return;

        CharacterStats playerStats = other.GetComponent<CharacterStats>();
        if (playerStats != null)
        {
            playerStats.TakeDamage(contactDamage);
            _nextDamageTime = Time.time + contactDamageCooldown;
        }
    }

    public void OnDeath()
    {
        if (_playerProgression == null)
            CachePlayerReferences();

        _playerProgression?.GainXP(xpReward);
    }

    public void SetReturnToPool(Action<MonsterController> returnToPool)
    {
        _returnToPool = returnToPool;
    }

    public void ResetState()
    {
        ResetState(MonsterSpawnContext.Default);
    }

    public void ResetState(MonsterSpawnContext spawnContext)
    {
        CacheBaseStats();

        ApplySpawnContext(spawnContext);
        _nextDamageTime = 0f;
        _rb.linearVelocity = Vector2.zero;
        _stats?.ResetHealth();
    }

    private void HandleDeath()
    {
        OnDeath();
        if (_returnToPool != null)
        {
            _returnToPool(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CachePlayerReferences()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("MonsterController: No GameObject with tag 'Player' found.");
            return;
        }

        _target = playerObj.transform;
        _playerProgression = playerObj.GetComponent<PlayerProgression>();
        if (_playerProgression == null)
            Debug.LogWarning("MonsterController: PlayerProgression component not found on player.");
    }

    private void CacheBaseStats()
    {
        if (_baseStatsCached)
            return;

        _baseStatsCached = true;
        _baseMoveSpeed = moveSpeed;
        _baseContactDamage = contactDamage;
        _baseXpReward = xpReward;

        if (_stats != null)
        {
            _baseMaxHealth = Mathf.Max(1f, _stats.maxHealth);
            _baseArmor = _stats.armor;
            _baseDodgeChance = _stats.dodgeChance;
        }
    }

    private void ApplySpawnContext(MonsterSpawnContext spawnContext)
    {
        MonsterSpawnContext normalized = spawnContext.WithDefaults();

        moveSpeed = _baseMoveSpeed * normalized.MoveSpeedMultiplier;
        contactDamage = _baseContactDamage * normalized.ContactDamageMultiplier;
        xpReward = _baseXpReward * normalized.XpRewardMultiplier;

        if (_stats != null)
        {
            _stats.maxHealth = _baseMaxHealth * normalized.MaxHealthMultiplier;
            _stats.armor = _baseArmor * normalized.ArmorMultiplier;
            _stats.dodgeChance = Mathf.Clamp01(_baseDodgeChance * normalized.DodgeChanceMultiplier);
        }
    }
}
