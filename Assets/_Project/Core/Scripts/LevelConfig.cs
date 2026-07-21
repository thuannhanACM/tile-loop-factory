using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tile Loop Factory/Level Config", fileName = "LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Serializable]
    public struct TileTypeCount
    {
        [SerializeField] private TileType _type;
        [SerializeField] private int _count;

        public TileType Type => _type;
        public int Count => _count;
    }

    [SerializeField] private string _levelName;
    [SerializeField] private LevelController _levelPrefab;
    [SerializeField] private List<TileTypeCount> _tileSpawns = new();
    [SerializeField] private List<TileTypeCount> _collectGoals = new();

    public string LevelName => _levelName;
    public LevelController LevelPrefab => _levelPrefab;
    public IReadOnlyList<TileTypeCount> TileSpawns => _tileSpawns;
    public IReadOnlyList<TileTypeCount> CollectGoals => _collectGoals;

    public int TotalTileCount
    {
        get
        {
            int total = 0;
            foreach (var entry in _tileSpawns)
            {
                total += entry.Count;
            }
            return total;
        }
    }

    public List<TileType> BuildSpawnSequence(bool shuffle = true)
    {
        var sequence = new List<TileType>(TotalTileCount);
        foreach (var entry in _tileSpawns)
        {
            for (int i = 0; i < entry.Count; i++)
            {
                sequence.Add(entry.Type);
            }
        }

        if (shuffle)
        {
            for (int i = sequence.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (sequence[i], sequence[swapIndex]) = (sequence[swapIndex], sequence[i]);
            }
        }

        return sequence;
    }
}