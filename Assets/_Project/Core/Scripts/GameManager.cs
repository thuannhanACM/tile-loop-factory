using System;
using System.Collections.Generic;
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
    [SerializeField] private LevelGoalsView _goalsView;
    [SerializeField] private Canvas _uiCanvas;
    [SerializeField] private float _matchPunchStrength = 0.3f;
    [SerializeField] private float _matchPunchDuration = 0.3f;
    [SerializeField] private float _collectFlyDuration = 0.4f;
    [SerializeField] private GameOverView _gameOverView;
    [SerializeField] private GameWinView _gameWinView;
    [SerializeField] private HomeView _homeView;
    [SerializeField] private GameObject _gameComponents;

    private TileItem[] _slotOccupants;
    private bool _isGameOver;
    private bool _hasWon;
    private LevelController _currentLevelController;
    private LevelConfig _currentLevelConfig;

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
            LoadLevel(_gameConfig != null ? _gameConfig.Level : null);
        }
    }

    private void OnStartGameClicked()
    {
        _homeView.Hide();
        _gameComponents?.SetActive(true);
        LoadLevel(_gameConfig != null ? _gameConfig.Level : null);
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

        if (_goalsView == null)
        {
            GameDebug.LogWarning("_goalsView is not assigned on GameManager — goal UI will not spawn.", LogTopic.UI);
        }
        else
        {
            _goalsView.Setup(levelConfig.CollectGoals);
        }
    }

    private void OnTileRemovedFromConveyor(TileItem tile, TileRemovalReason reason)
    {
        if (reason == TileRemovalReason.Clicked)
        {
            TryPlaceTileInSlot(tile);
        }
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
        MoveTileToSlot(tile, targetIndex, () => CheckForMatch(tile.Type));
        return true;
    }

    private void CheckForMatch(TileType type)
    {
        int count = 0;
        foreach (var occupant in _slotOccupants)
        {
            if (occupant != null && occupant.Type == type) count++;
        }

        if (count >= MatchCount)
        {
            ClearMatchedTiles(type);
        }
        else if (IsTrayFull())
        {
            TriggerGameOver();
        }
    }

    private bool IsTrayFull()
    {
        foreach (var occupant in _slotOccupants)
        {
            if (occupant == null) return false;
        }

        return true;
    }

    private void TriggerGameOver()
    {
        if (_isGameOver) return;
        _isGameOver = true;

        Time.timeScale = 0f;

        var statuses = _goalsView != null ? _goalsView.GetStatuses() : new List<LevelGoalsView.GoalStatus>();
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

    /// <summary>Checks whether every collect goal has reached its target amount; if so, triggers the win screen.</summary>
    private void CheckForWin()
    {
        if (_goalsView == null) return;

        var statuses = _goalsView.GetStatuses();
        if (statuses.Count == 0) return;

        foreach (var status in statuses)
        {
            if (status.Current < status.Goal) return;
        }

        TriggerWin(statuses);
    }

    private void TriggerWin(List<LevelGoalsView.GoalStatus> statuses)
    {
        if (_hasWon) return;
        _hasWon = true;

        Time.timeScale = 0f;

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
        Time.timeScale = 1f;
        _gameWinView?.Hide();

        CleanupCurrentLevel();
        _goalsView?.Setup(new List<LevelConfig.TileTypeCount>());
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
    }

    private void ClearMatchedTiles(TileType type)
    {
        var matched = new List<TileItem>(MatchCount);

        for (int i = 0; i < _slotOccupants.Length && matched.Count < MatchCount; i++)
        {
            if (_slotOccupants[i] != null && _slotOccupants[i].Type == type)
            {
                matched.Add(_slotOccupants[i]);
                _slotOccupants[i] = null;
            }
        }

        var goalView = _goalsView != null ? _goalsView.GetView(type) : null;

        foreach (var tile in matched)
        {
            var targetPosition = goalView != null
                ? GetWorldPositionForUI(goalView.ViewPoint, tile.transform.position)
                : tile.transform.position;

            tile.PlayCollectSequence(targetPosition, _matchPunchStrength, _matchPunchDuration, _collectFlyDuration, () =>
            {
                _goalsView?.ReportCollected(type);
                PoolObjectManager.Instance.Release(tile);
                CheckForWin();
            });
        }

        CompactSlots();
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

    /// <summary>Converts a UI element's screen position into a world position at the same camera-distance as referenceWorldPosition.</summary>
    private Vector3 GetWorldPositionForUI(RectTransform uiTarget, Vector3 referenceWorldPosition)
    {
        var worldCamera = Camera.main;
        if (worldCamera == null) return uiTarget.position;

        var uiCamera = _uiCanvas != null && _uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _uiCanvas.worldCamera
            : null;

        Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, uiTarget.position);
        screenPoint.z = worldCamera.WorldToScreenPoint(referenceWorldPosition).z;
        return worldCamera.ScreenToWorldPoint(screenPoint);
    }

    /// <summary>Flies a tile to the given slot's anchor, reparenting it there once the tween completes.</summary>
    private void MoveTileToSlot(TileItem tile, int slotIndex, Action onComplete = null)
    {
        var anchor = _slotAnchors[slotIndex];
        tile.FlyTo(anchor.position, _flyToSlotDuration, () =>
        {
            tile.transform.SetParent(anchor);
            onComplete?.Invoke();
        });
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
