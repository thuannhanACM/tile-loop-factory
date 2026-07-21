using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    public static UIManager Instance => _instance;

    [SerializeField] private TextMeshProUGUI _fpsText;
    [SerializeField] private float _fpsUpdateInterval = 0.5f;
    [SerializeField] private TextMeshProUGUI _levelText;

    private float _fpsTimer;
    private int _frameCount;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _instance = this;
    }

    /// <summary>Shows the level name; stays visible (does not auto-hide).</summary>
    public void ShowLevelName(string levelName)
    {
        if (_levelText == null) return;

        _levelText.text = levelName;
        _levelText.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateFpsCounter();
    }

    private void UpdateFpsCounter()
    {
        if (_fpsText == null) return;

        _frameCount++;
        _fpsTimer += Time.unscaledDeltaTime;

        if (_fpsTimer >= _fpsUpdateInterval)
        {
            float fps = _frameCount / _fpsTimer;
            _fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";

            _frameCount = 0;
            _fpsTimer = 0f;
        }
    }
}
