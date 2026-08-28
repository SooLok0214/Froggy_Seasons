using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    [System.NonSerialized] public AudioSource homeBGM;
    [System.NonSerialized] public AudioSource inGameBGM;
    [System.NonSerialized] public AudioSource gameOverBGM;

    public AudioMixer mixer;
    public Slider bgmSlider;
    public Slider sfxSlider;
    public GameManager sceneGameManager;

    [System.NonSerialized] public AudioSource buttonClickSfx;
    [System.NonSerialized] public AudioSource frogCroakSfx;
    [System.NonSerialized] public AudioSource fireSfx;
    [System.NonSerialized] public AudioSource levelUpSfx;

    public bool bgmMuted;
    public bool sfxMuted;

    public void Awake()
    {
        ConnectAudioChildren();

        if (instance != null && instance != this)
        {
            instance.CopySceneReferences(this);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (sceneGameManager != null)
            sceneGameManager.musicManager = this;
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
    }

    public void OnDestroy()
    {
        if (instance != this)
            return;

        instance = null;
    }

    public void CopySceneReferences(MusicManager sceneManager)
    {
        if (sceneManager.bgmSlider != null)
            bgmSlider = sceneManager.bgmSlider;

        if (sceneManager.sfxSlider != null)
            sfxSlider = sceneManager.sfxSlider;

        if (sceneManager.sceneGameManager != null)
        {
            sceneGameManager = sceneManager.sceneGameManager;
            sceneGameManager.musicManager = this;
        }

        BindSliders();
    }

    public void ConnectAudioChildren()
    {
        homeBGM = FindAudioSource("BGM-Home");
        inGameBGM = FindAudioSource("BGM-InGame");
        gameOverBGM = FindAudioSource("BGM-GameOver");
        buttonClickSfx = FindAudioSource("SFX-Click");
        frogCroakSfx = FindAudioSource("SFX-FrogCroak");
        fireSfx = FindAudioSource("SFX-Fire");
        levelUpSfx = FindAudioSource("SFX-LevelUp");

        AudioMixerGroup bgmGroup = FindMixerGroup("BGM");
        AudioMixerGroup sfxGroup = FindMixerGroup("SFX");

        SetupAudioSource(homeBGM, bgmGroup, true);
        SetupAudioSource(inGameBGM, bgmGroup, true);
        SetupAudioSource(gameOverBGM, bgmGroup, false);
        SetupAudioSource(buttonClickSfx, sfxGroup, false);
        SetupAudioSource(frogCroakSfx, sfxGroup, false);
        SetupAudioSource(fireSfx, sfxGroup, false);
        SetupAudioSource(levelUpSfx, sfxGroup, false);
    }

    public AudioSource FindAudioSource(string childName)
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.GetComponent<AudioSource>();
    }

    public AudioMixerGroup FindMixerGroup(string groupName)
    {
        if (mixer == null)
            return null;

        AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
        return groups.Length == 0 ? null : groups[0];
    }

    public void SetupAudioSource(AudioSource source, AudioMixerGroup group, bool loop)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;

        if (group != null)
            source.outputAudioMixerGroup = group;
    }

    public void BindSliders()
    {
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(SetBgmVol);
            bgmSlider.onValueChanged.AddListener(SetBgmVol);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(SetSfxVol);
            sfxSlider.onValueChanged.AddListener(SetSfxVol);
        }
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

    public void PlayFireSfx()
    {
        PlaySfx(fireSfx);
    }

    public void PlayLevelUpSfx()
    {
        PlaySfx(levelUpSfx);
    }

    public void PlaySfx(AudioClip clip)
    {
        // 保留此方法供舊的 Unity Button OnClick 連接使用。
        if (buttonClickSfx != null && clip != null)
            buttonClickSfx.PlayOneShot(clip);
    }

    public void PlaySfx(AudioSource source)
    {
        if (source != null && source.clip != null)
            source.PlayOneShot(source.clip);
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
            if (buttonClickSfx != null)
                buttonClickSfx.Stop();

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
