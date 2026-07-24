using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// A single goal target that can sit on top of up to a few locked boxes beneath it.
/// Each box tracks progress toward collecting a TileType; only an Unlocked box accepts
/// progress. Completing an Unlocked box's goal unlocks every box in <see cref="_boxesBelow"/>
/// (1-3 boxes), so a pile completes top-down, one layer at a time.
/// </summary>
public class TileGoalBox : MonoBehaviour
{
    public enum BoxState { Locked, Unlocked }

    private const string ClosedAnimation = "closed";
    private const string UnlockAnimation = "closedToOpen";
    private const string ArchiveAnimation = "openToArchive";

    [SerializeField] private Animation _animation;
    [SerializeField] private List<TileGoalBox> _boxesBelow = new();
    [SerializeField] private TileType _goalType;
    [SerializeField] private int _goalAmount;
    [SerializeField] private bool _startUnlocked;
    [SerializeField] private GoalBubble _goalBubblePrefab;
    [SerializeField] private float _hitPunchStrength = 0.4f;
    [SerializeField] private float _hitJumpHeight = 0.45f;
    [SerializeField] private float _hitAnimationDuration = 0.35f;
    [SerializeField] private float _bubbleCollapseDuration = 0.3f;

    [Header("Completion vanish")]
    [SerializeField] private float _vanishShakeDuration = 0.5f;
    [SerializeField] private float _vanishShakeStrength = 0.2f;
    [SerializeField] private float _vanishShakeRotation = 25f;
    [SerializeField] private int _vanishShakeVibrato = 20;
    [SerializeField] private GameObject _vanishVfxPrefab;

    private int _currentAmount;
    private GoalBubble _activeBubble;
    private Vector3 _restLocalPosition;
    private Vector3 _baseScale;

    public BoxState State { get; private set; } = BoxState.Locked;
    public TileType GoalType => _goalType;
    public int GoalAmount => _goalAmount;
    public int CurrentAmount => _currentAmount;
    public bool IsComplete => _currentAmount >= _goalAmount;
    public IReadOnlyList<TileGoalBox> BoxesBelow => _boxesBelow;

    /// <summary>Raised once, when this box's goal amount is reached.</summary>
    public event Action<TileGoalBox> Completed;

    /// <summary>Raised whenever CurrentAmount changes, so displays (e.g. GoalBubble) can refresh.</summary>
    public event Action<TileGoalBox> ProgressChanged;

    /// <summary>Sets the goal this box tracks. Call before Unlock().</summary>
    public void Setup(TileType goalType, int goalAmount)
    {
        _goalType = goalType;
        _goalAmount = goalAmount;
        _currentAmount = 0;
    }

    /// <summary>Adds a box that unlocks once this one completes its goal (for piles built at runtime). Supports 1-3 boxes below.</summary>
    public void AddBoxBelow(TileGoalBox boxBelow)
    {
        if (boxBelow != null && !_boxesBelow.Contains(boxBelow))
        {
            _boxesBelow.Add(boxBelow);
        }
    }

    /// <summary>Switches this box to Unlocked, plays the closed-to-open animation, and spawns the goal bubble. No-op if already unlocked or complete.</summary>
    public void Unlock()
    {
        if (State == BoxState.Unlocked) return;

        State = BoxState.Unlocked;

        if (_animation != null)
        {
            _animation.Play(UnlockAnimation);
        }

        SpawnGoalBubble();
    }

    /// <summary>Gets a goal bubble (pooled if available) as a child of this box, facing world-identity rotation regardless of the box's own orientation.</summary>
    private void SpawnGoalBubble()
    {
        if (_goalBubblePrefab == null) return;

        _activeBubble = GoalBubblePool.Instance != null
            ? GoalBubblePool.Instance.Get(_goalBubblePrefab, transform)
            : Instantiate(_goalBubblePrefab, transform);

        _activeBubble.transform.rotation = Quaternion.identity;
        _activeBubble.Bind(this);
    }

    /// <summary>Pronounced punch-scale + clean upward hop, played whenever a tile lands on this box.
    /// Skipped once complete so it never interrupts the vanish sequence.</summary>
    public void PlayHitReaction()
    {
        if (State != BoxState.Unlocked || IsComplete) return;

        transform.DOKill();
        transform.localPosition = _restLocalPosition;
        transform.localScale = _baseScale;

        // Single, high-amplitude pulse (vibrato 1) reads as a clear punch + jump rather than a rattle.
        transform.DOPunchScale(_baseScale * _hitPunchStrength, _hitAnimationDuration, 1, 0.5f);
        transform.DOPunchPosition(Vector3.up * _hitJumpHeight, _hitAnimationDuration, 1, 0.5f);
    }

    /// <summary>Adds progress toward this box's goal. Ignored while Locked, already complete, or if the type doesn't match.</summary>
    public void AddProgress(TileType type, int amount = 1)
    {
        if (State != BoxState.Unlocked || IsComplete) return;
        if (_goalType != null && type != _goalType) return;

        _currentAmount = Mathf.Min(_currentAmount + amount, _goalAmount);
        ProgressChanged?.Invoke(this);

        if (IsComplete)
        {
            PlayCompletionSequence();
            Completed?.Invoke(this);
            foreach (var box in _boxesBelow)
            {
                box?.Unlock();
            }
        }
    }

    /// <summary>
    /// On completion: closes the box (open-to-archive animation), collapses its goal bubble back to
    /// the pool, then shakes for a moment before spawning a VFX at its position and disappearing.
    /// </summary>
    private void PlayCompletionSequence()
    {
        if (_animation != null)
        {
            _animation.Play(ArchiveAnimation);
        }

        if (_activeBubble != null)
        {
            _activeBubble.CollapseAndRelease(_bubbleCollapseDuration);
            _activeBubble = null;
        }

        transform.DOKill();
        transform.localPosition = _restLocalPosition;
        transform.localScale = _baseScale;

        var sequence = DOTween.Sequence().SetTarget(transform);
        sequence.Append(transform.DOShakePosition(_vanishShakeDuration, _vanishShakeStrength, _vanishShakeVibrato, 90f, false, false));
        sequence.Join(transform.DOShakeRotation(_vanishShakeDuration, Vector3.one * _vanishShakeRotation, _vanishShakeVibrato));
        sequence.OnComplete(() =>
        {
            SpawnVanishVfx();
            gameObject.SetActive(false);
        });
    }

    /// <summary>Spawns the placeholder vanish VFX at the box's world position (parented to the level, not the box, so it survives the box being hidden).</summary>
    private void SpawnVanishVfx()
    {
        if (_vanishVfxPrefab == null) return;

        Instantiate(_vanishVfxPrefab, transform.position, Quaternion.identity, transform.parent);
    }

    private void Reset()
    {
        _animation = GetComponent<Animation>();
    }

    private void Awake()
    {
        if (_animation == null)
        {
            _animation = GetComponent<Animation>();
        }

        _restLocalPosition = transform.localPosition;
        _baseScale = transform.localScale;
    }

    private void OnDestroy()
    {
        // Kill any in-flight hit/vanish tweens so level teardown never touches a destroyed transform.
        transform.DOKill();
    }

    private void Start()
    {
        if (_startUnlocked)
        {
            Unlock();
        }
        else if (_animation != null)
        {
            _animation.Play(ClosedAnimation);
        }
    }
}
