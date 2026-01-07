using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject gameOverPanel; 
    public int targetResolutionWidth = 1920;
    public int targetResolutionHeight = 1080;
    public bool fullscreen = true;
    bool _gameOver = false;
    public bool paused;
    PlayerControls _playerControls;
    PlayerManager _playerManager;

    //UI
    public CanvasGroup pauseMenu;
    [SerializeField] AudioClip _openMenuClip;
    [SerializeField] AudioClip _bossAmbient;
    [SerializeField] AudioClip _bossSoundtrack;


    //Intro UI
    public CanvasGroup anyButtonText;
    public CanvasGroup panelIntroFade;
    public AudioClip windClip;

    public enum GameState { Intro, Begin, Play, Boss, End};
    public GameState state = GameState.Intro;
    bool _started;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }

        _playerControls = FindAnyObjectByType<PlayerControls>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    void Start()
    {
        Screen.SetResolution(targetResolutionWidth, targetResolutionHeight, fullscreen);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (SceneManager.GetActiveScene().buildIndex == 2)
        {
            state = GameState.Play;
            return;
        }
        state = GameState.Intro;
        
        StartCoroutine(HandleIntroSequence());
    }

    void Update()
    {
        switch (state)
        {
            case GameState.Begin:
                if (!_started) {
                    _started = true;
                    GameUI.Instance.ShowDayText(1);
                }
                break;

            case GameState.Play:
                break;
        }

        if (state == GameState.Intro) return; //no se puede abrir menu de pausa antes de empezar
        if (!paused && _playerControls.isInMenu)
        {
            paused = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            LightingManager.Instance.enabled = false;
            PlayerManager playerManager = GameObject.Find("Player").GetComponent<PlayerManager>();           
            playerManager.ToggleCombat(false);
            playerManager.ToggleMovement(false);
            playerManager.ToggleEngine(false);
            pauseMenu.gameObject.SetActive(true);
            Time.timeScale = 0f;
            GameUI.Instance.FadeInCanvas(pauseMenu, 0.75f);
            MusicManager.Instance.PauseAll();
            SoundFXManager.Instance.PlaySoundClip(_openMenuClip, playerManager.transform, 0.3f);

        }

        
    }

    public void TransitionTo(GameState newState)
    {
        state = newState;
        switch (newState)
        {
            case GameState.Begin:
                break;

            case GameState.Play: //Comienza musica, y tiempo
                MusicManager.Instance.PlayNextTrack();
                break;

            case GameState.Boss:
                StartCoroutine(ChangeToNextScene());
                break;

            case GameState.End:
                //Cuando ganas o pierdes (comprobar si jugador tiene 0 vida o no)
                break;
        }


    }

    IEnumerator ChangeToNextScene()
    {
        GameUI.Instance.FadeInCanvas(GameUI.Instance.panelFade, 1.5f);
        //Fade de los sonidos
        MusicManager.Instance.StopAllCoroutines();
        MusicManager.Instance.StopMusicSound(1.5f);
        MusicManager.Instance.StopAmbientSound(1.5f);
        yield return new WaitForSeconds(1.9f);
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneIndex);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        GameUI.Instance.gameObject.SetActive(false);
        GameUI.Instance.gameObject.SetActive(true);
        MusicManager.Instance.StopMusicSound(0.5f);
        MusicManager.Instance.PlayAmbientSound(_bossAmbient, 0.5f);

        yield return new WaitForSeconds(0.2f); //Esperar a que cargue
        //Fade out inicial
        GameUI.Instance.FadeOutCanvas(GameUI.Instance.panelFade, 1.5f, true);
    }

    public void RestartPlayer()
    {

        StartCoroutine(RestartAfterFade());
    }

    IEnumerator RestartAfterFade()
    {
        Time.timeScale = 1f;
        yield return new WaitForSeconds(1.5f); //Segundo y medio antes del fade in
        GameUI.Instance.FadeInCanvas(GameUI.Instance.panelFade, 1.5f);
        yield return new WaitForSeconds(1.9f); 

        //Mueve al jugador al ultimo checkpoint
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            Vector3 checkPointPos = PlayerStats.lastCheckpoint.position;
            Vector3 newPos = new Vector3(checkPointPos.x, checkPointPos.y + 1f, checkPointPos.z);
            Debug.Log("NEW POS: " + newPos);
            player.transform.position = newPos;
            Rigidbody rb = player.GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.position = newPos;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            //Baja de nivel y nuevos stats
            PlayerExperience pe = player.GetComponent<PlayerExperience>();
            pe.LevelDown();
            //Stats del jugador y componentes reactivados
            PlayerStats.health = PlayerStats.maxHealth;
            GameUI.Instance.ResetHealthUI();

            PlayerManager manager = player.GetComponent<PlayerManager>();
            if (manager != null)
            {
                manager.ToggleMovement(true);
                manager.ToggleCombat(true);
                manager.ToggleEngine(true);
                manager.ResetCollider();
                manager.ResetPlayerModel();
                manager.ResetPlayerHeat();
                manager.SetCompassSprite(true);
            }
        }

        yield return new WaitForSeconds(0.2f);
        
        GameUI.Instance.FadeOutCanvas(GameUI.Instance.panelFade, 1f, true);
    }

    public void FadeAndLoadScene(int sceneIndex)
    {
        StartCoroutine(FadeAndLoadSceneCoroutine(sceneIndex));
    }

    private IEnumerator FadeAndLoadSceneCoroutine(int sceneIndex) //Derrota contra el boss final
    {
        Time.timeScale = 0f; 
        GameUI.Instance.FadeInCanvas(GameUI.Instance.panelFade, 1.5f);
        MusicManager.Instance.StopMusicSound(1.5f);
        MusicManager.Instance.StopAmbientSound(1.5f);
        yield return new WaitForSecondsRealtime(1.9f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        GameUI.Instance.gameObject.SetActive(false);
        GameUI.Instance.gameObject.SetActive(true);
        GameUI.Instance.FadeOutCanvas(GameUI.Instance.panelFade, 1.5f, true);
    }


    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // Asegúrate de tener esta escena
    }


    //INTRO

    IEnumerator HandleIntroSequence()
    {
        yield return new WaitForSeconds(1.5f);
        MusicManager.Instance.PlayAmbientSound(windClip, 0.3f);
        yield return StartCoroutine(GameUI.Instance.FadeOutCanvasCoroutine(panelIntroFade, 2f, true)); //fade out panel negro
        Coroutine flickerRoutine = StartCoroutine(IntermitentText());

        while (!_playerControls.isAnyKey)
            yield return null;


        LightingManager.Instance.enabled = true;
        StopCoroutine(flickerRoutine); //Se detiene el parpadeo y acaba la intro
        yield return StartCoroutine(GameUI.Instance.FadeOutCanvasCoroutine(anyButtonText, 0.5f, false));
        MusicManager.Instance.StartMusic();
        yield return new WaitForSeconds(2.5f);
        TransitionTo(GameState.Begin);
    }

    IEnumerator IntermitentText()
    {
        while (true)
        {
            yield return StartCoroutine(GameUI.Instance.FadeInCanvasCoroutine(anyButtonText, 1f));
            yield return new WaitForSeconds(1f);
            yield return StartCoroutine(GameUI.Instance.FadeOutCanvasCoroutine(anyButtonText, 1f, true));
            yield return new WaitForSeconds(0.5f);
        }
    }


}
