using UnityEngine;

public interface ISingletonBootstrap
{
    bool IsBootstrapped { get; }
    void BootstrapIfNeeded();
}

[DefaultExecutionOrder(-100)]
public abstract class Singleton<T> : MonoBehaviour, ISingletonBootstrap where T : MonoBehaviour
{
    [SerializeField] protected bool _isDontDestroyOnLoad = true;

    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogWarning($"[{typeof(T).Name}] 인스턴스가 씬에 존재하지 않습니다.");
            }
            return _instance;
        }
    }

    public bool IsBootstrapped { get; private set; }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this as T)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        if (_isDontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Bootstrapper에 의해 호출되는 명시적 초기화 메서드입니다.
    /// </summary>
    public void BootstrapIfNeeded()
    {
        if (IsBootstrapped) return;

        OnBootstrap();
        IsBootstrapped = true;
    }

    /// <summary>
    /// 실제 초기화 로직(이벤트 구독, 데이터 로드 등)을 구현합니다.
    /// </summary>
    protected virtual void OnBootstrap() { }

    protected virtual void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}