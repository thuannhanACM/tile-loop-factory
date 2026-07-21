using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoalItemView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private RectTransform _viewPoint;
    [SerializeField] private Color _completedColor = Color.green;
    [SerializeField] private float _hitAnimationDuration = 0.5f;

    private Color _defaultColor;

    private int _current;
    private int _goal;

    public TileType Type { get; private set; }
    public int Current => _current;
    public int Goal => _goal;

    /// <summary>Target point for fly-to-goal animations. Falls back to this element's own RectTransform.</summary>
    public RectTransform ViewPoint => _viewPoint != null ? _viewPoint : (RectTransform)transform;

    public void Setup(TileType type, int goal, int current = 0)
    {
        Type = type;
        _goal = goal;
        _current = current;

        if (_icon != null && type != null)
        {
            _icon.sprite = type.Icon;
        }

        if (_amountText != null)
        {
            _defaultColor = _amountText.color;
        }

        Refresh();
    }

    public void SetCurrent(int current)
    {
        _current = current;
        Refresh();
        PlayHitAnimation();
    }

    private void Refresh()
    {
        if (_amountText == null) return;

        _amountText.text = $"{_current}/{_goal}";
        _amountText.color = _current >= _goal ? _completedColor : _defaultColor;
    }

    /// <summary>Punch-scale on the whole view + on the text, played whenever a tile lands on this goal.</summary>
    private void PlayHitAnimation()
    {
        transform.DOKill();
        transform.DOPunchScale(Vector3.one * 0.3f, _hitAnimationDuration, 6, 1f);

        if (_amountText != null)
        {
            _amountText.transform.DOKill();
            _amountText.transform.DOPunchScale(Vector3.one * 0.3f, _hitAnimationDuration, 6, 1f);
        }
    }
}