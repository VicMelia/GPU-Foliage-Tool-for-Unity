using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    //Barra vida 
    public Slider healthBar;
    public Image healthFill; 
    public Color healthColor = Color.red;
    public Color timerColor = Color.yellow;
    public float lerpSpeed = 5f;

    //Barra timer explosion
    public bool useTimer = false;
    public float timerValue = 0f;
    public float timerMax = 15f;
    float targetHealth;
    float currentHealth;

    //XP
    public Slider xpBar;
    public TextMeshProUGUI levelText;
    int _currentLevel = 1;
    float _currentXP = 0f;
    float _requiredXP = 100f;

    //Heat
    public Slider heatBar;
    public Image heatFill;

    //Menu Pausa
    public CanvasGroup pauseMenu;
    public GameObject mainMenu;
    //Texto dia
    public CanvasGroup dayCanvas;
    public TextMeshProUGUI dayText;
    public GameObject settingsMenu;
    public GameObject controlsMenu;

    //Panel fade
    public CanvasGroup panelFade;

    //Sound
    [SerializeField] AudioClip _dayClip;
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else {

            Instance = this;
            DontDestroyOnLoad(this);
        } 
        
    }

    private void Start()
    {
        ResetHealthUI();
        ResetHealthUI();
    }

    private void Update()
    {
        if (!useTimer)
        {
            currentHealth = Mathf.Lerp(currentHealth, targetHealth, Time.deltaTime * lerpSpeed);
            healthBar.value = currentHealth;
        }
        else
        {
            timerValue -= Time.deltaTime;
            timerValue = Mathf.Clamp(timerValue, 0f, timerMax);
            healthBar.value = timerValue;

            if (timerValue <= 0f)
            {
                EndGame();
            }
        }
    }

    public void UpdateHealthBar(float newHealth)
    {
        if (useTimer) return;
        targetHealth = Mathf.Clamp(newHealth, 0f, PlayerStats.maxHealth);
    }

    public void ActivateMorphTimer(float duration)
    {
        useTimer = true;
        timerMax = duration;
        timerValue = duration;
        healthBar.maxValue = duration;
        healthFill.color = timerColor;
    }

    public void SetLevelProgress(float current, float required, int level)
    {
        _currentXP = current;
        _requiredXP = required;
        _currentLevel = level;

        xpBar.maxValue = _requiredXP;
        xpBar.value = _currentXP;

        if (levelText != null)
            levelText.text = _currentLevel.ToString();
    }

    public void UpdateHeatSlider(float value)
    {
        heatBar.value = Mathf.Clamp(value, 0f, 1.5f);

    }

    public void ResetHealthUI()
    {
        useTimer = false;
        timerValue = 0f;
        healthBar.maxValue = PlayerStats.maxHealth;
        targetHealth = PlayerStats.health;
        currentHealth = targetHealth;
        healthBar.value = currentHealth;
        healthFill.color = healthColor;
    }

    public void ResetHeatUI()
    {
        heatBar.maxValue = 1.5f;
        heatBar.value = 0f;
    }

    public void ShowDayText(int dayNumber)
    {
        dayText.text = "DAY " + dayNumber.ToString();
        dayCanvas.gameObject.SetActive(true);
        StartCoroutine(ShowDayTextRoutine());
    }

    private IEnumerator ShowDayTextRoutine()
    {
        yield return new WaitForSeconds(1f);
        SoundFXManager.Instance.PlaySoundClip(_dayClip, GameObject.Find("Player").transform, 0.4f);
        yield return StartCoroutine(FadeInCanvasCoroutine(dayCanvas, 0.75f));
        yield return new WaitForSecondsRealtime(1.25f); //Visible un rato antes del fadeout
        yield return StartCoroutine(FadeOutCanvasCoroutine(dayCanvas, 0.75f, false));
        yield return new WaitForSeconds(0.75f);
        GameManager.Instance.TransitionTo(GameManager.GameState.Play);
    }

    public void FadeInCanvas(CanvasGroup group, float duration)
    {
        StartCoroutine(FadeInCanvasCoroutine(group, duration));
    }

    public IEnumerator FadeInCanvasCoroutine(CanvasGroup group, float fadeDuration)
    {
        if (group == null) yield break;

        group.alpha = 0f;
        group.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        group.alpha = 1f;
    }

    public void FadeOutCanvas(CanvasGroup group, float duration, bool active)
    {
        StartCoroutine(FadeOutCanvasCoroutine(group, duration, active));
    }

    public IEnumerator FadeOutCanvasCoroutine(CanvasGroup group, float fadeDuration, bool active)
    {
        if (group == null) yield break;

        group.alpha = 1f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }

        group.alpha = 0f;
        Time.timeScale = 1f;
        group.gameObject.SetActive(active);
    }

    public void ResumeGame()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        FadeOutCanvas(pauseMenu, 0.75f, false);
        MusicManager.Instance.UnPauseAll();
        LightingManager.Instance.enabled = true;
        GameManager.Instance.paused = false;
        PlayerManager playerManager = GameObject.Find("Player").GetComponent<PlayerManager>();
        playerManager.ToggleCombat(true);
        playerManager.ToggleMovement(true);
        playerManager.ToggleEngine(true);


    }

    public void Settings()
    {
        mainMenu.gameObject.SetActive(false);
        settingsMenu.SetActive(true);


    }

    public void Controls()
    {
        mainMenu.gameObject.SetActive(false);
        controlsMenu.SetActive(true);


    }

    public void LeaveGame()
    {
        Application.Quit();


    }

    private void EndGame()
    {
        Debug.Log("Fin de la partida. Tiempo agotado.");
        // Aquí puedes mostrar pantalla de derrota, reiniciar nivel, etc.
    }
}

    

