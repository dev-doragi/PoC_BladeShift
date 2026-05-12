using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[DefaultExecutionOrder(-97)]
/// <summary>
/// 이름 기반 풀을 관리하는 공통 오브젝트 풀 매니저입니다.
/// </summary>
public class PoolManager : Singleton<PoolManager>
{
    private readonly Dictionary<string, IObjectPool<GameObject>> _pools = new();

    [Header("Pool Setup")]
    [SerializeField] private Transform _poolRoot;
    [SerializeField] private RectTransform _uiPoolRoot;

    [Header("Global Pool Setup")]
    [Tooltip("프로젝트 전역에서 재사용할 프리팹 풀 목록")]
    [SerializeField] private List<PoolSetupData> _globalPools = new List<PoolSetupData>();

    protected override void OnBootstrap()
    {
        if (_poolRoot == null)
        {
            GameObject rootObj = new GameObject("PoolRoot");
            DontDestroyOnLoad(rootObj);
            _poolRoot = rootObj.transform;
        }

        if (_uiPoolRoot == null)
        {
        }

        foreach (var setup in _globalPools)
        {
            if (setup.Prefab != null)
            {
                CreatePool(setup.Prefab, setup.InitialSize, setup.MaxSize);
            }
        }
    }

    public void CreatePool(GameObject prefab, int initialSize, int maxSize = 100)
    {
        if (prefab == null)
        {
            Debug.LogError("[PoolManager] 생성할 프리팹이 null입니다.");
            return;
        }

        string key = prefab.name;
        if (_pools.ContainsKey(key)) return;

        bool isUI = prefab.GetComponent<RectTransform>() != null;

        if (isUI && _uiPoolRoot == null)
        {
            Debug.LogWarning($"[PoolManager] '{prefab.name}'은 UI 프리팹이지만 _uiPoolRoot가 설정되지 않았습니다. 풀링을 스킵합니다.");
            return;
        }

        Transform targetRoot = isUI ? _uiPoolRoot : _poolRoot;

        IObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject obj = Instantiate(prefab, targetRoot);
                obj.name = prefab.name;
                obj.SetActive(false);
                return obj;
            },
            actionOnGet: null,
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: obj => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: initialSize,
            maxSize: maxSize
        );

        _pools.Add(key, pool);

        if (initialSize > 0)
        {
            GameObject[] prewarmObjects = new GameObject[initialSize];
            for (int i = 0; i < initialSize; i++) prewarmObjects[i] = pool.Get();
            for (int i = 0; i < initialSize; i++) pool.Release(prewarmObjects[i]);
        }
    }

    public GameObject Spawn(string prefabName, Vector3 position, Quaternion rotation)
    {
        if (!_pools.TryGetValue(prefabName, out var pool))
        {
            Debug.LogError($"[PoolManager] '{prefabName}'에 해당하는 풀이 존재하지 않습니다. 먼저 CreatePool을 호출하세요.");
            return null;
        }

        GameObject obj = pool.Get();
        if (obj == null)
        {
            Debug.LogWarning($"[PoolManager] '{prefabName}' 풀에서 파괴된 오브젝트를 반환받았습니다. 재시도합니다.");
            obj = pool.Get();
            if (obj == null) return null;
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    public void Despawn(GameObject obj)
    {
        string key = obj.name;
        if (!_pools.TryGetValue(key, out var pool))
        {
            Destroy(obj);
            return;
        }

        pool.Release(obj);

        bool isUI = obj.GetComponent<RectTransform>() != null;
        obj.transform.SetParent(isUI ? _uiPoolRoot : _poolRoot);
    }

    public void ClearAllPools()
    {
        foreach (var pool in _pools.Values)
        {
            pool.Clear();
        }
        _pools.Clear();
    }
}