using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Floats above a TileGoalBox (its parent) showing the goal's tile icon and how many more
/// tiles are needed to complete it. Refreshes live as the box receives progress. Pooled, so
/// binding to a box is explicit (Bind/Unbind) rather than tied to Awake/OnEnable.
/// </summary>
public class GoalBubble : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _icon;
    [SerializeField] private TextMeshPro _amountText;
    [SerializeField] private float _floatAmplitude = 0.06f;
    [SerializeField] private float _floatCycleDuration = 2f;

    private TileGoalBox _goalBox;

    private void Reset()
    {
        _icon = transform.Find("Icon")?.GetComponent<SpriteRenderer>();
        _amountText = transform.Find("GoalText")?.GetComponent<TextMeshPro>();
    }

    /// <summary>Binds this bubble to a box: subscribes to its progress and shows the current remaining count.</summary>
    public void Bind(TileGoalBox box)
    {
        Unbind();

        _goalBox = box;
        if (_goalBox != null)
        {
            _goalBox.ProgressChanged += Refresh;
            Refresh(_goalBox);
        }

        StartFloating();
    }

    /// <summary>Gently bobs the bubble up and down forever, one full cycle every _floatCycleDuration seconds.</summary>
    private void StartFloating()
    {
        transform.DOKill();

        var basePos = transform.localPosition;
        float half = _floatAmplitude * 0.5f;

        // Centre the bob on the resting position: start slightly low, ease up and back down.
        transform.localPosition = basePos + Vector3.down * half;
        transform.DOLocalMoveY(basePos.y + half, _floatCycleDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetId(this);
    }

    /// <summary>Stops listening to the currently bound box (safe to call when not bound).</summary>
    public void Unbind()
    {
        if (_goalBox != null)
        {
            _goalBox.ProgressChanged -= Refresh;
            _goalBox = null;
        }
    }

    private void OnDestroy()
    {
        // Kill any in-flight collapse tween so teardown never touches a destroyed transform.
        transform.DOKill();
        DOTween.Kill(this);
    }

    /// <summary>Punch-scales down to zero, then unbinds and returns to the pool (or destroys if no pool exists).</summary>
    public void CollapseAndRelease(float duration)
    {
        transform.DOKill();

        var sequence = DOTween.Sequence().SetId(this);
        sequence.Append(transform.DOPunchScale(Vector3.one * 0.2f, duration * 0.35f, 6, 1f));
        sequence.Append(transform.DOScale(Vector3.zero, duration * 0.65f).SetEase(Ease.InBack));
        sequence.OnComplete(() =>
        {
            Unbind();

            if (GoalBubblePool.Instance != null)
            {
                GoalBubblePool.Instance.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        });
    }

    private void Refresh(TileGoalBox box)
    {
        if (_icon != null && box.GoalType != null)
        {
            _icon.sprite = box.GoalType.Icon;
        }

        if (_amountText != null)
        {
            _amountText.text = Mathf.Max(0, box.GoalAmount - box.CurrentAmount).ToString();
        }
    }
}
