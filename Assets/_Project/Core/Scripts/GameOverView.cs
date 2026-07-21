using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverView : MonoBehaviour
{
    [SerializeField] private Button _restartButton;
    [SerializeField] private GoalItemView _goalItemPrefab;
    [SerializeField] private Transform _goalsContainer;

    private readonly List<GoalItemView> _spawnedGoals = new();

    public event Action RestartClicked;

    void Start()
    {
        if (_restartButton != null)
        {
            _restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
        }
    }

    public void Show(IReadOnlyList<LevelGoalsView.GoalStatus> goalStatuses)
    {
        ClearGoals();

        if (_goalItemPrefab != null && _goalsContainer != null && goalStatuses != null)
        {
            foreach (var status in goalStatuses)
            {
                var view = Instantiate(_goalItemPrefab, _goalsContainer);
                view.Setup(status.Type, status.Goal, status.Current);
                _spawnedGoals.Add(view);
            }
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void ClearGoals()
    {
        foreach (var view in _spawnedGoals)
        {
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }

        _spawnedGoals.Clear();
    }
}