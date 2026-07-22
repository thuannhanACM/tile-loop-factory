using System;
using DG.Tweening;
using UnityEngine;

public class TileItem : BeltItem
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Renderer _renderer;
    
    private TileType _type;
    public TileType Type => _type;
    public event Action<TileItem> Clicked;

    void Start()
    {
        ApplyVisuals();
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

        var targetLocalEuler = transform.localEulerAngles;
        targetLocalEuler.x = -90f;

        var sequence = DOTween.Sequence();
        sequence.Join(transform.DOMove(targetPosition, duration));
        sequence.Join(transform.DOLocalRotate(targetLocalEuler, duration));
        sequence.OnComplete(() => onComplete?.Invoke());
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
        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (_type == null) return;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = _type.Icon;
        }

        if (_renderer != null)
        {
            _renderer.material.color = _type.Color;
        }
    }
}
