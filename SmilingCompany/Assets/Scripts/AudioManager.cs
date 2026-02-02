using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource monsterAlertSource; // Looping alert sound

    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioClip dropSuccessSound; // Item dropped in correct zone
    [SerializeField] private AudioClip monsterAlertSound; // Looping tension sound
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;

    void Awake()
    {
        // Singleton pattern - persist between scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudio()
    {
        // Setup music source
        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            if (backgroundMusic != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.Play();
            }
        }

        // Setup monster alert source
        if (monsterAlertSource != null)
        {
            monsterAlertSource.loop = true;
            monsterAlertSource.volume = 0f;
            if (monsterAlertSound != null)
            {
                monsterAlertSource.clip = monsterAlertSound;
                monsterAlertSource.Play(); // Always playing, just at 0 volume
            }
        }
    }

    // Call this when player picks up an object
    public void PlayPickupSound()
    {
        if (sfxSource != null && pickupSound != null)
        {
            sfxSource.PlayOneShot(pickupSound, sfxVolume);
        }
    }

    // Call this when item is dropped in correct zone
    public void PlayDropSuccessSound()
    {
        if (sfxSource != null && dropSuccessSound != null)
        {
            sfxSource.PlayOneShot(dropSuccessSound, sfxVolume);
        }
    }

    // Call this from MonsterZoneEmotionGate to set alert intensity (0-1)
    public void SetMonsterAlertIntensity(float intensity)
    {
        if (monsterAlertSource != null)
        {
            monsterAlertSource.volume = Mathf.Clamp01(intensity) * sfxVolume;
        }
    }

    // Stop the alert sound (when player exits zone or is safe)
    public void StopMonsterAlert()
    {
        if (monsterAlertSource != null)
        {
            monsterAlertSource.volume = 0f;
        }
    }

    // Optional: control music
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public void PauseMusic() => musicSource?.Pause();
    public void ResumeMusic() => musicSource?.UnPause();
}
