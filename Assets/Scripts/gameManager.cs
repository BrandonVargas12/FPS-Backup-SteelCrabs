using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;

public class gameManager : MonoBehaviour
{
    public static gameManager instance;

    [Header("----- Player -----")]
    public GameObject player;
    public playerController playerScript;
    public GameObject playerSpawnPos;
    public GameObject playerDamage;
    public int sensHor;
    public int sensVert;

    [Header("----- Menus -----")]
    public GameObject menuCurrentlyOpen;
    public GameObject pauseMenu;
    public GameObject playerDeadMenu;
    public GameObject winMenu;
    public GameObject startMenu;
    public GameObject settingsMenu;
    public GameObject creditsMenu;

    public UnityEngine.EventSystems.EventSystem eventSystem;
    public GameObject buttonToSelect;
    public GameObject startButton;
    public GameObject resumeButton;
    public GameObject backButtonSettings;
    public GameObject backButtonCredits;

    [Header("----- UI -----")]
    [Range(3, 10)] [SerializeField] int countDownTimer;
    [SerializeField] public TextMeshPro triggerAssembly;
    public Image HPBar;
    public Text countDownDisplay;
    public TextMeshProUGUI hullOnList;
    public TextMeshProUGUI wingsOnList;
    public TextMeshProUGUI enginesOnList;

    [Header("----- Settings -----")]
    public int firstPlayInt;

    private static readonly string FirstPlay = "FirstPlay";
    private static readonly string MainVolumePref = "MainVolumePref";
    private static readonly string SFXPref = "SFXPref";
    private static readonly string MusicVolumePref = "MusicVolumePref";
    public Slider mainVolumeSlider, SFXSlider, musicVolumeSlider;
    public float mainVolumeFloat, SFXFloat, musicVolimeFloat;

    private static readonly string SensHoriPref = "SensHoriPref";
    private static readonly string SensVertPref = "SensVertPref";
    public Slider sensHoriSlider, sensVertSlider;

    private static readonly string BrightnessPref = "BrightnessPref";
    public Slider brightnessSlider;
    public float brightnessVolumeFloat;
    public PostProcessProfile brightness;
    public PostProcessLayer layer;
    public AutoExposure exposure;

    [Header("----- Audio -----")]
    [SerializeField] public AudioSource MainVolume;
    [SerializeField] public AudioSource MusicVolume;
    [SerializeField] public AudioSource SFXVolume;

    public AudioClip buttonClicked;

    [Header("----- Scope -----")]
    public GameObject scopeMask;
    public GameObject basicReticle;
    public float zoomMult;
    public float defaultFOV;

    [Header("----- Misc -----")]
    public int enemyCount;
    public int waveCount;
    public bool isCounting;
    public bool isPaused;
    public bool isFiringRange;
    public bool canTriggerWin;
    float timeScaleOrig;

    [Header("----- Parts Collected -----")]
    public bool hullCollected;
    public bool engineCollected;
    public bool wingsCollected;

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;

        canTriggerWin = false;

        //Game Music
        MusicVolume.Play();

        //Sets player's object, controller, spawn pos and sensitivity
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerScript = player.GetComponent<playerController>();
        playerSpawnPos = GameObject.Find("Player Spawn Pos");
        sensHor = 600;
        sensVert = 600;

        //Keeps the games original time scale
        timeScaleOrig = Time.timeScale;

        //Sets camera fov
        if (Camera.main != null) defaultFOV = Camera.main.fieldOfView;
        
        //A bool for the countdown timer feature
        isCounting = true;

        //Sets the time scale to one for any time we are going into a different scene
        Time.timeScale = 1;

        //Settings
        firstPlayInt = PlayerPrefs.GetInt(FirstPlay);
        brightness.TryGetSettings(out exposure);

        //If this is the users first time playing it sets the default settings
        if (firstPlayInt == 0)
        {
            mainVolumeFloat = .8f;
            SFXFloat = .5f;
            musicVolimeFloat = .3f;
            brightnessVolumeFloat = 1f;
            exposure.keyValue.value = 1f;
            sensHor = 600;
            sensVert = 600;
            mainVolumeSlider.value = mainVolumeFloat;
            SFXSlider.value = SFXFloat;
            musicVolumeSlider.value = musicVolimeFloat;
            brightnessSlider.value = brightnessVolumeFloat;
            sensHoriSlider.value = sensHor;
            sensVertSlider.value = sensVert;
            PlayerPrefs.SetFloat(MainVolumePref, mainVolumeFloat);
            PlayerPrefs.SetFloat(SFXPref, SFXFloat);
            PlayerPrefs.SetFloat(MusicVolumePref, musicVolimeFloat);
            PlayerPrefs.SetFloat(BrightnessPref, brightnessVolumeFloat);
            PlayerPrefs.SetFloat(SensHoriPref, sensHor);
            PlayerPrefs.SetFloat(SensVertPref, sensVert);
            PlayerPrefs.SetInt(FirstPlay, -1);
        }
        else
        {
            //If not it sets it to the settings they last left them on
            mainVolumeFloat = PlayerPrefs.GetFloat(MainVolumePref);
            SFXFloat = PlayerPrefs.GetFloat(SFXPref);
            musicVolimeFloat = PlayerPrefs.GetFloat(MusicVolumePref);
            brightnessVolumeFloat = PlayerPrefs.GetFloat(BrightnessPref);
            sensHor = (int)PlayerPrefs.GetFloat(SensHoriPref);
            sensVert = (int)PlayerPrefs.GetFloat(SensVertPref);
            mainVolumeSlider.value = mainVolumeFloat;
            SFXSlider.value = mainVolumeFloat;
            musicVolumeSlider.value = musicVolimeFloat;
            brightnessSlider.value = brightnessVolumeFloat;
            sensHoriSlider.value = sensHor;
            sensVertSlider.value = sensVert;
        }

        ContinueSettings();
    }

    // Update is called once per frame
    void Update()
    {
        //Check for escape key to pause game
        if (Input.GetButtonDown("Cancel") && menuCurrentlyOpen == null)
        {
            //Bug fix to getting settings menu stuck
            if(isPaused && menuCurrentlyOpen == settingsMenu)
            {
                settingsMenu.SetActive(false);
                menuCurrentlyOpen = pauseMenu;
                menuCurrentlyOpen.SetActive(true);
            }
            //Pauses game or unpauses
            else
            {
                scopeMask.SetActive(false);
                isPaused = !isPaused;
                menuCurrentlyOpen = pauseMenu;
                menuCurrentlyOpen.SetActive(isPaused);

                if (isPaused)
                {
                    CursorLockPause();

                    //Sets the highlighted button to the needed button
                    buttonToSelect = resumeButton;
                    eventSystem.SetSelectedGameObject(buttonToSelect);
                }
                else CursorUnlockUnpause();
            }
        }

        //Makes sure game isnt paused
        if (!isPaused)
        {
            //Does a zoom effect
            if (Input.GetMouseButton(1))
            {
                ZoomCamera(defaultFOV / zoomMult);
                scopeMask.SetActive(true);
                basicReticle.SetActive(false);
            }
            else if (Camera.main.fieldOfView != defaultFOV)
            {
                ZoomCamera(defaultFOV);
                scopeMask.SetActive(false);
                basicReticle.SetActive(true);
            }
        }

        //Check for win game condition
        if(hullCollected &&wingsCollected && engineCollected)
        {
            canTriggerWin = true;
        }

        //Floating text always faces player
        if(triggerAssembly != null) triggerAssembly.transform.LookAt(Camera.main.transform);
    }

    #region Settings

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(MainVolumePref, mainVolumeSlider.value);
        PlayerPrefs.SetFloat(SFXPref, SFXSlider.value);
        PlayerPrefs.SetFloat(MusicVolumePref, musicVolumeSlider.value);
        PlayerPrefs.SetFloat(BrightnessPref, brightnessSlider.value);
        PlayerPrefs.SetFloat(SensHoriPref, sensHoriSlider.value);
        PlayerPrefs.SetFloat(SensVertPref, sensVertSlider.value);
    }

    void ContinueSettings()
    {
        mainVolumeFloat = PlayerPrefs.GetFloat(MainVolumePref);
        SFXFloat = PlayerPrefs.GetFloat(MainVolumePref);
        musicVolimeFloat = PlayerPrefs.GetFloat(MainVolumePref);
        brightnessVolumeFloat = PlayerPrefs.GetFloat(BrightnessPref);
        sensHor = (int)PlayerPrefs.GetFloat(SensHoriPref);
        sensVert = (int)PlayerPrefs.GetFloat(SensVertPref);
        MainVolume.volume = mainVolumeFloat;
        SFXVolume.volume = SFXFloat;
        MusicVolume.volume = musicVolimeFloat;
        exposure.keyValue.value = brightnessVolumeFloat;
    }

    void OnApplicationFocus(bool inFocus)
    {
        if (!inFocus) SaveSettings();
    }

    #endregion

    #region Pause & Unpause

    public void CursorLockPause()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        Time.timeScale = 0;
    }

    public void CursorUnlockUnpause()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = timeScaleOrig;
        if (menuCurrentlyOpen != null) menuCurrentlyOpen.SetActive(false);
        menuCurrentlyOpen = null;
    }

    #endregion


    public void AdjustPartsList(TextMeshProUGUI tmpToStrike)
    {
        tmpToStrike.fontStyle = FontStyles.Strikethrough;
    }

    IEnumerator checkEnemyTotal()
    {
        if (!isFiringRange && enemyCount <= 0)
        {
            yield return new WaitForSeconds(2);
            /*menuCurrentlyOpen = winMenu;
            menuCurrentlyOpen.SetActive(true);
            CursorLockPause();*/
        }
    }

    public void PlayerIsDead()
    {
        //Makes sure to unzoom, pause the game, and turns on the dead menu
        scopeMask.SetActive(false);
        isPaused = true;
        playerDeadMenu.SetActive(true);
        menuCurrentlyOpen = playerDeadMenu;
        CursorLockPause();
    }

    IEnumerator WinGame()
    {
        //Waits 2 seconds and opens win menu
        yield return new WaitForSeconds(2);
        menuCurrentlyOpen = winMenu;
        menuCurrentlyOpen.SetActive(true);
        CursorLockPause();
    }

    public IEnumerator CountDownStart()
    {
        //Pauses game & turns on text
        Time.timeScale = 0;
        countDownDisplay.gameObject.SetActive(true);

        while (countDownTimer != 0)
        {
            //Sets text to int's value
            countDownDisplay.text = countDownTimer.ToString();

            //Waits a second
            yield return new WaitForSecondsRealtime(1f);

            //Decrement the int
            countDownTimer--;
        }

        //Resumes game & gives back player functionality
        Time.timeScale = 1;

        //Lets player know they can move now
        countDownDisplay.text = "Go!!!";

        //Disables the text getting start off the screen
        yield return new WaitForSeconds(1f);
        countDownDisplay.gameObject.SetActive(false);
        countDownDisplay.text = "";
        isCounting = false;
    }

    void ZoomCamera(float target)
    {
        float angle = Mathf.Abs((defaultFOV/zoomMult)-defaultFOV);
        Camera.main.fieldOfView = Mathf.MoveTowards(Camera.main.fieldOfView, target, angle * Time.deltaTime);
    }
    
}

