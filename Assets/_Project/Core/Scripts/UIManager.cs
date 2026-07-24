using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    public static UIManager Instance => _instance;

    [SerializeField] private TextMeshProUGUI _fpsText;
    [SerializeField] private float _fpsUpdateInterval = 0.5f;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _timerText;

    [Header("Tension (low time)")]
    [SerializeField] private float _urgentThresholdSeconds = 10f;
    [SerializeField] private Color _timerNormalColor = Color.white;
    [SerializeField] private Color _timerUrgentColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private Image _dangerOverlay;
    [SerializeField] private float _dangerOverlayMaxAlpha = 0.35f;
    [SerializeField] private AudioSource _tickAudioSource;
    [SerializeField] private AudioClip _tickClip;
    [SerializeField] private float _tickPitchMin = 1f;
    [SerializeField] private float _tickPitchMax = 1.6f;

    [Header("Tension (tray fill)")]
    [SerializeField] private int _trayTensionStartCount = 2;

    private float _fpsTimer;
    private int _frameCount;
    private int _lastTickSecond = -1;
    private float _trayTension;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _instance = this;
        ResetTension();
    }

    /// <summary>Shows the level name; stays visible (does not auto-hide).</summary>
    public void ShowLevelName(string levelName)
    {
        if (_levelText == null) return;

        _levelText.text = levelName;
        _levelText.gameObject.SetActive(true);
    }

    /// <summary>Displays the remaining time as mm:ss and drives the low-time tension feedback (color, pulsing overlay, ticking sound).</summary>
    public void UpdateTimer(float secondsRemaining)
    {
        if (_timerText != null)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }

        UpdateTension(secondsRemaining);
    }

    /// <summary>Updates how "full" the tray is (drives one half of the tension). Ramps from
    /// _trayTensionStartCount tiles up to a full tray; below the start count there is no tray tension.</summary>
    public void SetTrayTension(int tileCount, int traySize)
    {
        if (tileCount < _trayTensionStartCount || traySize <= _trayTensionStartCount)
        {
            _trayTension = 0f;
            return;
        }

        // Onset at the start count, reaching 1 when the tray is full.
        _trayTension = Mathf.Clamp01((float)(tileCount - _trayTensionStartCount + 1) / (traySize - _trayTensionStartCount + 1));
    }

    /// <summary>Resets all tension feedback to its idle state (call on game over / win / returning to menu).</summary>
    public void ResetTension()
    {
        _lastTickSecond = -1;
        _trayTension = 0f;

        if (_timerText != null) _timerText.color = _timerNormalColor;
        SetDangerOverlayAlpha(0f);
    }

    private void UpdateTension(float secondsRemaining)
    {
        float timeTension = 0f;
        if (secondsRemaining > 0f && secondsRemaining <= _urgentThresholdSeconds)
        {
            timeTension = 1f - Mathf.Clamp01(secondsRemaining / _urgentThresholdSeconds);
        }

        // Overall suspense is the stronger of the two pressures: dwindling time and a filling tray.
        float tension = Mathf.Max(timeTension, _trayTension);

        if (tension <= 0f)
        {
            ResetTension();
            return;
        }

        if (_timerText != null)
        {
            _timerText.color = Color.Lerp(_timerNormalColor, _timerUrgentColor, tension);
        }

        if (_dangerOverlay != null)
        {
            float pulseFrequency = Mathf.Lerp(2f, 6f, tension);
            float pulse = Mathf.Sin(Time.unscaledTime * pulseFrequency * Mathf.PI * 2f) * 0.5f + 0.5f;
            SetDangerOverlayAlpha(Mathf.Lerp(0f, _dangerOverlayMaxAlpha, tension) * pulse);
        }

        // The tick + punch are literal clock ticks — only while the countdown itself is in the urgent window.
        if (timeTension > 0f)
        {
            int currentSecond = Mathf.CeilToInt(secondsRemaining);
            if (currentSecond != _lastTickSecond)
            {
                _lastTickSecond = currentSecond;
                PlayTick(tension);
                PunchTimerText();
            }
        }
    }

    private void SetDangerOverlayAlpha(float alpha)
    {
        if (_dangerOverlay == null) return;

        var color = _dangerOverlay.color;
        color.a = alpha;
        _dangerOverlay.color = color;
    }

    private void PlayTick(float tensionRatio)
    {
        if (_tickAudioSource == null || _tickClip == null) return;

        _tickAudioSource.pitch = Mathf.Lerp(_tickPitchMin, _tickPitchMax, tensionRatio);
        _tickAudioSource.PlayOneShot(_tickClip);
    }

    private void PunchTimerText()
    {
        if (_timerText == null) return;

        var t = _timerText.transform;
        t.DOKill();
        t.localScale = Vector3.one;
        t.DOPunchScale(Vector3.one * 0.25f, 0.25f, 4, 0.5f).SetUpdate(true);
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
