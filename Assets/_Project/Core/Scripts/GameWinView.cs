using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameWinView : MonoBehaviour
{
    [SerializeField] private Button _bonusLevelButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private GoalItemView _goalItemPrefab;
    [SerializeField] private Transform _goalsContainer;

    private readonly List<GoalItemView> _spawnedGoals = new();

    public event Action BonusLevelClicked;
    public event Action BackClicked;

    void Start()
    {
        if (_bonusLevelButton != null)
        {
            _bonusLevelButton.onClick.AddListener(() => BonusLevelClicked?.Invoke());
        }

        if (_backButton != null)
        {
            _backButton.onClick.AddListener(() => BackClicked?.Invoke());
        }
    }

    public void Show(IReadOnlyList<LevelGoalsView.GoalStatus> goalStatuses, bool showBonusLevelButton = true)
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

        if (_bonusLevelButton != null)
        {
            _bonusLevelButton.gameObject.SetActive(showBonusLevelButton);
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