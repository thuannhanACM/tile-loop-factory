using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [SerializeField] private List<ConveyorBelt> _conveyorBelts = new();
    [SerializeField] private float _spawnInterval = 0.5f;
    [SerializeField] private float _spawnDelay = 0f;

    public IReadOnlyList<ConveyorBelt> ConveyorBelts => _conveyorBelts;

    private readonly Queue<TileType> _pendingTypes = new();
    private readonly List<Action<TileItem, TileRemovalReason>> _tileRemovedHandlers = new();

    private float _spawnTimer;
    private bool _spawnStarted;

    /// <summary>Called by whoever spawns this level (GameManager) right after Instantiate.</summary>
    public void Initialize(IEnumerable<TileType> spawnSequence)
    {
        _pendingTypes.Clear();
        foreach (var type in spawnSequence)
        {
            _pendingTypes.Enqueue(type);
        }
    }

    void Start()
    {
        for (int i = 0; i < _conveyorBelts.Count; i++)
        {
            int beltIndex = i;
            Action<TileItem, TileRemovalReason> handler = (tile, reason) => OnTileRemovedFromConveyor(beltIndex, tile, reason);
            _tileRemovedHandlers.Add(handler);
            _conveyorBelts[i].TileRemoved += handler;
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < _conveyorBelts.Count && i < _tileRemovedHandlers.Count; i++)
        {
            if (_conveyorBelts[i] != null)
            {
                _conveyorBelts[i].TileRemoved -= _tileRemovedHandlers[i];
            }
        }
    }

    void Update()
    {
        UpdateSpawnQueue();
    }

    private void UpdateSpawnQueue()
    {
        if (_pendingTypes.Count == 0) return;

        _spawnTimer += Time.deltaTime;
        float threshold = _spawnStarted ? _spawnInterval : _spawnDelay;
        if (_spawnTimer >= threshold)
        {
            _spawnTimer -= threshold;
            _spawnStarted = true;
            SpawnNextTile();
        }
    }

    private void SpawnNextTile()
    {
        if (_pendingTypes.Count == 0 || _conveyorBelts.Count == 0) return;

        var type = _pendingTypes.Dequeue();
        var tile = PoolObjectManager.Instance.Get();
        tile.SetType(type);
        _conveyorBelts[0].AddTile(tile);
    }

    private void OnTileRemovedFromConveyor(int beltIndex, TileItem tile, TileRemovalReason reason)
    {
        if (reason != TileRemovalReason.ReachedEnd) return;

        int nextIndex = beltIndex + 1;
        if (nextIndex < _conveyorBelts.Count)
        {
            // Hand the tile straight to the start of the next belt in the chain.
            _conveyorBelts[nextIndex].AddTile(tile);
            return;
        }

        // Last belt in the chain and not clicked in time: put it back at the end of the
        // spawn queue instead of losing it — it'll be spawned again, respecting _spawnInterval.
        _pendingTypes.Enqueue(tile.Type);
        PoolObjectManager.Instance.Release(tile);
    }
}