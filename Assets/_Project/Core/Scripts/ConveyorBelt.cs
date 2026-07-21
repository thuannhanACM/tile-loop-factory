using System;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    [SerializeField] LineRenderer _lineRenderer;
    [SerializeField] float _scrollSpeed = 1f;
    [SerializeField] private BeltItem _beltItemArrow;
    [SerializeField] private Transform _arrowRoot;
    [SerializeField] private float _arrowSpawnInterval = 2f;

    private Material _material;
    private float _textureOffset;

    private Vector3[] _pathPoints;
    private float[] _cumulativeDistances;
    private float _pathLength;
    private readonly Dictionary<TileItem, float> _progress = new();
    private readonly List<TileItem> _activeTiles = new();

    private readonly Dictionary<BeltItem, float> _arrowProgress = new();
    private readonly List<BeltItem> _activeArrows = new();
    private float _arrowSpawnTimer;

    public event Action<TileItem, TileRemovalReason> TileRemoved;

    /// <summary>Stops tracking every active tile (properly unsubscribing Clicked) and hands each one to onEachReleased.</summary>
    public void ReleaseAllTiles(Action<TileItem> onEachReleased)
    {
        foreach (var tile in _activeTiles)
        {
            tile.Clicked -= OnTileClicked;
            onEachReleased?.Invoke(tile);
        }

        _activeTiles.Clear();
        _progress.Clear();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _material = _lineRenderer.material;

        BuildPath();
        SpawnArrow();
    }

    /// <summary>Pushes a tile onto the belt. Caller (LevelController) owns spawning/pooling.</summary>
    public void AddTile(TileItem tile)
    {
        tile.gameObject.SetActive(true);
        _progress[tile] = 0f;
        _activeTiles.Add(tile);
        tile.Clicked += OnTileClicked;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateArrowSpawn();
    }

    private void FixedUpdate()
    {
        MoveTiles();
        MoveArrows();
    }

    private void UpdateArrowSpawn()
    {
        _arrowSpawnTimer += Time.deltaTime;
        if (_arrowSpawnTimer >= _arrowSpawnInterval)
        {
            _arrowSpawnTimer -= _arrowSpawnInterval;
            SpawnArrow();
        }
    }

    private void SpawnArrow()
    {
        if (_beltItemArrow == null || _arrowRoot == null) return;

        var arrow = Instantiate(_beltItemArrow, _arrowRoot);
        _arrowProgress[arrow] = 0f;
        _activeArrows.Add(arrow);
    }

    private void MoveArrows()
    {
        if (_pathPoints == null || _pathPoints.Length < 2) return;

        for (int i = _activeArrows.Count - 1; i >= 0; i--)
        {
            var arrow = _activeArrows[i];
            float distance = Mathf.Min(_arrowProgress[arrow] + Mathf.Abs(_scrollSpeed) * Time.deltaTime, _pathLength);

            if (distance >= _pathLength)
            {
                _arrowProgress.Remove(arrow);
                _activeArrows.RemoveAt(i);
                Destroy(arrow.gameObject);
                continue;
            }

            _arrowProgress[arrow] = distance;
            var point = GetPointAtDistance(distance);
            var newPosition = new Vector3(point.x, point.y, point.z - 0.1f);

            var direction = newPosition - arrow.transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                arrow.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            arrow.transform.position = newPosition;
        }
    }

    private void OnTileClicked(TileItem tile)
    {
        _activeTiles.Remove(tile);
        ReleaseTile(tile, TileRemovalReason.Clicked);
    }

    /// <summary>Stops tracking the tile and hands it back to whoever is listening (LevelController, GameManager).</summary>
    private void ReleaseTile(TileItem tile, TileRemovalReason reason)
    {
        _progress.Remove(tile);
        tile.Clicked -= OnTileClicked;
        TileRemoved?.Invoke(tile, reason);
    }

    private void BuildPath()
    {
        int count = _lineRenderer.positionCount;
        _pathPoints = new Vector3[count];
        _lineRenderer.GetPositions(_pathPoints);

        if (!_lineRenderer.useWorldSpace)
        {
            for (int i = 0; i < count; i++)
            {
                _pathPoints[i] = _lineRenderer.transform.TransformPoint(_pathPoints[i]);
            }
        }

        _cumulativeDistances = new float[count];
        _pathLength = 0f;
        for (int i = 1; i < count; i++)
        {
            _pathLength += Vector3.Distance(_pathPoints[i - 1], _pathPoints[i]);
            _cumulativeDistances[i] = _pathLength;
        }
    }

    private void MoveTiles()
    {
        if (_pathPoints == null || _pathPoints.Length < 2) return;

        for (int i = _activeTiles.Count - 1; i >= 0; i--)
        {
            var tile = _activeTiles[i];
            float distance = Mathf.Min(_progress[tile] + Mathf.Abs(_scrollSpeed) * Time.deltaTime, _pathLength);

            if (distance >= _pathLength)
            {
                _activeTiles.RemoveAt(i);
                ReleaseTile(tile, TileRemovalReason.ReachedEnd);
                continue;
            }

            _progress[tile] = distance;
            var point = GetPointAtDistance(distance);
            tile.transform.position = new Vector3(point.x, point.y, point.z - 0.1f);
        }
    }

    private Vector3 GetPointAtDistance(float distance)
    {
        for (int i = 1; i < _cumulativeDistances.Length; i++)
        {
            if (distance <= _cumulativeDistances[i])
            {
                float segmentLength = _cumulativeDistances[i] - _cumulativeDistances[i - 1];
                float t = segmentLength > 0f ? (distance - _cumulativeDistances[i - 1]) / segmentLength : 0f;
                return Vector3.Lerp(_pathPoints[i - 1], _pathPoints[i], t);
            }
        }
        return _pathPoints[_pathPoints.Length - 1];
    }
}