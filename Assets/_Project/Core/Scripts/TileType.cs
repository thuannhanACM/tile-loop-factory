using UnityEngine;

[CreateAssetMenu(menuName = "Tile Loop Factory/Tile Type", fileName = "TileType")]
public class TileType : ScriptableObject
{
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private Color _color = Color.white;

    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public Color Color => _color;
}

