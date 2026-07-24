using System;
using DG.Tweening;
using UnityEngine;

public class TileItem : BeltItem
{
    private const float LandingPunchStrength = 0.25f;
    private const float LandingPunchDuration = 0.2f;

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
        // Kill every tween tagged to this tile (including ones nested inside a
        // Sequence, e.g. the near-end punch/shrink) before anything else, so the
        // click's own animation (fly-to-slot) always starts from a clean, fully
        // visible state.
        KillAllTweens();
        transform.localScale = Vector3.one;

        Clicked?.Invoke(this);
    }

    /// <summary>Kills every tween/sequence this tile has started, wherever it is in its lifecycle.</summary>
    private void KillAllTweens()
    {
        DOTween.Kill(this);
    }

    public void FlyTo(Vector3 targetPosition, float duration, Action onComplete = null)
    {
        KillAllTweens();

        var sequence = DOTween.Sequence().SetId(this);
        sequence.Append(transform.DOMove(targetPosition, duration).SetEase(Ease.OutQuad));
        sequence.Append(transform.DOPunchScale(Vector3.one * LandingPunchStrength, LandingPunchDuration, 6, 1f));
        sequence.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>Pops the tile up in place (like a UI popup), then flies it to targetPosition; invokes onComplete when done.</summary>
    public void PlayClickPopupThenFly(Vector3 targetPosition, float popupHeight, float popupDuration, float flyDuration, Action onComplete = null)
    {
        KillAllTweens();

        var popupPosition = transform.position + Vector3.up * popupHeight;

        var sequence = DOTween.Sequence().SetId(this);
        sequence.Append(transform.DOMove(popupPosition, popupDuration).SetEase(Ease.OutQuad));
        sequence.Join(transform.DOPunchScale(Vector3.one * 0.2f, popupDuration, 1, 0f));
        sequence.Append(transform.DOMove(targetPosition, flyDuration).SetEase(Ease.InQuad));
        sequence.Append(transform.DOPunchScale(Vector3.one * LandingPunchStrength, LandingPunchDuration, 6, 1f));
        sequence.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>Pops in from scale 0 to 1 with a juicy overshoot.</summary>
    public void PlaySpawnAnimation(float duration)
    {
        KillAllTweens();
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack).SetId(this);
    }

    /// <summary>Light punch-scale (juicy emphasis), then shrinks down to scale 0.</summary>
    public void PlayNearEndEffect(float punchStrength, float punchDuration, float shrinkDuration)
    {
        KillAllTweens();

        var sequence = DOTween.Sequence().SetId(this);
        sequence.Append(transform.DOPunchScale(Vector3.one * punchStrength, punchDuration, 6, 1f));
        sequence.Append(transform.DOScale(Vector3.zero, shrinkDuration));
    }

    /// <summary>Small punch/shake, then flies to targetPosition; invokes onComplete when done.</summary>
    public void PlayCollectSequence(Vector3 targetPosition, float punchStrength, float punchDuration, float flyDuration, Action onComplete)
    {
        KillAllTweens();

        var sequence = DOTween.Sequence().SetId(this);
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
