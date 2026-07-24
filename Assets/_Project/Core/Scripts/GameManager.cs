using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private const int MatchCount = 3;

    private static GameManager _instance;
    public static GameManager Instance => _instance;

    [SerializeField] private GameConfig _gameConfig;
    [SerializeField] private Transform _levelRoot;
    [SerializeField] private int _maxFrameRate = 60;
    [SerializeField] private Transform[] _slotAnchors = new Transform[4];
    [SerializeField] private float _flyToSlotDuration = 0.3f;
    [SerializeField] private float _clickPopupHeight = 0.5f;
    [SerializeField] private float _clickPopupDuration = 0.15f;
    [SerializeField] private float _matchPunchStrength = 0.3f;
    [SerializeField] private float _matchPunchDuration = 0.3f;
    [SerializeField] private float _collectFlyDuration = 0.4f;
    [SerializeField] private float _compactDelaySeconds = 0.5f;
    [SerializeField] private float _slotPunchStrength = 0.25f;
    [SerializeField] private float _slotPunchDuration = 0.2f;
    [SerializeField] private GameOverView _gameOverView;
    [SerializeField] private GameWinView _gameWinView;
    [SerializeField] private HomeView _homeView;
    [SerializeField] private GameObject _gameComponents;
    [SerializeField] private int _levelIndex;

    private TileItem[] _slotOccupants;
    private bool _isGameOver;
    private bool _hasWon;
    private LevelController _currentLevelController;
    private LevelConfig _currentLevelConfig;
    private float _timeRemaining;
    private bool _timerActive;

    void Start()
    {
        _instance = this;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = _maxFrameRate;

        _slotOccupants = new TileItem[_slotAnchors.Length];

        if (_gameComponents == null)
        {
            GameDebug.LogWarning("_gameComponents is not assigned on GameManager.", LogTopic.General);
        }
        else
        {
            _gameComponents.SetActive(false);
        }

        if (_gameOverView != null)
        {
            _gameOverView.RestartClicked += RestartLevel;
        }

        if (_gameWinView != null)
        {
            _gameWinView.BonusLevelClicked += GoToBonusLevel;
            _gameWinView.BackClicked += OnBackToHomeClicked;
        }

        if (_homeView != null)
        {
            _homeView.StartGameClicked += OnStartGameClicked;
            _homeView.Show();
        }
        else
        {
            _gameComponents?.SetActive(true);
            var level = _gameConfig != null ? _gameConfig.Levels.Length > _levelIndex ? _gameConfig.Levels[_levelIndex] : _gameConfig.Levels[0] : null;
            LoadLevel(level);
        }
    }

    void Update()
    {
        if (!_timerActive || _isGameOver || _hasWon) return;

        _timeRemaining -= Time.deltaTime;
        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            _timerActive = false;
            UIManager.Instance?.UpdateTimer(_timeRemaining);
            TriggerGameOver();
            return;
        }

        UIManager.Instance?.UpdateTimer(_timeRemaining);
    }

    private void OnStartGameClicked()
    {
        _homeView.Hide();
        _gameComponents?.SetActive(true);
        var level = _gameConfig != null ? _gameConfig.Levels.Length > _levelIndex ? _gameConfig.Levels[_levelIndex] : _gameConfig.Levels[0] : null;
        LoadLevel(level);
    }

    private void LoadLevel(LevelConfig levelConfig)
    {
        if (levelConfig == null || levelConfig.LevelPrefab == null)
        {
            GameDebug.LogWarning("No LevelConfig/LevelPrefab assigned — level did not spawn.", LogTopic.UI);
            return;
        }

        _currentLevelConfig = levelConfig;

        var parent = _levelRoot != null ? _levelRoot : transform;
        _currentLevelController = Instantiate(levelConfig.LevelPrefab, parent);
        _currentLevelController.Initialize(levelConfig.BuildSpawnSequence());

        foreach (var belt in _currentLevelController.ConveyorBelts)
        {
            belt.TileRemoved += OnTileRemovedFromConveyor;
        }

        UIManager.Instance.ShowLevelName(levelConfig.LevelName);

        _timeRemaining = levelConfig.TimeLimitSeconds;
        _timerActive = true;
        UIManager.Instance.UpdateTimer(_timeRemaining);
    }

    private void OnTileRemovedFromConveyor(TileItem tile, TileRemovalReason reason)
    {
        if (reason != TileRemovalReason.Clicked) return;

        // Decide before moving the tile: if this click completes a set, the new tile and its
        // same-type mates already in the tray all fly straight to the goal box.
        if (IsMatching(tile.Type))
        {
            CollectMatch(tile);
        }
        else if (FindFirstEmptyIndex() < 0)
        {
            // No match and no free slot left to hold the tile — the tray is jammed, level lost.
            TriggerGameOver();
        }
        else
        {
            TryPlaceTileInSlot(tile);
        }
    }

    private int CountInTray(TileType type)
    {
        int count = 0;
        foreach (var occupant in _slotOccupants)
        {
            if (occupant != null && occupant.Type == type) count++;
        }

        return count;
    }

    /// <summary>A click matches only when it completes a set (this tile + its tray mates reach MatchCount)
    /// AND the level has an unlocked goal box of that type ready to receive it.</summary>
    private bool IsMatching(TileType type)
    {
        return CountInTray(type) + 1 >= MatchCount
            && _currentLevelController != null
            && _currentLevelController.HasUnlockedGoalBox(type);
    }

    /// <summary>Pushes the current tray fill to the UI so tension can rise with the number of tiles held.</summary>
    private void NotifyTrayTension()
    {
        if (UIManager.Instance == null) return;

        int count = 0;
        foreach (var occupant in _slotOccupants)
        {
            if (occupant != null) count++;
        }

        UIManager.Instance.SetTrayTension(count, _slotOccupants.Length);
    }

    /// <summary>
    /// No slot of the same type -> first available slot.
    /// Otherwise -> right after the end of the same-type chain; if that slot is taken,
    /// the occupant flies to the next available slot in parallel with the selected tile.
    /// </summary>
    private bool TryPlaceTileInSlot(TileItem tile)
    {
        int lastSameTypeIndex = FindLastIndexOfType(tile.Type);
        int targetIndex = lastSameTypeIndex < 0 ? FindFirstEmptyIndex() : lastSameTypeIndex + 1;

        if (targetIndex >= 0 && targetIndex < _slotOccupants.Length && _slotOccupants[targetIndex] != null)
        {
            var displaced = _slotOccupants[targetIndex];
            int freeIndex = FindFirstEmptyIndex();
            if (freeIndex < 0) return false;

            _slotOccupants[targetIndex] = null;
            _slotOccupants[freeIndex] = displaced;
            MoveTileToSlot(displaced, freeIndex);
        }
        else if (targetIndex >= _slotOccupants.Length)
        {
            // Chain already reaches the last slot, no room to extend it.
            targetIndex = FindFirstEmptyIndex();
        }

        if (targetIndex < 0 || targetIndex >= _slotOccupants.Length) return false;

        _slotOccupants[targetIndex] = tile;
        NotifyTrayTension();

        var anchor = _slotAnchors[targetIndex];
        tile.PlayClickPopupThenFly(anchor.position, _clickPopupHeight, _clickPopupDuration, _flyToSlotDuration, () =>
        {
            tile.transform.SetParent(anchor);
            PunchSlot(anchor);

            if (IsBoardDead()) TriggerGameOver();
        });

        return true;
    }

    /// <summary>The board can no longer produce a combo. Two ways it dies:
    /// (1) MatchCount tiles whose types have no unlocked box (any mix of types) are stuck — they can
    /// never clear and leave too few slots to assemble another combo; or (2) a full tray where no type
    /// is one tile short of matching into an unlocked box.</summary>
    private bool IsBoardDead()
    {
        int stuck = 0;
        foreach (var occupant in _slotOccupants)
        {
            if (occupant == null) continue;
            if (_currentLevelController == null || !_currentLevelController.HasUnlockedGoalBox(occupant.Type)) stuck++;
        }

        if (stuck >= MatchCount) return true;

        return IsTrayDeadlocked();
    }

    /// <summary>True when the tray is full and no type in it can still be matched — i.e. no type has
    /// enough tiles AND an unlocked goal box, so no possible next tile could ever complete a combo
    /// (and there is no room to place it either).</summary>
    private bool IsTrayDeadlocked()
    {
        foreach (var occupant in _slotOccupants)
        {
            if (occupant == null) return false; // still room to build toward a match
        }

        foreach (var occupant in _slotOccupants)
        {
            if (IsMatching(occupant.Type)) return false; // one more of this type would combo into an unlocked box
        }

        return true;
    }

    private void TriggerGameOver()
    {
        // Once the level is won (win pending or shown), a late tray-full must not flip it to game over.
        if (_isGameOver || _hasWon) return;
        _isGameOver = true;
        _timerActive = false;
        UIManager.Instance?.ResetTension();

        Time.timeScale = 0f;

        var statuses = _currentLevelController != null ? _currentLevelController.GetGoalStatuses() : new List<LevelGoalsView.GoalStatus>();
        _gameOverView?.Show(statuses);
    }

    private void RestartLevel()
    {
        _isGameOver = false;
        Time.timeScale = 1f;
        _gameOverView?.Hide();

        var levelToReload = _currentLevelConfig;
        CleanupCurrentLevel();
        LoadLevel(levelToReload);
    }

    /// <summary>
    /// Checks whether every goal box is complete. When the level is won, the countdown stops
    /// immediately but the win screen is deferred until the final box finishes vanishing.
    /// </summary>
    private void CheckForWin(TileGoalBox lastBox)
    {
        if (_hasWon) return;
        if (_currentLevelController == null || !_currentLevelController.AreAllGoalsComplete()) return;

        // Win is decided: freeze the countdown right away, but let the last box play out its
        // vanish animation before showing the win screen.
        _hasWon = true;
        _timerActive = false;
        UIManager.Instance?.ResetTension();

        if (lastBox != null && lastBox.gameObject.activeSelf)
        {
            lastBox.Vanished += OnFinalBoxVanished;
        }
        else
        {
            ShowWinScreen();
        }
    }

    private void OnFinalBoxVanished(TileGoalBox box)
    {
        box.Vanished -= OnFinalBoxVanished;
        ShowWinScreen();
    }

    private void ShowWinScreen()
    {
        Time.timeScale = 0f;

        var statuses = _currentLevelController != null ? _currentLevelController.GetGoalStatuses() : new List<LevelGoalsView.GoalStatus>();
        bool isBonusLevel = _gameConfig != null && _currentLevelConfig == _gameConfig.BonusLevel;
        _gameWinView?.Show(statuses, !isBonusLevel);
    }

    private void GoToBonusLevel()
    {
        _hasWon = false;
        Time.timeScale = 1f;
        _gameWinView?.Hide();

        CleanupCurrentLevel();
        LoadLevel(_gameConfig != null ? _gameConfig.BonusLevel : null);
    }

    private void OnBackToHomeClicked()
    {
        _hasWon = false;
        _timerActive = false;
        UIManager.Instance?.ResetTension();
        Time.timeScale = 1f;
        _gameWinView?.Hide();

        CleanupCurrentLevel();
        _gameComponents?.SetActive(false);
        _homeView?.Show();
    }

    /// <summary>Releases every tile still on the belt or in the tray back to the pool, then destroys the current LevelController.</summary>
    private void CleanupCurrentLevel()
    {
        if (_currentLevelController != null)
        {
            foreach (var belt in _currentLevelController.ConveyorBelts)
            {
                if (belt == null) continue;

                belt.TileRemoved -= OnTileRemovedFromConveyor;
                belt.ReleaseAllTiles(tile => PoolObjectManager.Instance.Release(tile));
            }

            Destroy(_currentLevelController.gameObject);
            _currentLevelController = null;
        }

        for (int i = 0; i < _slotOccupants.Length; i++)
        {
            if (_slotOccupants[i] != null)
            {
                PoolObjectManager.Instance.Release(_slotOccupants[i]);
                _slotOccupants[i] = null;
            }
        }

        NotifyTrayTension();
    }

    /// <summary>Flies the freshly clicked tile together with its same-type mates in the tray to the goal box.</summary>
    private void CollectMatch(TileItem newTile)
    {
        var type = newTile.Type;
        var matched = new List<TileItem>(MatchCount);

        // Pull the same-type tiles already sitting in the tray...
        for (int i = 0; i < _slotOccupants.Length; i++)
        {
            if (_slotOccupants[i] != null && _slotOccupants[i].Type == type)
            {
                matched.Add(_slotOccupants[i]);
                _slotOccupants[i] = null;
            }
        }

        // ...plus the newly clicked tile, which never entered a slot.
        matched.Add(newTile);
        NotifyTrayTension();

        var box = _currentLevelController != null ? _currentLevelController.PickGoalBox(type) : null;

        foreach (var tile in matched)
        {
            var targetPosition = box != null ? box.transform.position : tile.transform.position;

            tile.PlayCollectSequence(targetPosition, _matchPunchStrength, _matchPunchDuration, _collectFlyDuration, () =>
            {
                if (box != null)
                {
                    box.AddProgress(type);
                    box.PlayHitReaction();
                }

                PoolObjectManager.Instance.Release(tile);
                CheckForWin(box);
            });
        }

        // Let the matched tiles clear the tray first, then slide the survivors forward.
        DOVirtual.DelayedCall(_compactDelaySeconds, CompactSlots);
    }

    /// <summary>Shifts every remaining occupant forward to fill the gaps left by a match.</summary>
    private void CompactSlots()
    {
        var remaining = new List<TileItem>(_slotOccupants.Length);
        foreach (var occupant in _slotOccupants)
        {
            if (occupant != null) remaining.Add(occupant);
        }

        for (int i = 0; i < _slotOccupants.Length; i++)
        {
            _slotOccupants[i] = i < remaining.Count ? remaining[i] : null;
        }

        for (int i = 0; i < remaining.Count; i++)
        {
            MoveTileToSlot(remaining[i], i);
        }
    }

    /// <summary>Flies a tile to the given slot's anchor, reparenting it there once the tween completes.</summary>
    private void MoveTileToSlot(TileItem tile, int slotIndex, Action onComplete = null)
    {
        var anchor = _slotAnchors[slotIndex];
        tile.FlyTo(anchor.position, _flyToSlotDuration, () =>
        {
            tile.transform.SetParent(anchor);
            PunchSlot(anchor);
            onComplete?.Invoke();
        });
    }

    /// <summary>Punch-scales the slot anchor (and the tile now parented to it) as the tile lands.</summary>
    private void PunchSlot(Transform anchor)
    {
        if (anchor == null) return;

        // Finish any in-flight punch first so rapid re-landings don't stack or drift the scale.
        anchor.DOComplete();
        anchor.DOPunchScale(Vector3.one * _slotPunchStrength, _slotPunchDuration, 6, 1f);
    }

    private int FindLastIndexOfType(TileType type)
    {
        for (int i = _slotOccupants.Length - 1; i >= 0; i--)
        {
            if (_slotOccupants[i] != null && _slotOccupants[i].Type == type)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindFirstEmptyIndex()
    {
        for (int i = 0; i < _slotOccupants.Length; i++)
        {
            if (_slotOccupants[i] == null) return i;
        }

        return -1;
    }
}
