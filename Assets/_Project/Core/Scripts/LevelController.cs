using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [SerializeField] private ConveyorBelt _conveyorBelt;
    [SerializeField] private float _spawnInterval = 0.5f;
    [SerializeField] private float _spawnDelay = 0f;

    public ConveyorBelt ConveyorBelt => _conveyorBelt;

    private readonly Queue<TileType> _pendingTypes = new();

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
        _conveyorBelt.TileRemoved += OnTileRemovedFromConveyor;
    }

    void OnDestroy()
    {
        if (_conveyorBelt != null)
        {
            _conveyorBelt.TileRemoved -= OnTileRemovedFromConveyor;
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
        if (_pendingTypes.Count == 0) return;

        var type = _pendingTypes.Dequeue();
        var tile = PoolObjectManager.Instance.Get();
        tile.SetType(type);
        _conveyorBelt.AddTile(tile);
    }

    private void OnTileRemovedFromConveyor(TileItem tile, TileRemovalReason reason)
    {
        if (reason == TileRemovalReason.ReachedEnd)
        {
            // Not clicked in time: put it back at the end of the spawn queue instead of
            // losing it — it'll be spawned again in its turn, respecting _spawnInterval.
            _pendingTypes.Enqueue(tile.Type);
            PoolObjectManager.Instance.Release(tile);
        }
    }
}