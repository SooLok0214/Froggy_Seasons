using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    public const float SilentSliderValue = 0.0001f;

    public static MusicManager instance;

    [System.NonSerialized] public AudioSource homeBGM;
    [System.NonSerialized] public AudioSource inGameBGM;
    [System.NonSerialized] public AudioSource gameOverBGM;

    public AudioMixer mixer;
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Sprite volumeBarSprite;
    [Min(1f)] public float volumeBarHeight = 12f;
    [Min(0f)] public float volumeBarHorizontalPadding = 20f;
    public Color volumeBarColor = Color.white;
    public GameManager sceneGameManager;

    [System.NonSerialized] public AudioSource buttonClickSfx;
    [System.NonSerialized] public AudioSource frogCroakSfx;
    [System.NonSerialized] public AudioSource fireSfx;
    [System.NonSerialized] public AudioSource levelUpSfx;
    [System.NonSerialized] public AudioSource monsterHitSfx;
    [System.NonSerialized] public Sprite muteIconSprite;

    public bool bgmMuted;
    public bool sfxMuted;

    public void Awake()
    {
        ConnectAudioChildren();
        ConnectSceneSliders();

        if (instance != null && instance != this)
        {
            instance.CopySceneReferences(this);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (sceneGameManager != null)
            sceneGameManager.musicManager = this;
    }

    public void Start()
    {
        LoadSavedVolumeState();
        SetupVolumeBars();
        SyncSliderValues();
        ApplyBgmVolume();
        ApplySfxVolume();
        BindSliders();
        BindButtonAudio();
    }

    public void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    public void CopySceneReferences(MusicManager sceneManager)
    {
        CopyMonsterHitAudio(sceneManager);

        if (sceneManager.bgmSlider != null)
            bgmSlider = sceneManager.bgmSlider;

        if (sceneManager.sfxSlider != null)
            sfxSlider = sceneManager.sfxSlider;

        if (sceneManager.sceneGameManager != null)
        {
            sceneGameManager = sceneManager.sceneGameManager;
            sceneGameManager.musicManager = this;
        }

        if (sceneManager.volumeBarSprite != null)
            volumeBarSprite = sceneManager.volumeBarSprite;

        volumeBarHeight = sceneManager.volumeBarHeight;
        volumeBarHorizontalPadding = sceneManager.volumeBarHorizontalPadding;
        volumeBarColor = sceneManager.volumeBarColor;

        SetupVolumeBars();
        SyncSliderValues();
        ApplyBgmVolume();
        ApplySfxVolume();
        BindSliders();
    }

    public void CopyMonsterHitAudio(MusicManager sceneManager)
    {
        // The menu manager survives scene changes. Keep its own AudioSource,
        // but adopt the gameplay scene's current clip before that manager dies.
        AudioSource sceneSource = sceneManager.FindAudioSource("SFX-MonsterHit");
        if (sceneSource == null || sceneSource.clip == null)
            return;

        monsterHitSfx = FindAudioSource("SFX-MonsterHit");
        if (monsterHitSfx == null)
        {
            Transform child = transform.Find("SFX-MonsterHit");
            if (child == null)
            {
                GameObject sourceObject = new GameObject("SFX-MonsterHit");
                sourceObject.transform.SetParent(transform, false);
                child = sourceObject.transform;
            }
            monsterHitSfx = child.gameObject.AddComponent<AudioSource>();
        }

        monsterHitSfx.clip = sceneSource.clip;
        monsterHitSfx.volume = sceneSource.volume;
        monsterHitSfx.pitch = sceneSource.pitch;
        monsterHitSfx.mute = sceneSource.mute;
        monsterHitSfx.enabled = sceneSource.enabled;
        monsterHitSfx.gameObject.SetActive(sceneSource.gameObject.activeSelf);
        SetupAudioSource(monsterHitSfx, sceneSource.outputAudioMixerGroup, false);
    }

    public void ConnectSceneSliders()
    {
        Slider[] sliders = Object.FindObjectsByType<Slider>(FindObjectsInactive.Include);

        foreach (Slider slider in sliders)
        {
            if (slider.name == "BgmSlider")
                bgmSlider = slider;
            else if (slider.name == "SfxSlider")
                sfxSlider = slider;
        }
    }

    public void SetupVolumeBars()
    {
        SetupVolumeBar(bgmSlider);
        SetupVolumeBar(sfxSlider);
    }

    public void SetupVolumeBar(Slider slider)
    {
        if (slider == null || volumeBarSprite == null)
            return;

        Transform existingArea = slider.transform.Find("VolumeFillArea");
        GameObject fillAreaObject;

        if (existingArea == null)
        {
            fillAreaObject = new GameObject(
                "VolumeFillArea",
                typeof(RectTransform)
            );
            fillAreaObject.layer = slider.gameObject.layer;
            fillAreaObject.transform.SetParent(slider.transform, false);
        }
        else
        {
            fillAreaObject = existingArea.gameObject;
        }

        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRect.anchoredPosition = Vector2.zero;
        fillAreaRect.sizeDelta = new Vector2(
            -volumeBarHorizontalPadding * 2f,
            volumeBarHeight
        );
        fillAreaRect.SetAsFirstSibling();

        Transform existingFill = fillAreaRect.Find("VolumeFill");
        GameObject fillObject;

        if (existingFill == null)
        {
            fillObject = new GameObject(
                "VolumeFill",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            fillObject.layer = slider.gameObject.layer;
            fillObject.transform.SetParent(fillAreaRect, false);
        }
        else
        {
            fillObject = existingFill.gameObject;
        }

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.sprite = volumeBarSprite;
        fillImage.color = volumeBarColor;
        fillImage.type = Image.Type.Simple;
        fillImage.preserveAspect = false;
        fillImage.raycastTarget = false;

        slider.fillRect = fillRect;
    }

    public void LoadSavedVolumeState()
    {
        float bgmValue = Mathf.Clamp01(PlayerPrefs.GetFloat("bgmSliderVal", 1f));
        float sfxValue = Mathf.Clamp01(PlayerPrefs.GetFloat("sfxSliderVal", 1f));

        bgmMuted = PlayerPrefs.GetInt("bgmMuted", 0) == 1 || IsSilent(bgmValue);
        sfxMuted = PlayerPrefs.GetInt("sfxMuted", 0) == 1 || IsSilent(sfxValue);
    }

    public void SyncSliderValues()
    {
        float bgmValue = bgmMuted
            ? 0f
            : Mathf.Clamp01(PlayerPrefs.GetFloat("bgmSliderVal", 1f));
        float sfxValue = sfxMuted
            ? 0f
            : Mathf.Clamp01(PlayerPrefs.GetFloat("sfxSliderVal", 1f));

        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(bgmValue);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(sfxValue);
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
        monsterHitSfx = FindAudioSource("SFX-MonsterHit");

        AudioMixerGroup bgmGroup = FindMixerGroup("BGM");
        AudioMixerGroup sfxGroup = FindMixerGroup("SFX");

        SetupAudioSource(homeBGM, bgmGroup, true);
        SetupAudioSource(inGameBGM, bgmGroup, true);
        SetupAudioSource(gameOverBGM, bgmGroup, false);
        SetupAudioSource(buttonClickSfx, sfxGroup, false);
        SetupAudioSource(frogCroakSfx, sfxGroup, false);
        SetupAudioSource(fireSfx, sfxGroup, false);
        SetupAudioSource(levelUpSfx, sfxGroup, false);
        SetupAudioSource(monsterHitSfx, sfxGroup, false);
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConnectSceneSliders();
        SetupVolumeBars();
        SyncSliderValues();
        ApplyBgmVolume();
        ApplySfxVolume();
        BindSliders();
        BindButtonAudio();
    }

    public void BindButtonAudio()
    {
        EnsureButton("musicBtn");
        EnsureButton("SFXbtn");
        EnsureButton("homeLogoBtn");
        EnsureButton("homeLogoButton");
        EnsureButton("endGameOver");

        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);

        foreach (Button button in buttons)
        {
            if (button.name == "attackButton")
                continue;

            button.onClick.RemoveListener(PlayButtonClick);
            button.onClick.AddListener(PlayButtonClick);

            if (button.name == "homeLogoButton" || button.name == "homeLogoBtn" || button.name == "endGameOver")
            {
                button.onClick.RemoveListener(PlayButtonClick);
                button.onClick.RemoveListener(PlayFrogCroak);
                button.onClick.AddListener(PlayFrogCroak);
            }

            if (button.name == "musicBtn")
            {
                button.onClick.RemoveListener(ToggleBgmMute);
                button.onClick.AddListener(ToggleBgmMute);
            }

            if (button.name == "SFXbtn")
            {
                button.onClick.RemoveListener(ToggleSfxMute);
                button.onClick.AddListener(ToggleSfxMute);
            }
        }

        UpdateMuteIcons();
    }

    public void EnsureButton(string objectName)
    {
        RectTransform[] rects = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include);

        foreach (RectTransform rect in rects)
        {
            if (rect.name != objectName)
                continue;

            Image image = rect.GetComponent<Image>();
            if (image == null)
                continue;

            image.raycastTarget = true;

            Button button = rect.GetComponent<Button>();
            if (button == null)
                button = rect.gameObject.AddComponent<Button>();

            button.targetGraphic = image;

        }
    }

    public void UpdateMuteIcons()
    {
        UpdateMuteIcon("musicBtn", IsBgmSilent());
        UpdateMuteIcon("SFXbtn", IsSfxSilent());
    }

    public void UpdateMuteIcon(string objectName, bool muted)
    {
        RectTransform[] rects = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include);

        foreach (RectTransform rect in rects)
        {
            if (rect.name != objectName)
                continue;

            Transform existing = rect.Find("MuteIcon");
            GameObject iconObject;

            if (existing == null)
            {
                iconObject = new GameObject("MuteIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(rect, false);

                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = new Vector2(47f, 0f);
                iconRect.sizeDelta = new Vector2(62f, 62f);

                Image iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = GetMuteIconSprite();
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
            else
            {
                iconObject = existing.gameObject;
            }

            iconObject.SetActive(muted);
            iconObject.transform.SetAsLastSibling();
        }
    }

    public Sprite GetMuteIconSprite()
    {
        if (muteIconSprite != null)
            return muteIconSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 red = new Color32(235, 20, 20, 255);
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x, y) - center;
                float radius = point.magnitude;
                bool ring = radius >= 25f && radius <= 30f;
                bool slash = Mathf.Abs(point.x + point.y) <= 4f && radius <= 29f;
                pixels[y * size + x] = ring || slash ? red : clear;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.DontSave;

        muteIconSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        muteIconSprite.name = "RuntimeMuteIcon";
        muteIconSprite.hideFlags = HideFlags.DontSave;
        return muteIconSprite;
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
        ConnectSceneSliders();

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

    public void PlayMonsterHitSfx()
    {
        PlaySfx(monsterHitSfx);
    }

    public void PlaySfx(AudioSource source)
    {
        if (source != null && source.clip != null)
            source.PlayOneShot(source.clip);
    }

    public void SetBgmVol(float val)
    {
        val = Mathf.Clamp01(val);
        bool muteStateChanged = bgmMuted != IsSilent(val);
        bgmMuted = IsSilent(val);
        float vol = bgmMuted ? -80f : ToDecibel(val);

        PlayerPrefs.SetFloat("bgmSliderVal", val);
        PlayerPrefs.SetFloat("bgmVol", vol);
        PlayerPrefs.SetInt("bgmMuted", bgmMuted ? 1 : 0);

        if (mixer != null)
            mixer.SetFloat("bgm", vol);

        if (muteStateChanged)
            UpdateMuteIcons();
    }

    public void SetSfxVol(float val)
    {
        val = Mathf.Clamp01(val);
        bool muteStateChanged = sfxMuted != IsSilent(val);
        sfxMuted = IsSilent(val);
        float vol = sfxMuted ? -80f : ToDecibel(val);

        PlayerPrefs.SetFloat("sfxSliderVal", val);
        PlayerPrefs.SetFloat("sfxVol", vol);
        PlayerPrefs.SetInt("sfxMuted", sfxMuted ? 1 : 0);

        if (mixer != null)
            mixer.SetFloat("sfx", vol);

        if (muteStateChanged)
            UpdateMuteIcons();
    }

    public void ToggleBgmMute()
    {
        float newValue = IsBgmSilent() ? 1f : 0f;

        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(newValue);

        SetBgmVol(newValue);
        PlayerPrefs.Save();
    }

    public void ToggleSfxMute()
    {
        bool wasSilent = IsSfxSilent();
        float newValue = wasSilent ? 1f : 0f;

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(newValue);

        SetSfxVol(newValue);
        PlayerPrefs.Save();

        if (wasSilent)
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

        float bgmValue = Mathf.Clamp01(PlayerPrefs.GetFloat("bgmSliderVal", 1f));
        float bgmVolume = ToDecibel(bgmValue);
        mixer.SetFloat("bgm", IsBgmSilent() ? -80f : bgmVolume);
    }

    public void ApplySfxVolume()
    {
        if (mixer == null)
            return;

        float sfxValue = Mathf.Clamp01(PlayerPrefs.GetFloat("sfxSliderVal", 1f));
        float sfxVolume = ToDecibel(sfxValue);
        mixer.SetFloat("sfx", IsSfxSilent() ? -80f : sfxVolume);
    }

    public bool IsBgmSilent()
    {
        float value = bgmSlider != null
            ? bgmSlider.value
            : PlayerPrefs.GetFloat("bgmSliderVal", 1f);
        return bgmMuted || IsSilent(value);
    }

    public bool IsSfxSilent()
    {
        float value = sfxSlider != null
            ? sfxSlider.value
            : PlayerPrefs.GetFloat("sfxSliderVal", 1f);
        return sfxMuted || IsSilent(value);
    }

    public bool IsSilent(float value)
    {
        return value <= SilentSliderValue;
    }

    public float ToDecibel(float val)
    {
        return Mathf.Log10(Mathf.Clamp(val, SilentSliderValue, 1f)) * 20f;
    }
}
