using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 

public class MainMenuManager_sc : MonoBehaviour
{
    public static MainMenuManager_sc Instance;

    public static bool usePreTrainedAI = false;

    [Header("Ses Kaynakları")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Efekt Sesleri")]
    public AudioClip buttonClickSound;
    public AudioClip enemyHurtSound;

    
    [Header("Yapay Zeka")]
    public Toggle aiToggle;

    public static float musicVolume = 1f;
    public static float sfxVolume = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume;

        if (sfxSource != null)
            sfxSource.volume = sfxVolume;

       
        if (aiToggle != null)
        {
            usePreTrainedAI = aiToggle.isOn;
            aiToggle.onValueChanged.AddListener(ToggleAI);
        }
    }

    public void ToggleAI(bool isOn)
    {
        usePreTrainedAI = isOn;
        Debug.Log("Yapay Zeka Yükleme Modu: " + (isOn ? "AÇIK" : "KAPALI"));
    }

    public void StartGame()
    {
        PlayButtonClick();
        SceneManager.LoadScene(1);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        if (musicSource != null)
            musicSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        if (sfxSource != null)
            sfxSource.volume = volume;
    }

    public void PlayButtonClick()
    {
        if (sfxSource != null && buttonClickSound != null)
            sfxSource.PlayOneShot(buttonClickSound);
    }

    public void PlayEnemyHurt()
    {
        if (sfxSource != null && enemyHurtSound != null)
            sfxSource.PlayOneShot(enemyHurtSound);
    }
}