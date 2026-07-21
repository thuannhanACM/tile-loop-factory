using UnityEngine;

[CreateAssetMenu(menuName = "Tile Loop Factory/Game Config", fileName = "GameConfig")]
public class GameConfig : ScriptableObject
{
    [SerializeField] private LevelConfig _level;
    [SerializeField] private LevelConfig _bonusLevel;

    public LevelConfig Level => _level;
    public LevelConfig BonusLevel => _bonusLevel;
}