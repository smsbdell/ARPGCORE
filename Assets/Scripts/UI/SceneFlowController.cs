using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowController : MonoBehaviour
{
    [Serializable]
    public class WaveModeOption
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public MonsterWaveScalingConfig scalingConfig;
        [Min(0)] public int totalWaves = 10;
    }

    [Serializable]
    public class AreaOption
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
    }

    public struct RunSelection
    {
        public WaveModeOption WaveMode { get; }
        public AreaOption Area { get; }

        public RunSelection(WaveModeOption waveMode, AreaOption area)
        {
            WaveMode = waveMode;
            Area = area;
        }
    }

    public struct CombatRunContext
    {
        public RunSelection Selection;
        public WaveManager WaveManager;
        public MonsterSpawner Spawner;
        public MonsterWaveScalingConfig ScalingConfig;
        public int TotalWaves;
    }

    public static SceneFlowController Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string _baseCampSceneName = "BaseCamp";
    [SerializeField] private string _combatSceneName = "SampleScene";

    [Header("Run Options")]
    [SerializeField] private List<WaveModeOption> _waveModes = new List<WaveModeOption>();
    [SerializeField] private List<AreaOption> _areaOptions = new List<AreaOption>();

    private int _waveModeIndex;
    private int _areaIndex;
    private RunSelection? _activeSelection;
    private bool _isCombatLoading;
    private CombatRunContext? _activeRunContext;

    public event Action<RunSelection> OnSelectionChanged;
    public event Action<bool> OnRunLoadingStateChanged;
    public event Action<CombatRunContext> OnCombatRunPrepared;

    public bool IsCombatLoading => _isCombatLoading;
    public WaveModeOption CurrentWaveMode => _waveModes.Count > 0 ? _waveModes[_waveModeIndex] : null;
    public AreaOption CurrentArea => _areaOptions.Count > 0 ? _areaOptions[_areaIndex] : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureDefaults();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        NotifySelectionChanged();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsBaseCamp(scene.name))
        {
            ClearActiveRunContext();
            SetCombatLoading(false);
            return;
        }

        if (IsCombatScene(scene.name))
        {
            ApplyCombatConfiguration();
        }
    }

    public void GoToNextWaveMode()
    {
        if (_waveModes.Count == 0)
            return;

        _waveModeIndex = (_waveModeIndex + 1) % _waveModes.Count;
        NotifySelectionChanged();
    }

    public void GoToPreviousWaveMode()
    {
        if (_waveModes.Count == 0)
            return;

        _waveModeIndex = (_waveModeIndex - 1 + _waveModes.Count) % _waveModes.Count;
        NotifySelectionChanged();
    }

    public void GoToNextArea()
    {
        if (_areaOptions.Count == 0)
            return;

        _areaIndex = (_areaIndex + 1) % _areaOptions.Count;
        NotifySelectionChanged();
    }

    public void GoToPreviousArea()
    {
        if (_areaOptions.Count == 0)
            return;

        _areaIndex = (_areaIndex - 1 + _areaOptions.Count) % _areaOptions.Count;
        NotifySelectionChanged();
    }

    public void StartCombatRun()
    {
        if (_isCombatLoading)
            return;

        if (!EnsureSelection())
            return;

        _activeSelection = BuildSelection();
        SetCombatLoading(true);
        StartCoroutine(LoadCombatSceneRoutine());
    }

    public void ReturnToBaseCamp()
    {
        if (IsBaseCamp(SceneManager.GetActiveScene().name))
            return;

        StopCurrentRun();
        SceneManager.LoadSceneAsync(_baseCampSceneName);
    }

    public bool TryGetActiveRunContext(out CombatRunContext context)
    {
        if (_activeRunContext.HasValue)
        {
            context = _activeRunContext.Value;
            return true;
        }

        context = default;
        return false;
    }

    public RunSelection BuildSelection()
    {
        WaveModeOption wave = _waveModes.Count > 0 ? _waveModes[_waveModeIndex] : new WaveModeOption { displayName = "Unknown" };
        AreaOption area = _areaOptions.Count > 0 ? _areaOptions[_areaIndex] : new AreaOption { displayName = "Unknown" };
        return new RunSelection(wave, area);
    }

    private IEnumerator LoadCombatSceneRoutine()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(_combatSceneName);
        while (!op.isDone)
        {
            yield return null;
        }

        SetCombatLoading(false);
    }

    private void ApplyCombatConfiguration()
    {
        RunSelection selection = _activeSelection ?? BuildSelection();

        MonsterSpawner spawner = FindObjectOfType<MonsterSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("SceneFlowController could not find a MonsterSpawner in the combat scene.");
            return;
        }

        spawner.SetSpawningEnabled(false);
        spawner.DespawnAll();

        WaveManager waveManager = spawner.GetComponent<WaveManager>();
        if (waveManager == null)
        {
            waveManager = spawner.gameObject.AddComponent<WaveManager>();
        }

        waveManager.ResetProgress();
        waveManager.ConfigureForRun(spawner, selection.WaveMode.scalingConfig, false);
        waveManager.StartWaves();

        PublishCombatRun(waveManager, spawner, selection);
    }

    private void PublishCombatRun(WaveManager waveManager, MonsterSpawner spawner, RunSelection selection)
    {
        ClearActiveRunContext();

        CombatRunContext context = new CombatRunContext
        {
            Selection = selection,
            WaveManager = waveManager,
            Spawner = spawner,
            ScalingConfig = selection.WaveMode?.scalingConfig,
            TotalWaves = selection.WaveMode != null ? Mathf.Max(0, selection.WaveMode.totalWaves) : 0
        };

        _activeRunContext = context;
        OnCombatRunPrepared?.Invoke(context);
    }

    private void StopCurrentRun()
    {
        if (_activeRunContext.HasValue)
        {
            CombatRunContext context = _activeRunContext.Value;
            context.WaveManager?.StopWaves();
            context.Spawner?.DespawnAll();
        }

        ClearActiveRunContext();
    }

    private void EnsureDefaults()
    {
        if (_waveModes.Count == 0)
        {
            _waveModes.Add(new WaveModeOption
            {
                id = "standard",
                displayName = "Standard Waves",
                description = "Balanced monster growth tuned for steady runs.",
                scalingConfig = null,
                totalWaves = 10
            });

            _waveModes.Add(new WaveModeOption
            {
                id = "onslaught",
                displayName = "Onslaught",
                description = "Spawns ramp faster for testing loadouts.",
                scalingConfig = null,
                totalWaves = 10
            });
        }

        if (_areaOptions.Count == 0)
        {
            _areaOptions.Add(new AreaOption
            {
                id = "forest",
                displayName = "Whispering Forest",
                description = "Cooldowns between waves give room to breathe."
            });

            _areaOptions.Add(new AreaOption
            {
                id = "ruins",
                displayName = "Sunken Ruins",
                description = "Tighter arenas that favor close-range builds."
            });
        }

        _waveModeIndex = Mathf.Clamp(_waveModeIndex, 0, _waveModes.Count - 1);
        _areaIndex = Mathf.Clamp(_areaIndex, 0, _areaOptions.Count - 1);
    }

    private bool EnsureSelection()
    {
        if (_waveModes.Count == 0)
        {
            Debug.LogWarning("SceneFlowController has no wave modes configured.");
            return false;
        }

        if (_areaOptions.Count == 0)
        {
            Debug.LogWarning("SceneFlowController has no area options configured.");
            return false;
        }

        return true;
    }

    private void ClearActiveRunContext()
    {
        _activeRunContext = null;
        _activeSelection = null;
    }

    private void NotifySelectionChanged()
    {
        OnSelectionChanged?.Invoke(BuildSelection());
    }

    private void SetCombatLoading(bool isLoading)
    {
        _isCombatLoading = isLoading;
        OnRunLoadingStateChanged?.Invoke(_isCombatLoading);
    }

    private bool IsBaseCamp(string sceneName)
    {
        return string.Equals(sceneName, _baseCampSceneName, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCombatScene(string sceneName)
    {
        return string.Equals(sceneName, _combatSceneName, StringComparison.OrdinalIgnoreCase);
    }
}
