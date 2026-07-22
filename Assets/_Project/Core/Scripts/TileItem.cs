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
        // Cancel any in-flight spawn/near-end punch-scale tween so the click's own
        // animation (fly-to-slot) always starts from a fully visible scale.
        transform.DOKill();
        transform.localScale = Vector3.one;

        Clicked?.Invoke(this);
    }

    public void FlyTo(Vector3 targetPosition, float duration, Action onComplete = null)
    {
        transform.DOKill();
        transform.DOMove(targetPosition, duration).OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>Pops in from scale 0 to 1 with a juicy overshoot.</summary>
    public void PlaySpawnAnimation(float duration)
    {
        transform.DOKill();
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
    }

    /// <summary>Light punch-scale (juicy emphasis), then shrinks down to scale 0.</summary>
    public void PlayNearEndEffect(float punchStrength, float punchDuration, float shrinkDuration)
    {
        transform.DOKill();

        var sequence = DOTween.Sequence();
        sequence.Append(transform.DOPunchScale(Vector3.one * punchStrength, punchDuration, 6, 1f));
        sequence.Append(transform.DOScale(Vector3.zero, shrinkDuration));
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
