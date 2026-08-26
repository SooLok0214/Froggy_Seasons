using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    public AudioSource homeBGM;
    public AudioSource inGameBGM;
    public AudioSource gameOverBGM;

    public AudioMixer mixer;
    public Slider bgmSlider;
    public Slider sfxSlider;

    public AudioSource sfxSource;
    public AudioClip buttonClickSfx;
    public AudioClip frogCroakSfx;
    public AudioMixerGroup sfxMixerGroup;

    public bool bgmMuted;
    public bool sfxMuted;

    [System.NonSerialized] public HashSet<Transform> clickTargets = new HashSet<Transform>();
    [System.NonSerialized] public HashSet<Transform> muteTargets = new HashSet<Transform>();
    [System.NonSerialized] public HashSet<Slider> sliderTargets = new HashSet<Slider>();

    public void Awake()
    {
        if (instance != null && instance != this)
        {
            instance.CopySceneReferences(this);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSfxSource();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void Start()
    {
        float bgmValue = PlayerPrefs.GetFloat("bgmSliderVal", 1f);
        float sfxValue = PlayerPrefs.GetFloat("sfxSliderVal", 1f);

        bgmMuted = PlayerPrefs.GetInt("bgmMuted", 0) == 1;
        sfxMuted = PlayerPrefs.GetInt("sfxMuted", 0) == 1;

        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(bgmValue);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(sfxValue);

        ApplyBgmVolume();
        ApplySfxVolume();
        BindSliders();
        RegisterSceneTargets();
    }

    public void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindSliders();
        RegisterSceneTargets();
    }

    public void CopySceneReferences(MusicManager sceneManager)
    {
        // 每個場景只需要更換 MusicManager 子物件上的 AudioClip。
        // 保留下來的 MusicManager 會同步相對應的三首 BGM。
        CopyBgmClip(sceneManager.homeBGM, homeBGM);
        CopyBgmClip(sceneManager.inGameBGM, inGameBGM);
        CopyBgmClip(sceneManager.gameOverBGM, gameOverBGM);

        if (sceneManager.bgmSlider != null)
            bgmSlider = sceneManager.bgmSlider;

        if (sceneManager.sfxSlider != null)
            sfxSlider = sceneManager.sfxSlider;
    }

    public void CopyBgmClip(AudioSource source, AudioSource target)
    {
        if (source == null || target == null || source.clip == null)
            return;

        bool wasPlaying = target.isPlaying;
        target.clip = source.clip;
        target.loop = source.loop;
        target.playOnAwake = false;

        if (wasPlaying)
            target.Play();
    }

    public void EnsureSfxSource()
    {
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    public void BindSliders()
    {
        if (bgmSlider != null && sliderTargets.Add(bgmSlider))
            bgmSlider.onValueChanged.AddListener(SetBgmVol);

        if (sfxSlider != null && sliderTargets.Add(sfxSlider))
            sfxSlider.onValueChanged.AddListener(SetSfxVol);
    }

    public void RegisterSceneTargets()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);

        foreach (Button button in buttons)
        {
            if (button.name != "homeLogoButton")
                AddPointerEvent(button.transform, EventTriggerType.PointerDown, data => PlayButtonClick(), clickTargets);
        }

        Transform[] sceneObjects = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);

        foreach (Transform sceneObject in sceneObjects)
        {
            if (sceneObject.name == "homeLogoButton")
                AddPointerEvent(sceneObject, EventTriggerType.PointerDown, data => PlayFrogCroak(), clickTargets);

            if (sceneObject.name == "musicBtn")
            {
                AddPointerEvent(sceneObject, EventTriggerType.PointerDown, data => PlayButtonClick(), clickTargets);
                AddPointerEvent(sceneObject, EventTriggerType.PointerClick, data => ToggleBgmMute(), muteTargets);
            }

            if (sceneObject.name == "SFXbtn")
            {
                AddPointerEvent(sceneObject, EventTriggerType.PointerDown, data => PlayButtonClick(), clickTargets);
                AddPointerEvent(sceneObject, EventTriggerType.PointerClick, data => ToggleSfxMute(), muteTargets);
            }
        }
    }

    public void AddPointerEvent(Transform target, EventTriggerType eventType, UnityAction<BaseEventData> action, HashSet<Transform> targets)
    {
        if (!targets.Add(target))
            return;

        Graphic graphic = target.GetComponent<Graphic>();

        if (graphic != null)
            graphic.raycastTarget = true;

        EventTrigger trigger = target.GetComponent<EventTrigger>();

        if (trigger == null)
            trigger = target.gameObject.AddComponent<EventTrigger>();

        if (trigger.triggers == null)
            trigger.triggers = new List<EventTrigger.Entry>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    public void PlayHomeMusic()
    {
        PlayMusic(homeBGM);
    }

    public void PlayInGameMusic()
    {
        PlayMusic(inGameBGM);
    }

    public void PlayGameOverMusic()
    {
        PlayMusic(gameOverBGM);
    }

    public void PlayMusic(AudioSource music)
    {
        StopAllMusic();

        if (music != null)
            music.Play();
    }

    public void StopAllMusic()
    {
        if (homeBGM != null)
            homeBGM.Stop();

        if (inGameBGM != null)
            inGameBGM.Stop();

        if (gameOverBGM != null)
            gameOverBGM.Stop();
    }

    public void PlayButtonClick()
    {
        PlaySfx(buttonClickSfx);
    }

    public void PlayFrogCroak()
    {
        PlaySfx(frogCroakSfx);
    }

    public void PlaySfx(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public void SetBgmVol(float val)
    {
        val = Mathf.Clamp(val, 0.0001f, 1f);
        float vol = ToDecibel(val);

        PlayerPrefs.SetFloat("bgmSliderVal", val);
        PlayerPrefs.SetFloat("bgmVol", vol);

        if (!bgmMuted && mixer != null)
            mixer.SetFloat("bgm", vol);
    }

    public void SetSfxVol(float val)
    {
        val = Mathf.Clamp(val, 0.0001f, 1f);
        float vol = ToDecibel(val);

        PlayerPrefs.SetFloat("sfxSliderVal", val);
        PlayerPrefs.SetFloat("sfxVol", vol);

        if (!sfxMuted && mixer != null)
            mixer.SetFloat("sfx", vol);
    }

    public void ToggleBgmMute()
    {
        bgmMuted = !bgmMuted;
        PlayerPrefs.SetInt("bgmMuted", bgmMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyBgmVolume();
    }

    public void ToggleSfxMute()
    {
        sfxMuted = !sfxMuted;
        PlayerPrefs.SetInt("sfxMuted", sfxMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplySfxVolume();

        if (!sfxMuted)
        {
            sfxSource.Stop();
            PlayButtonClick();
        }
    }

    public void ApplyBgmVolume()
    {
        if (mixer == null)
            return;

        float bgmValue = PlayerPrefs.GetFloat("bgmSliderVal", 1f);
        float bgmVolume = PlayerPrefs.GetFloat("bgmVol", ToDecibel(bgmValue));
        mixer.SetFloat("bgm", bgmMuted ? -80f : bgmVolume);
    }

    public void ApplySfxVolume()
    {
        if (mixer == null)
            return;

        float sfxValue = PlayerPrefs.GetFloat("sfxSliderVal", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("sfxVol", ToDecibel(sfxValue));
        mixer.SetFloat("sfx", sfxMuted ? -80f : sfxVolume);
    }

    public float ToDecibel(float val)
    {
        return Mathf.Log10(Mathf.Clamp(val, 0.0001f, 1f)) * 20f;
    }
}
