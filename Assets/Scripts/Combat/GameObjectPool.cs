using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Lightweight pooling utility for projectiles and VFX.
/// Wraps Unity's ObjectPool and attaches a <see cref="PooledObject"/> marker to every spawned instance.
/// </summary>
public static class GameObjectPool
{
    private static readonly Dictionary<GameObject, ObjectPool<GameObject>> Pools = new();

    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        ObjectPool<GameObject> pool = GetOrCreatePool(prefab);
        GameObject instance = pool.Get();
        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    public static void Release(GameObject instance)
    {
        if (instance == null)
            return;

        PooledObject pooled = instance.GetComponent<PooledObject>();
        if (pooled != null && pooled.Pool != null)
        {
            pooled.Pool.Release(instance);
        }
        else
        {
            Object.Destroy(instance);
        }
    }

    private static ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
    {
        if (Pools.TryGetValue(prefab, out ObjectPool<GameObject> existing))
            return existing;

        ObjectPool<GameObject> pool = null;
        pool = new ObjectPool<GameObject>(
            () => CreateInstance(prefab, () => pool),
            actionOnGet: go => go.SetActive(true),
            actionOnRelease: go =>
            {
                go.SetActive(false);
                PooledObject pooled = go.GetComponent<PooledObject>();
                if (pooled != null)
                {
                    pooled.OnReleasedToPool();
                }
            },
            actionOnDestroy: go => Object.Destroy(go),
            collectionCheck: false,
            defaultCapacity: 8,
            maxSize: 128);

        Pools[prefab] = pool;
        return pool;
    }

    private static GameObject CreateInstance(GameObject prefab, System.Func<ObjectPool<GameObject>> poolAccessor)
    {
        GameObject instance = Object.Instantiate(prefab);
        PooledObject pooled = instance.GetComponent<PooledObject>();
        if (pooled == null)
        {
            pooled = instance.AddComponent<PooledObject>();
        }

        pooled.SetPool(poolAccessor(), prefab);
        instance.SetActive(false);
        return instance;
    }
}

/// <summary>
/// Marker component attached to pooled objects so they can return themselves to the originating pool.
/// </summary>
public class PooledObject : MonoBehaviour
{
    public ObjectPool<GameObject> Pool { get; private set; }
    public GameObject Prefab { get; private set; }

    private bool _releaseScheduled;

    public void SetPool(ObjectPool<GameObject> pool, GameObject prefab)
    {
        Pool = pool;
        Prefab = prefab;
    }

    public void Release()
    {
        if (Pool != null)
        {
            Pool.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ReleaseAfter(float time)
    {
        if (_releaseScheduled)
            return;

        _releaseScheduled = true;
        StartCoroutine(ReleaseAfterRoutine(time));
    }

    public void OnReleasedToPool()
    {
        _releaseScheduled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private IEnumerator ReleaseAfterRoutine(float time)
    {
        yield return new WaitForSeconds(time);
        Release();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _releaseScheduled = false;
    }
}
