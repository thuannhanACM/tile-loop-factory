using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelGoalsView : MonoBehaviour
{
    public readonly struct GoalStatus
    {
        public readonly TileType Type;
        public readonly int Current;
        public readonly int Goal;

        public GoalStatus(TileType type, int current, int goal)
        {
            Type = type;
            Current = current;
            Goal = goal;
        }
    }

    [SerializeField] private GoalItemView _goalItemPrefab;
    [SerializeField] private Transform _container;
    [SerializeField] private HorizontalLayoutGroup _layoutGroup;

    private readonly List<GoalItemView> _views = new();

    public void Setup(IReadOnlyList<LevelConfig.TileTypeCount> goals)
    {
        Clear();

        if (_goalItemPrefab == null)
        {
            GameDebug.LogWarning("_goalItemPrefab is not assigned on LevelGoalsView — nothing will spawn.", LogTopic.UI);
            return;
        }

        if (_container == null)
        {
            GameDebug.LogWarning("_container is not assigned on LevelGoalsView — nothing will spawn.", LogTopic.UI);
            return;
        }

        if (goals == null || goals.Count == 0)
        {
            GameDebug.LogWarning("LevelConfig.CollectGoals is empty — LevelGoalsView has nothing to render.", LogTopic.UI);
            return;
        }
        if(_layoutGroup != null)
            _layoutGroup.enabled = true;
        
        foreach (var goal in goals)
        {
            var view = Instantiate(_goalItemPrefab, _container);
            view.Setup(goal.Type, goal.Count);
            _views.Add(view);
        }

        StartCoroutine(DisableLayoutAfterArrangeNextFrame());
    }

    /// <summary>Waits 1 frame (so freshly instantiated children have valid layout sizes), forces the
    /// HorizontalLayoutGroup to arrange them once, then disables it so their RectTransforms stay
    /// static as fixed fly-to targets afterward.</summary>
    private IEnumerator DisableLayoutAfterArrangeNextFrame()
    {
        if (_layoutGroup == null) yield break;

        yield return null;

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_container);
        _layoutGroup.enabled = false;
    }

    public void ReportCollected(TileType type, int amount = 1)
    {
        var view = GetView(type);
        if (view != null)
        {
            view.SetCurrent(view.Current + amount);
        }
    }

    public GoalItemView GetView(TileType type)
    {
        foreach (var view in _views)
        {
            if (view.Type == type) return view;
        }

        return null;
    }

    public List<GoalStatus> GetStatuses()
    {
        var statuses = new List<GoalStatus>(_views.Count);
        foreach (var view in _views)
        {
            statuses.Add(new GoalStatus(view.Type, view.Current, view.Goal));
        }

        return statuses;
    }

    private void Clear()
    {
        foreach (var view in _views)
        {
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }

        _views.Clear();
    }
}