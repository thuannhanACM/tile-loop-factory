using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TileItem : BeltItem
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private TileType _type;

    public TileType Type => _type;
    public event Action<TileItem> Clicked;

    void Start()
    {
        ApplyIcon();
    }

    void OnMouseDown()
    {
        Clicked?.Invoke(this);
    }

    public void FlyTo(Vector3 targetPosition, float duration, Action onComplete = null)
    {
        transform.DOKill();
        transform.DOMove(targetPosition, duration).OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>Small punch/shake, then flies to targetPosition; invokes onComplete when done.</summary>
    public void PlayCollectSequence(Vector3 targetPosition, float punchStrength, float punchDuration, float flyDuration, Action onComplete)
    {
        transform.DOKill();

        var sequence = DOTween.Sequence();
        sequence.Append(transform.DOPunchPosition(Vector3.up * punchStrength, punchDuration, 6, 1f));
        sequence.Append(transform.DOMove(targetPosition, flyDuration));
        sequence.Join(transform.DOScale(Vector3.zero, flyDuration));
        sequence.OnComplete(() => onComplete?.Invoke());
    }

    public void SetType(TileType type)
    {
        _type = type;
        ApplyIcon();
    }

    private void ApplyIcon()
    {
        if (_spriteRenderer != null && _type != null)
        {
            _spriteRenderer.sprite = _type.Icon;
        }
    }
}
