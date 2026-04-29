using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AudioManager>();
                if (instance == null)
                {
                    instance = new GameObject("Spawned AudioManager", typeof(AudioManager)).GetComponent<AudioManager>();
                }
            }

            return instance;
        }

        private set
        {
            instance = value;
        }
    }

    // create two music source so we can crossfade music to another smoothly
    private AudioSource musicSource;
    private AudioSource musicSource2;
    private AudioSource sfxSource;

    [SerializeField] private float musicVolume = 0.5f;
    [SerializeField] private float masterVolumeSE = 0.5f;

    private bool firstMusicSourceIsPlaying;

    Coroutine musicPlayer;

    // loaded resources
    [SerializeField]
    private AudioClip[] seClips;
    [SerializeField]
    private AudioClip[] bgmClips;

    private void Awake()
    {
        // make sure we don't destroy this instance
        DontDestroyOnLoad(this.gameObject);

        // create audio source, and save them as references
        musicSource = this.gameObject.AddComponent<AudioSource>();
        musicSource2 = this.gameObject.AddComponent<AudioSource>();
        sfxSource = this.gameObject.AddComponent<AudioSource>();

        // Loop the music tracks
        musicSource.loop = true;
        musicSource2.loop = true;

        // default music volume
        musicVolume = 0.5f;
        masterVolumeSE = 0.5f;

        // Load all audio Resources
        seClips = Resources.LoadAll<AudioClip>("Audio/SE");
        bgmClips = Resources.LoadAll<AudioClip>("Audio/BGM");

        // Load all voice acting
        AudioClip[] voiceActing = Resources.LoadAll<AudioClip>("Audio/BATTLE_SE");
        seClips = seClips.Union(voiceActing).ToArray();
    }

    public void SetSEMasterVolume(float value)
    {
        masterVolumeSE = value;
    }
    public float GetSEMasterVolume()
    {
        return masterVolumeSE;
    }

    public void PlayMusic(string bgmname)
    {
        // Determine which music source is active
        AudioSource activeSource = (firstMusicSourceIsPlaying) ? musicSource : musicSource2;

        AudioClip clipToPlay = NameToBGMClip(bgmname);

        if (clipToPlay != null)
        {
            activeSource.clip = clipToPlay;
            activeSource.volume = musicVolume;
            activeSource.Play();
            return;
        }
    }

    public void StopMusicWithFade(float transitiontime = 1f)
    {
        // Determine which music source is active
        AudioSource activeSource = (firstMusicSourceIsPlaying) ? musicSource : musicSource2;

        StartCoroutine(UpdateMusicVolume(activeSource, transitiontime, 0.0f));
        return;
    }

    public void PlayMusicWithFade(string bgmname, float transitionTime)
    {
        // Determine which music source is active
        AudioSource activeSource = (firstMusicSourceIsPlaying) ? musicSource : musicSource2;

        AudioClip clipToPlay = NameToBGMClip(bgmname);

        if (clipToPlay != null)
        {
            musicPlayer = StartCoroutine(UpdateMusicWithFade(activeSource, clipToPlay, transitionTime));
        }
    }
    public void DestroyCurrentMusicSource()
    {
        // Determine which music source is active
        AudioSource activeSource = (firstMusicSourceIsPlaying) ? musicSource : musicSource2;

        activeSource.Stop();
        activeSource.clip = null;
    }

    public void PlayMusicWithCrossFade(string bgmname, float transitionTime = 1.0f)
    {
        // Determine which music source is active
        AudioSource activeSource = (firstMusicSourceIsPlaying) ? musicSource : musicSource2;
        AudioSource newSource = (firstMusicSourceIsPlaying) ? musicSource2 : musicSource;

        AudioClip clipToPlay = NameToBGMClip(bgmname);

        if (clipToPlay != null)
        {
            // Swap the source
            firstMusicSourceIsPlaying = !firstMusicSourceIsPlaying;

            // Set the fields of the audio source, then start the coroutine to crossfade
            newSource.clip = clipToPlay;
            newSource.Play();
            StartCoroutine(UpdateMusicWithCrossFade(activeSource, newSource, transitionTime));
        }
    }

    private IEnumerator UpdateMusicWithFade(AudioSource activeSource, AudioClip newClip, float transitionTime)
    {
        // make sure the source is active and playing
        if (!activeSource.isPlaying)
            activeSource.Play();
        
        activeSource.clip = newClip;
        activeSource.Play();

        // Fadein
        float t = 0.0f;
        for (t = 0; t < transitionTime; t += Time.deltaTime)
        {
            activeSource.volume = ((t / transitionTime) * musicVolume);
            yield return null;
        }
    }

    private IEnumerator UpdateMusicWithCrossFade(AudioSource originalSource, AudioSource newSource, float transitionTime)
    {
        float t = 0.0f;

        for (t = 0; t < transitionTime; t += Time.deltaTime)
        {
            originalSource.volume = (musicVolume - ((t / transitionTime) * musicVolume));
            newSource.volume = ((t / transitionTime) * musicVolume);
            yield return null;
        }

        originalSource.Stop();
    }

    private IEnumerator UpdateMusicVolume(AudioSource source, float transitionTime, float targetVolume)
    {
        float t = 0.0f;
        float originalVolume = source.volume ;
        for (t = 0; t < transitionTime; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(originalVolume, targetVolume, t / transitionTime);
            yield return null;
        }

        if (musicPlayer != null)
        {
            StopCoroutine(musicPlayer);
        }
        source.Stop();
    }

    public AudioSource PlaySFX(string clipname)
    {
        if (string.IsNullOrEmpty(clipname)) return sfxSource;

        AudioClip clipToPlay = NameToSEClip(clipname);

        if (clipToPlay != null)
        {
            sfxSource.PlayOneShot(clipToPlay, masterVolumeSE);
        }

        return sfxSource;
    }

    public AudioSource PlayClip(AudioClip clip)
    {
        AudioClip clipToPlay = clip;

        if (clipToPlay != null)
        {
            sfxSource.PlayOneShot(clipToPlay, masterVolumeSE);
        }

        return sfxSource;
    }

    public AudioSource GetSFXSource()
    {
        return sfxSource;
    }

    public void PlaySFXDelay(string clipname, float delay)
    {
        StartCoroutine(DelaySFX(clipname, delay));
    }

    private IEnumerator DelaySFX(string clip, float time)
    {
        yield return new WaitForSeconds(time);

        PlaySFX(clip);
    }

    public void PlaySFX(string sename, float volume)
    {
        AudioClip clipToPlay = NameToSEClip(sename);

        if (clipToPlay != null)
        {
            sfxSource.PlayOneShot(clipToPlay, volume * masterVolumeSE);
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        AudioSource activeSource = (firstMusicSourceIsPlaying) ? musicSource : musicSource2;
        activeSource.volume = musicVolume;
    }
    public float GetMusicVolume()
    {
        return musicVolume;
    }

    public void PauseMusic()
    {
        AudioSource activeSource = (firstMusicSourceIsPlaying) ? musicSource : musicSource2;
        activeSource.Pause();
    }

    public void UnpauseMusic()
    {
        AudioSource activeSource = (firstMusicSourceIsPlaying) ? musicSource : musicSource2;
        activeSource.Play();
    }

    private AudioClip NameToSEClip(string name)
    {
        AudioClip foundClip = null;
        for (int i = 0; i < seClips.Length; ++i)
        {
            if (seClips[i].name == name)
            {
                foundClip = seClips[i];
            }
        }

        if (foundClip == null)
        {
            Debug.LogWarning("Sound Effect [" + name + "] not found.");
        }

        return foundClip;
    }

    private AudioClip NameToBGMClip(string name)
    {
        AudioClip foundClip = null;
        for (int i = 0; i < bgmClips.Length; ++i)
        {
            if (bgmClips[i].name == name)
            {
                foundClip = bgmClips[i];
            }
        }

        if (foundClip == null)
        {
            Debug.LogWarning("Background Music [" + name + "] not found.");
        }

        return foundClip;
    }
}