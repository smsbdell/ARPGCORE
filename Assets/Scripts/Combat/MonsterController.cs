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

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _stats = GetComponent<CharacterStats>();
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
}
