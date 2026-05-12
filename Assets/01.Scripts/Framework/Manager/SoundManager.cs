using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[DefaultExecutionOrder(-120)]
/// <summary>
/// 전역 배경음(BGM)과 효과음(SFX) 재생을 담당하는 오디오 관리자입니다.
/// </summary>
public class SoundManager : Singleton<SoundManager>
{
    [Header("Pool Setup")]
    [SerializeField] private GameObject _soundPlayerPrefab;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmSource;

    [Header("BGM Assets")]
    [SerializeField] private AudioClip _lobbyBGM;
    [SerializeField] private AudioClip _prepareBGM;
    [SerializeField] private AudioClip _waveBGM;
    [SerializeField] private AudioClip _winBGM;

    [Header("BGM Volume")]
    [SerializeField, Range(0f, 1f)] private float _bgmVolume = 1f;

    [Header("Scene Transition BGM Control")]
    [SerializeField] private bool _stopBGMOnSceneLoaded = true;
    [SerializeField] private List<string> _preserveBGMSceneNames = new List<string>();

    private float _bgmVolumeMultiplier = 1f;
    private bool _skipNextSceneLoadedBGMStop;

    public float BGMVolume
    {
        get => _bgmVolume;
        set
        {
            _bgmVolume = Mathf.Clamp01(value);
            UpdateBGMVolume();
        }
    }

    public float BGMVolumeMultiplier
    {
        get => _bgmVolumeMultiplier;
        set
        {
            _bgmVolumeMultiplier = Mathf.Max(0f, value);
            UpdateBGMVolume();
        }
    }

    protected override void OnBootstrap()
    {
        if (_bgmSource == null)
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
        }

        UpdateBGMVolume();

        if (GameManager.Instance != null)
        {
            OnGameStateChanged(new GameStateChangedEvent { NewState = GameManager.Instance.CurrentState });
        }

        if (GameFlowManager.Instance != null)
        {
            OnInGameStateChanged(new InGameStateChangedEvent { NewState = GameFlowManager.Instance.CurrentInGameState });
        }
    }

    private void OnEnable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Instance.Subscribe<InGameStateChangedEvent>(OnInGameStateChanged);
            EventBus.Instance.Subscribe<PlaySFXEvent>(OnPlaySFX);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Instance.Unsubscribe<InGameStateChangedEvent>(OnInGameStateChanged);
            EventBus.Instance.Unsubscribe<PlaySFXEvent>(OnPlaySFX);
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_stopBGMOnSceneLoaded)
        {
            return;
        }

        if (_skipNextSceneLoadedBGMStop)
        {
            _skipNextSceneLoadedBGMStop = false;
            return;
        }

        if (IsPreservedScene(scene.name))
        {
            return;
        }

        StopBGM();
    }

    private bool IsPreservedScene(string sceneName)
    {
        for (int i = 0; i < _preserveBGMSceneNames.Count; i++)
        {
            if (_preserveBGMSceneNames[i] == sceneName)
            {
                return true;
            }
        }

        return false;
    }

    public void RequestSkipNextSceneLoadedBGMStop()
    {
        _skipNextSceneLoadedBGMStop = true;
    }

    public void StopBGM(bool clearClip = true)
    {
        if (_bgmSource == null)
        {
            return;
        }

        if (_bgmSource.isPlaying)
        {
            _bgmSource.Stop();
        }
        else
        {
            _bgmSource.Stop();
        }

        if (clearClip)
        {
            _bgmSource.clip = null;
        }
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        switch (evt.NewState)
        {
            case GameState.Ready:
                PlayBGM(_lobbyBGM);
                break;

            case GameState.GameOver:
            case GameState.GameClear:
                StopBGM();
                break;
        }
    }

    private void OnInGameStateChanged(InGameStateChangedEvent evt)
    {
        switch (evt.NewState)
        {
            case InGameState.Prepare:
                PlayBGM(_prepareBGM);
                break;
            case InGameState.WavePlaying:
                PlayBGM(_waveBGM);
                break;
            case InGameState.StageCleared:
                PlayBGM(_winBGM);
                break;
        }
    }

    private void OnPlaySFX(PlaySFXEvent evt)
    {
        PlaySFX(evt.Clip, Vector3.zero, evt.Volume);
    }

    private void UpdateBGMVolume()
    {
        if (_bgmSource != null)
        {
            _bgmSource.volume = _bgmVolume * _bgmVolumeMultiplier;
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || _bgmSource == null)
        {
            return;
        }

        if (_bgmSource.clip == clip && _bgmSource.isPlaying)
        {
            return;
        }

        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        if (PoolManager.Instance == null || _soundPlayerPrefab == null) return;

        GameObject obj = PoolManager.Instance.Spawn(_soundPlayerPrefab.name, position, Quaternion.identity);
        if (obj == null) return;

        SoundPlayer player = obj.GetComponentInChildren<SoundPlayer>(true);
        if (player != null)
        {
            player.Play(clip, volume);
        }
        else
        {
            Debug.LogError($"[SoundManager] SoundPlayer 컴포넌트를 찾을 수 없음: {obj.name}", obj);
        }
    }
}