using System;
using UnityEngine;

/// <summary>
/// Attached at runtime to a pooled ParticleSystem instance. Remembers which prefab it came from,
/// notifies an optional callback when the (non-looping) system finishes, and returns itself to
/// the VfxPool.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class PooledParticle : MonoBehaviour
{
    private VfxPool _pool;
    private ParticleSystem _particles;
    private Action _onStopped;

    /// <summary>The prefab this instance was created from — used as the pool key.</summary>
    public ParticleSystem Prefab { get; private set; }

    /// <summary>Called once, right after instantiation, to wire the pool and force stop-callback behaviour.</summary>
    public void Initialize(VfxPool pool, ParticleSystem prefab)
    {
        _pool = pool;
        Prefab = prefab;
        _particles = GetComponent<ParticleSystem>();

        var main = _particles.main;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.Callback; // fire OnParticleSystemStopped so we can recycle
    }

    /// <summary>Restarts the effect from scratch; onStopped runs once the effect finishes playing.</summary>
    public void Play(Action onStopped)
    {
        _onStopped = onStopped;
        _particles.Clear(true);
        _particles.Play(true);
    }

    private void OnParticleSystemStopped()
    {
        var callback = _onStopped;
        _onStopped = null;

        if (_pool != null)
        {
            _pool.Return(this);
        }
        else
        {
            gameObject.SetActive(false);
        }

        callback?.Invoke();
    }
}
