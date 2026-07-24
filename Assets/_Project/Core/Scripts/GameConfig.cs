using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Tile Loop Factory/Game Config", fileName = "GameConfig")]
public class GameConfig : ScriptableObject
{
    [SerializeField] private LevelConfig[] levels;
    [SerializeField] private LevelConfig _bonusLevel;

    public LevelConfig[] Levels => levels;
    public LevelConfig BonusLevel => _bonusLevel;
}