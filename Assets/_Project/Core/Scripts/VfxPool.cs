using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic pool for one-shot particle VFX, keyed by prefab. Lives on a scene-root object so
/// pooled effects survive level reloads. Instances return themselves via PooledParticle when done.
/// </summary>
public class VfxPool : MonoBehaviour
{
    public static VfxPool Instance { get; private set; }

    [SerializeField] private Transform _poolRoot;

    private readonly Dictionary<ParticleSystem, Queue<PooledParticle>> _pools = new();

    private void Awake()
    {
        // Awake (not Start) so callers in other objects' Start()/click handlers find the pool ready.
        Instance = this;
    }

    /// <summary>Plays the given VFX prefab at a world position, reusing a pooled instance when possible.
    /// onComplete (if any) fires once the effect finishes playing — or immediately if there is no prefab.</summary>
    public void Play(ParticleSystem prefab, Vector3 worldPosition, Action onComplete = null)
    {
        if (prefab == null)
        {
            onComplete?.Invoke();
            return;
        }

        var instance = GetFromPool(prefab);
        instance.transform.SetParent(_poolRoot != null ? _poolRoot : transform, false);
        instance.transform.position = worldPosition;
        instance.gameObject.SetActive(true);
        instance.Play(onComplete);
    }

    private PooledParticle GetFromPool(ParticleSystem prefab)
    {
        if (_pools.TryGetValue(prefab, out var queue) && queue.Count > 0)
        {
            return queue.Dequeue();
        }

        var particles = Instantiate(prefab);
        var pooled = particles.GetComponent<PooledParticle>();
        if (pooled == null)
        {
            pooled = particles.gameObject.AddComponent<PooledParticle>();
        }

        pooled.Initialize(this, prefab);
        return pooled;
    }

    /// <summary>Deactivates a finished effect and returns it to its prefab's pool.</summary>
    public void Return(PooledParticle pooled)
    {
        if (pooled == null) return;

        pooled.gameObject.SetActive(false);
        pooled.transform.SetParent(_poolRoot != null ? _poolRoot : transform, false);

        if (!_pools.TryGetValue(pooled.Prefab, out var queue))
        {
            queue = new Queue<PooledParticle>();
            _pools[pooled.Prefab] = queue;
        }

        queue.Enqueue(pooled);
    }
}
