using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [SerializeField] private List<ConveyorBelt> _conveyorBelts = new();
    [SerializeField] private float _spawnInterval = 0.5f;
    [SerializeField] private float _spawnDelay = 0f;
    [SerializeField] private List<TileGoalBox> _goalBoxes = new();

    public IReadOnlyList<ConveyorBelt> ConveyorBelts => _conveyorBelts;
    public IReadOnlyList<TileGoalBox> GoalBoxes => _goalBoxes;

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

        if (_goalBoxes.Count == 0)
        {
            GameDebug.LogWarning("_goalBoxes is empty on LevelController — there is nothing to win.", LogTopic.Gameplay);
        }
    }

    /// <summary>
    /// Picks the single box every tile in a match batch should fly to for the given TileType.
    /// Among boxes tracking that type, prefers the Unlocked, not-yet-complete one closest to
    /// completion (least remaining) so overshoot from a 3-tile match is minimized — AddProgress
    /// itself clamps at the goal, so it never receives more than it needs. Falls back to any
    /// same-type box (even Locked/complete) so tiles always have a real destination to fly to.
    /// </summary>
    public TileGoalBox PickGoalBox(TileType type)
    {
        TileGoalBox fallback = null;
        TileGoalBox best = null;
        int bestRemaining = int.MaxValue;

        foreach (var box in _goalBoxes)
        {
            if (box == null || box.GoalType != type) continue;

            fallback ??= box;

            if (box.State != TileGoalBox.BoxState.Unlocked || box.IsComplete) continue;

            int remaining = box.GoalAmount - box.CurrentAmount;
            if (remaining < bestRemaining)
            {
                bestRemaining = remaining;
                best = box;
            }
        }

        return best != null ? best : fallback;
    }

    /// <summary>The win condition: every goal box in the level must be complete.</summary>
    public bool AreAllGoalsComplete()
    {
        if (_goalBoxes.Count == 0) return false;

        foreach (var box in _goalBoxes)
        {
            if (box == null || !box.IsComplete) return false;
        }

        return true;
    }

    public List<LevelGoalsView.GoalStatus> GetGoalStatuses()
    {
        var statuses = new List<LevelGoalsView.GoalStatus>(_goalBoxes.Count);
        foreach (var box in _goalBoxes)
        {
            if (box == null) continue;
            statuses.Add(new LevelGoalsView.GoalStatus(box.GoalType, box.CurrentAmount, box.GoalAmount));
        }

        return statuses;
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