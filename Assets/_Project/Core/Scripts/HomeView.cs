using System;
using UnityEngine;
using UnityEngine.UI;

public class HomeView : MonoBehaviour
{
    [SerializeField] private Button _startGameButton;

    public event Action StartGameClicked;

    void Start()
    {
        if (_startGameButton != null)
        {
            _startGameButton.onClick.AddListener(() => StartGameClicked?.Invoke());
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}