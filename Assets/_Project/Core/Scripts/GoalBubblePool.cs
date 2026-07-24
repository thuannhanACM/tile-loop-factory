using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Recycles GoalBubble instances so completed boxes can hand theirs back instead of destroying it.
/// Lives on a scene-root object (outside any level) so pooled bubbles survive level reloads.
/// </summary>
public class GoalBubblePool : MonoBehaviour
{
    public static GoalBubblePool Instance { get; private set; }

    [SerializeField] private Transform _poolRoot;

    private readonly Queue<GoalBubble> _pool = new();

    private void Awake()
    {
        // Awake (not Start) so boxes that unlock in their own Start() find the pool ready.
        Instance = this;
    }

    /// <summary>Gets a bubble (reused or freshly instantiated), parents it under `parent`, and resets its transform.</summary>
    public GoalBubble Get(GoalBubble prefab, Transform parent)
    {
        var bubble = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(prefab);

        var t = bubble.transform;
        t.SetParent(parent, false);
        t.localPosition = Vector3.zero;
        t.localScale = Vector3.one;
        t.rotation = Quaternion.identity;

        bubble.gameObject.SetActive(true);
        return bubble;
    }

    /// <summary>Disables the bubble and returns it to the pool for reuse.</summary>
    public void Release(GoalBubble bubble)
    {
        if (bubble == null) return;

        bubble.transform.DOKill();
        bubble.gameObject.SetActive(false);
        bubble.transform.SetParent(_poolRoot != null ? _poolRoot : transform, false);

        _pool.Enqueue(bubble);
    }
}
