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

    private float _spawnTimer;
    private readonly List<GameObject> _monsters = new List<GameObject>();

    private void Update()
    {
        if (monsterPrefab == null || player == null || Camera.main == null)
            return;

        _spawnTimer -= Time.deltaTime;

        if (_spawnTimer <= 0f)
        {
            _spawnTimer = spawnInterval;

            _monsters.RemoveAll(m => m == null);

            if (_monsters.Count >= maxMonsters)
                return;

            Vector3 spawnPos = GetSpawnPositionAroundPlayer();
            GameObject monster = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
            _monsters.Add(monster);
        }
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
