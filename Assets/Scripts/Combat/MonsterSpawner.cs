using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject monsterPrefab;
    public Transform player;

    [Header("Spawning")]
    public float spawnInterval = 2f;
    public int maxMonsters = 50;
    public float spawnBuffer = 2f;
    [Tooltip("Maximum monsters to activate per frame, even if capacity allows more.")]
    public int maxSpawnsPerFrame = 3;

    private float _spawnTimer;
    private readonly List<GameObject> _activeMonsters = new List<GameObject>();
    private readonly Queue<GameObject> _monsterPool = new Queue<GameObject>();
    private int _spawnsThisFrame;
    private bool _spawningEnabled = true;

    private void Awake()
    {
        PrewarmPool();
    }

    private void Update()
    {
        if (monsterPrefab == null || player == null || Camera.main == null)
            return;

        if (!_spawningEnabled)
            return;

        _spawnsThisFrame = 0;
        _spawnTimer -= Time.deltaTime;

        while (_spawnTimer <= 0f && _spawnsThisFrame < maxSpawnsPerFrame)
        {
            if (!TrySpawnMonster())
            {
                _spawnTimer = spawnInterval;
                break;
            }

            _spawnTimer += spawnInterval;
            _spawnsThisFrame++;
        }
    }

    private bool TrySpawnMonster()
    {
        if (_activeMonsters.Count >= maxMonsters)
            return false;

        if (_monsterPool.Count == 0)
            return false;

        Vector3 spawnPos = GetSpawnPositionAroundPlayer();
        GameObject monster = _monsterPool.Dequeue();
        MonsterController controller = monster.GetComponent<MonsterController>();

        monster.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
        controller?.ResetState();

        monster.SetActive(true);
        _activeMonsters.Add(monster);
        return true;
    }

    private void PrewarmPool()
    {
        if (monsterPrefab == null)
            return;

        int desiredPoolCount = Mathf.Max(maxMonsters, 0);
        while (_monsterPool.Count + _activeMonsters.Count < desiredPoolCount)
        {
            GameObject monster = Instantiate(monsterPrefab);
            RegisterForPooling(monster);
            _monsterPool.Enqueue(monster);
        }
    }

    private void RegisterForPooling(GameObject monster)
    {
        monster.SetActive(false);
        MonsterController controller = monster.GetComponent<MonsterController>();
        if (controller != null)
        {
            controller.SetReturnToPool(ReturnMonsterToPool);
            controller.ResetState();
        }

        Rigidbody2D rb = monster.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void ReturnMonsterToPool(MonsterController controller)
    {
        if (controller == null)
            return;

        GameObject monster = controller.gameObject;
        if (!_activeMonsters.Remove(monster))
            return;

        RegisterForPooling(monster);
        _monsterPool.Enqueue(monster);
    }

    public void SetSpawningEnabled(bool enabled)
    {
        _spawningEnabled = enabled;
        _spawnTimer = Mathf.Min(spawnInterval, Mathf.Max(0f, _spawnTimer));
    }

    public void UpdateSpawnSettings(float interval, int maxMonstersCount)
    {
        spawnInterval = Mathf.Max(0.05f, interval);
        maxMonsters = Mathf.Max(0, maxMonstersCount);
        _spawnTimer = Mathf.Min(spawnInterval, Mathf.Max(0f, _spawnTimer));
        PrewarmPool();
    }

    public void DespawnAll()
    {
        for (int i = _activeMonsters.Count - 1; i >= 0; i--)
        {
            GameObject monster = _activeMonsters[i];
            RegisterForPooling(monster);
            _monsterPool.Enqueue(monster);
        }

        _activeMonsters.Clear();
        _spawnTimer = spawnInterval;
        _spawnsThisFrame = 0;
    }

    private Vector3 GetSpawnPositionAroundPlayer()
    {
        Camera cam = Camera.main;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float screenRadius = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight);
        float spawnRadius = screenRadius + spawnBuffer;

        float angleDeg = Random.Range(0f, 360f);
        float angleRad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

        Vector3 center = player.position;
        Vector3 spawnPos = center + (Vector3)(dir * spawnRadius);

        return spawnPos;
    }
}
