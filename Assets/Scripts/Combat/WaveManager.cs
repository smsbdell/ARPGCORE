using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class WaveManager : MonoBehaviour
{
    [Serializable]
    public class WaveEvent : UnityEvent<int> { }

    [Serializable]
    public class WaveCountdownEvent : UnityEvent<int, float> { }

    [Header("References")]
    [SerializeField] private MonsterSpawner _monsterSpawner;
    [SerializeField] private MonsterWaveScalingConfig _waveScalingConfig;

    [Header("Wave Timing")]
    [Tooltip("How long a wave runs before pausing spawning.")]
    [SerializeField] private float _waveDuration = 30f;
    [Tooltip("Delay between waves after all monsters are cleared.")]
    [SerializeField] private float _interWaveDelay = 5f;
    [SerializeField] private bool _autoStart = true;

    [Header("Spawn Scaling")]
    [Tooltip("Base spawn interval (seconds) for wave 1 before scaling.")]
    [SerializeField] private float _baseSpawnInterval = 2f;
    [Tooltip("Base maximum concurrent monsters for wave 1 before scaling.")]
    [SerializeField] private int _baseMaxMonsters = 30;
    [Tooltip("Multiplier curve evaluated by wave index (starting at 1) to adjust spawn interval per wave.")]
    [SerializeField] private AnimationCurve _spawnIntervalCurve = AnimationCurve.Linear(1f, 1f, 10f, 0.4f);
    [Tooltip("Multiplier curve evaluated by wave index (starting at 1) to adjust max monsters per wave.")]
    [SerializeField] private AnimationCurve _maxMonstersCurve = AnimationCurve.Linear(1f, 1f, 10f, 2f);

    [Header("Events")]
    [SerializeField] private WaveEvent _onWaveStarted = new WaveEvent();
    [SerializeField] private WaveEvent _onWaveEnded = new WaveEvent();
    [SerializeField] private WaveCountdownEvent _onInterWaveCountdown = new WaveCountdownEvent();

    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveEnded;
    public event Action<int, float> OnInterWaveCountdown;

    public int CurrentWave => _currentWave;

    private int _currentWave = 0;
    private Coroutine _waveRoutine;

    private void Awake()
    {
        if (_monsterSpawner == null)
            Debug.LogError("WaveManager requires a MonsterSpawner reference.");
    }

    private void OnEnable()
    {
        if (_autoStart)
            StartWaves();
    }

    private void OnDisable()
    {
        StopWaveRoutine();
        _monsterSpawner?.SetSpawningEnabled(false);
        _monsterSpawner?.DespawnAll();
    }

    public void StartWaves()
    {
        if (_waveRoutine != null || _monsterSpawner == null)
            return;

        _waveRoutine = StartCoroutine(WaveLoop());
    }

    public void StopWaves()
    {
        StopWaveRoutine();
        _monsterSpawner?.SetSpawningEnabled(false);
        _monsterSpawner?.DespawnAll();
    }

    private IEnumerator WaveLoop()
    {
        while (enabled && _monsterSpawner != null)
        {
            _currentWave++;
            StartWave(_currentWave);

            yield return RunWaveDuration();

            EndWave(_currentWave);

            yield return RunInterWaveDelay();
        }

        _waveRoutine = null;
    }

    private void StartWave(int waveIndex)
    {
        _monsterSpawner.DespawnAll();
        _monsterSpawner.SetSpawnContext(BuildSpawnContext(waveIndex));
        _monsterSpawner.UpdateSpawnSettings(CalculateSpawnInterval(waveIndex), CalculateMaxMonsters(waveIndex));
        _monsterSpawner.SetSpawningEnabled(true);

        OnWaveStarted?.Invoke(waveIndex);
        _onWaveStarted?.Invoke(waveIndex);
    }

    private void EndWave(int waveIndex)
    {
        _monsterSpawner.SetSpawningEnabled(false);
        _monsterSpawner.DespawnAll();

        OnWaveEnded?.Invoke(waveIndex);
        _onWaveEnded?.Invoke(waveIndex);
    }

    private IEnumerator RunWaveDuration()
    {
        float remaining = Mathf.Max(0f, _waveDuration);
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            yield return null;
        }

        yield return null;
    }

    private IEnumerator RunInterWaveDelay()
    {
        float remaining = Mathf.Max(0f, _interWaveDelay);
        while (remaining > 0f)
        {
            OnInterWaveCountdown?.Invoke(_currentWave + 1, remaining);
            _onInterWaveCountdown?.Invoke(_currentWave + 1, remaining);

            remaining -= Time.deltaTime;
            yield return null;
        }
    }

    private float CalculateSpawnInterval(int waveIndex)
    {
        float multiplier = _spawnIntervalCurve != null
            ? _spawnIntervalCurve.Evaluate(Mathf.Max(1f, waveIndex))
            : 1f;

        return Mathf.Max(0.05f, _baseSpawnInterval * multiplier);
    }

    private int CalculateMaxMonsters(int waveIndex)
    {
        float multiplier = _maxMonstersCurve != null
            ? _maxMonstersCurve.Evaluate(Mathf.Max(1f, waveIndex))
            : 1f;

        int scaledMax = Mathf.RoundToInt(_baseMaxMonsters * multiplier);
        return Mathf.Max(0, scaledMax);
    }

    private MonsterSpawnContext BuildSpawnContext(int waveIndex)
    {
        if (_waveScalingConfig != null)
            return _waveScalingConfig.GetContextForWave(waveIndex);

        MonsterSpawnContext context = MonsterSpawnContext.Default;
        context.WaveIndex = Mathf.Max(1, waveIndex);
        return context;
    }

    private void StopWaveRoutine()
    {
        if (_waveRoutine != null)
        {
            StopCoroutine(_waveRoutine);
            _waveRoutine = null;
        }
    }
}
