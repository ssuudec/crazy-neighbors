using UnityEngine;
using UnityEngine. SceneManagement;

public class MainMenuManager_sc : MonoBehaviour
{
    public static MainMenuManager_sc Instance;

    [Header("Ses Kaynakları")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Efekt Sesleri")]
    public AudioClip buttonClickSound;
    public AudioClip enemyHurtSound;

    // sahneler arası kalıcı ses değerleri
    public static float musicVolume = 1f;
    public static float sfxVolume = 1f;

    void Awake()
{
    // başka bir instance varsa ve bu farklı bir sahneden geldiyse
    if (Instance != null && Instance != this)
    {
        // sahneyi değil de sadece bu yeni objeyi yok etmek için 
        Destroy(gameObject);
        return;
    }

    // bu objeyi instance yapıp ve sahneler arası korumak için 
    Instance = this;
    DontDestroyOnLoad(gameObject);
}

    void Start()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume;

        if (sfxSource != null)
            sfxSource. volume = sfxVolume;
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
            sfxSource. volume = volume;
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