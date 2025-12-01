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
    private float _nextDamageTime = 0f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _stats = GetComponent<CharacterStats>();
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _target = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("MonsterController: No GameObject with tag 'Player' found.");
        }
    }

    private void FixedUpdate()
    {
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
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
            return;

        PlayerProgression progression = playerObj.GetComponent<PlayerProgression>();
        if (progression == null)
            return;

        progression.GainXP(xpReward);
    }
}
