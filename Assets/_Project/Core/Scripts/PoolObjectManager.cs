using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PoolObjectManager : MonoBehaviour
{
    private static PoolObjectManager _instance;
    public static PoolObjectManager Instance => _instance;
    
    [SerializeField] private TileItem _prefab;
    [SerializeField] private Transform _poolRoot;

    private readonly Queue<TileItem> _pool = new();

    void Start()
    {
        _instance = this;
    }

    public TileItem Get()
    {
        var parent = _poolRoot != null ? _poolRoot : transform;

        if (_pool.Count > 0)
        {
            var tile = _pool.Dequeue();
            tile.transform.SetParent(parent);
            tile.transform.localScale = Vector3.one;
            return tile;
        }

        return Instantiate(_prefab, parent);
    }

    public void Release(TileItem tile)
    {
        tile.transform.DOKill();
        tile.gameObject.SetActive(false);

        if (_poolRoot != null)
        {
            tile.transform.SetParent(_poolRoot);
        }

        tile.transform.localPosition = Vector3.zero;

        _pool.Enqueue(tile);
    }
}