using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private struct RunSelection
    {
        public WaveModeOption waveMode;
        public AreaOption area;
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

    private Canvas _baseCampCanvas;
    private Text _waveModeLabel;
    private Text _areaLabel;
    private Text _detailsLabel;
    private Text _lootDetailsLabel;
    private Text _skillDetailsLabel;
    private Button _startButton;

    private Canvas _combatCanvas;
    private Text _combatSummary;

    [Header("Debug Options")]
    [SerializeField] private bool _showWaveDebugOverlay;

    private Text _waveDebugLabel;
    private WaveManager _activeWaveManager;
    private MonsterSpawner _activeSpawner;
    private MonsterWaveScalingConfig _activeScalingConfig;
    private int _totalWavesForMap;

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

        if (IsBaseCamp(SceneManager.GetActiveScene().name))
        {
            BuildBaseCampUi();
            UpdateBaseCampUi();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        DetachWaveManager();
    }

    private void Update()
    {
        if (_showWaveDebugOverlay)
            UpdateWaveDebugLabel();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsBaseCamp(scene.name))
        {
            ShowBaseCampUi();
            return;
        }

        HideBaseCampUi();

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
        UpdateBaseCampUi();
    }

    public void GoToPreviousWaveMode()
    {
        if (_waveModes.Count == 0)
            return;

        _waveModeIndex = (_waveModeIndex - 1 + _waveModes.Count) % _waveModes.Count;
        UpdateBaseCampUi();
    }

    public void GoToNextArea()
    {
        if (_areaOptions.Count == 0)
            return;

        _areaIndex = (_areaIndex + 1) % _areaOptions.Count;
        UpdateBaseCampUi();
    }

    public void GoToPreviousArea()
    {
        if (_areaOptions.Count == 0)
            return;

        _areaIndex = (_areaIndex - 1 + _areaOptions.Count) % _areaOptions.Count;
        UpdateBaseCampUi();
    }

    public void ShowLootPreview()
    {
        if (_lootDetailsLabel != null)
        {
            _lootDetailsLabel.text = "Loot inspection placeholder:\nPreview drops and stash here.";
        }
    }

    public void ShowSkillTreePreview()
    {
        if (_skillDetailsLabel != null)
        {
            _skillDetailsLabel.text = "Skill trees placeholder:\nPlan upgrades and respecs here.";
        }
    }

    public void StartCombatRun()
    {
        if (_isCombatLoading)
            return;

        if (!EnsureSelection())
            return;

        _activeSelection = BuildSelection();
        _isCombatLoading = true;
        if (_startButton != null)
            _startButton.interactable = false;

        StartCoroutine(LoadCombatSceneRoutine());
    }

    public void ReturnToBaseCamp()
    {
        if (IsBaseCamp(SceneManager.GetActiveScene().name))
            return;

        StopCurrentRun();
        SceneManager.LoadSceneAsync(_baseCampSceneName);
    }

    private IEnumerator LoadCombatSceneRoutine()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(_combatSceneName);
        while (!op.isDone)
        {
            yield return null;
        }

        _isCombatLoading = false;
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
        waveManager.ConfigureForRun(spawner, selection.waveMode.scalingConfig, false);
        waveManager.StartWaves();

        TrackActiveRun(waveManager, spawner, selection.waveMode.scalingConfig, selection.waveMode.totalWaves);

        BuildCombatOverlay();
        UpdateCombatOverlay(selection);
    }

    private void StopCurrentRun()
    {
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
            waveManager.StopWaves();

        MonsterSpawner spawner = FindObjectOfType<MonsterSpawner>();
        if (spawner != null)
            spawner.DespawnAll();

        TrackActiveRun(null, null, null, 0);
    }

    private void BuildBaseCampUi()
    {
        if (_baseCampCanvas != null)
            return;

        _baseCampCanvas = CreateCanvas("BaseCampCanvas");
        _baseCampCanvas.gameObject.SetActive(IsBaseCamp(SceneManager.GetActiveScene().name));

        RectTransform panel = CreatePanel(_baseCampCanvas.transform, new Vector2(1080, 720));
        _detailsLabel = CreateText(panel, "Details", string.Empty, 16, TextAnchor.UpperLeft, new Vector2(-500f, 180f), new Vector2(1000f, 120f));

        CreateText(panel, "Title", "Base Camp", 24, TextAnchor.UpperLeft, new Vector2(-500f, 300f), new Vector2(300f, 40f));

        _waveModeLabel = CreateText(panel, "WaveLabel", string.Empty, 18, TextAnchor.MiddleLeft, new Vector2(-240f, 110f), new Vector2(480f, 32f));
        CreateButton(panel, "PrevWave", "<", new Vector2(-500f, 110f), new Vector2(50f, 32f), GoToPreviousWaveMode);
        CreateButton(panel, "NextWave", ">", new Vector2(500f - 50f, 110f), new Vector2(50f, 32f), GoToNextWaveMode);

        _areaLabel = CreateText(panel, "AreaLabel", string.Empty, 18, TextAnchor.MiddleLeft, new Vector2(-240f, 60f), new Vector2(480f, 32f));
        CreateButton(panel, "PrevArea", "<", new Vector2(-500f, 60f), new Vector2(50f, 32f), GoToPreviousArea);
        CreateButton(panel, "NextArea", ">", new Vector2(500f - 50f, 60f), new Vector2(50f, 32f), GoToNextArea);

        _startButton = CreateButton(panel, "StartCombat", "Enter Combat", new Vector2(0f, -10f), new Vector2(240f, 50f), StartCombatRun);

        Button lootButton = CreateButton(panel, "LootButton", "Inspect Loot", new Vector2(-200f, -120f), new Vector2(200f, 40f), ShowLootPreview);
        lootButton.GetComponent<Image>().color = new Color(0.24f, 0.35f, 0.25f, 0.9f);
        _lootDetailsLabel = CreateText(panel, "LootDetails", "Select 'Inspect Loot' to preview upcoming treasure.", 14, TextAnchor.UpperLeft, new Vector2(-500f, -190f), new Vector2(480f, 180f));

        Button skillButton = CreateButton(panel, "SkillButton", "Manage Skill Trees", new Vector2(200f, -120f), new Vector2(200f, 40f), ShowSkillTreePreview);
        skillButton.GetComponent<Image>().color = new Color(0.25f, 0.32f, 0.42f, 0.9f);
        _skillDetailsLabel = CreateText(panel, "SkillDetails", "Select 'Manage Skill Trees' to plan upgrades.", 14, TextAnchor.UpperLeft, new Vector2(20f, -190f), new Vector2(480f, 180f));
    }

    private void BuildCombatOverlay()
    {
        if (_combatCanvas != null)
            return;

        _combatCanvas = CreateCanvas("CombatOverlay");
        RectTransform panel = CreatePanel(_combatCanvas.transform, new Vector2(420f, _showWaveDebugOverlay ? 260f : 200f));
        panel.anchorMin = new Vector2(1f, 1f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-20f, -20f);

        _combatSummary = CreateText(panel, "CombatSummary", string.Empty, 14, TextAnchor.UpperLeft, new Vector2(-190f, _showWaveDebugOverlay ? 90f : 50f), new Vector2(380f, _showWaveDebugOverlay ? 80f : 120f));
        Button returnButton = CreateButton(panel, "ReturnButton", "Return to Base Camp", new Vector2(-120f, -50f), new Vector2(240f, 40f), ReturnToBaseCamp);
        returnButton.GetComponent<Image>().color = new Color(0.35f, 0.21f, 0.21f, 0.9f);

        if (_showWaveDebugOverlay)
        {
            _waveDebugLabel = CreateText(panel, "WaveDebug", "Wave debug overlay initializing...", 12, TextAnchor.UpperLeft, new Vector2(-190f, 5f), new Vector2(380f, 120f));
            _waveDebugLabel.color = new Color(0.8f, 0.9f, 1f, 1f);
        }

        _combatCanvas.gameObject.SetActive(false);
    }

    private void UpdateBaseCampUi()
    {
        if (_waveModes.Count > 0 && _waveModeLabel != null)
        {
            WaveModeOption wave = _waveModes[_waveModeIndex];
            _waveModeLabel.text = $"Wave Mode: {wave.displayName}";
            if (_detailsLabel != null)
                _detailsLabel.text = wave.description;
        }

        if (_areaOptions.Count > 0 && _areaLabel != null)
        {
            AreaOption area = _areaOptions[_areaIndex];
            _areaLabel.text = $"Area: {area.displayName}";
            if (_detailsLabel != null)
                _detailsLabel.text += $"\n\nArea Notes: {area.description}";
        }

        if (_startButton != null)
        {
            _startButton.interactable = !_isCombatLoading;
        }
    }

    private void UpdateCombatOverlay(RunSelection selection)
    {
        if (_combatCanvas == null)
            return;

        _combatCanvas.gameObject.SetActive(true);

        if (_combatSummary != null)
        {
            string waveName = selection.waveMode?.displayName ?? "Unknown";
            string areaName = selection.area?.displayName ?? "Unknown";
            _combatSummary.text = $"Running {waveName}\nArea: {areaName}\nUse the button below to return when finished.";
        }
    }

    private Canvas CreateCanvas(string name)
    {
        GameObject canvasObj = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        return canvas;
    }

    private RectTransform CreatePanel(Transform parent, Vector2 size)
    {
        GameObject panelObj = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObj.transform.SetParent(parent, false);

        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        Image image = panelObj.GetComponent<Image>();
        image.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        return rect;
    }

    private Text CreateText(Transform parent, string name, string value, int fontSize, TextAnchor anchor, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Text text = textObj.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = Color.white;
        return text;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObj.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        Button button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        Text text = CreateText(buttonObj.transform, name + "Label", label, 16, TextAnchor.MiddleCenter, Vector2.zero, size);
        text.color = Color.white;

        return button;
    }

    private bool IsBaseCamp(string sceneName)
    {
        return string.Equals(sceneName, _baseCampSceneName, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCombatScene(string sceneName)
    {
        return string.Equals(sceneName, _combatSceneName, StringComparison.OrdinalIgnoreCase);
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

    private RunSelection BuildSelection()
    {
        WaveModeOption wave = _waveModes.Count > 0 ? _waveModes[_waveModeIndex] : new WaveModeOption { displayName = "Unknown" };
        AreaOption area = _areaOptions.Count > 0 ? _areaOptions[_areaIndex] : new AreaOption { displayName = "Unknown" };
        return new RunSelection { waveMode = wave, area = area };
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

    private void ShowBaseCampUi()
    {
        if (_baseCampCanvas == null)
        {
            BuildBaseCampUi();
        }

        if (_baseCampCanvas != null)
        {
            _baseCampCanvas.gameObject.SetActive(true);
            UpdateBaseCampUi();
        }

        if (_combatCanvas != null)
            _combatCanvas.gameObject.SetActive(false);

        _isCombatLoading = false;
        if (_startButton != null)
            _startButton.interactable = true;
    }

    private void HideBaseCampUi()
    {
        if (_baseCampCanvas != null)
            _baseCampCanvas.gameObject.SetActive(false);
    }

    private void TrackActiveRun(WaveManager waveManager, MonsterSpawner spawner, MonsterWaveScalingConfig scalingConfig, int totalWaves)
    {
        DetachWaveManager();

        _activeWaveManager = waveManager;
        _activeSpawner = spawner;
        _activeScalingConfig = scalingConfig;
        _totalWavesForMap = Mathf.Max(0, totalWaves);

        if (_activeWaveManager != null)
            _activeWaveManager.OnWaveStarted += HandleWaveStarted;

        UpdateWaveDebugLabel();
    }

    private void DetachWaveManager()
    {
        if (_activeWaveManager != null)
            _activeWaveManager.OnWaveStarted -= HandleWaveStarted;
    }

    private void HandleWaveStarted(int waveIndex)
    {
        UpdateWaveDebugLabel();
    }

    private void UpdateWaveDebugLabel()
    {
        if (!_showWaveDebugOverlay || _waveDebugLabel == null)
            return;

        if (_combatCanvas == null || !_combatCanvas.gameObject.activeSelf)
        {
            _waveDebugLabel.text = "Wave debug overlay inactive outside combat.";
            return;
        }

        int currentWave = _activeWaveManager?.CurrentWave ?? 0;
        string totalWaveText = _totalWavesForMap > 0 ? _totalWavesForMap.ToString() : "∞";
        int monsterCount = _activeSpawner?.ActiveMonsterCount ?? 0;

        MonsterSpawnContext context = (_activeScalingConfig != null && currentWave > 0)
            ? _activeScalingConfig.GetContextForWave(currentWave)
            : MonsterSpawnContext.Default;

        _waveDebugLabel.text = "Wave Debug" +
            $"\nWave: {currentWave}/{totalWaveText}" +
            $"\nMonsters active: {monsterCount}" +
            "\nDifficulty multipliers:" +
            $"\n • Move Speed x{context.MoveSpeedMultiplier:0.00}" +
            $"\n • Contact Damage x{context.ContactDamageMultiplier:0.00}" +
            $"\n • Max Health x{context.MaxHealthMultiplier:0.00}" +
            $"\n • Armor x{context.ArmorMultiplier:0.00}" +
            $"\n • Dodge Chance x{context.DodgeChanceMultiplier:0.00}" +
            $"\n • XP Reward x{context.XpRewardMultiplier:0.00}";
    }
}
