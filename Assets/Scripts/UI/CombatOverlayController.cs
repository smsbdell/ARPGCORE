using UnityEngine;
using UnityEngine.UI;

public class CombatOverlayController : MonoBehaviour
{
    [SerializeField] private SceneFlowController _sceneFlowController;
    [SerializeField] private bool _showWaveDebugOverlay = true;

    private Canvas _combatCanvas;
    private Text _combatSummary;
    private Text _waveDebugLabel;
    private SceneFlowController.CombatRunContext? _currentContext;
    private float _pendingCountdown;

    private void Awake()
    {
        if (_sceneFlowController == null)
            _sceneFlowController = SceneFlowController.Instance;

        BuildCombatOverlay();
    }

    private void OnEnable()
    {
        SubscribeToFlow();
        RefreshWithCurrentRun();
    }

    private void OnDisable()
    {
        UnsubscribeFromFlow();
        DetachContext();
    }

    private void SubscribeToFlow()
    {
        if (_sceneFlowController == null)
            return;

        _sceneFlowController.OnCombatRunPrepared += HandleCombatRunPrepared;
    }

    private void UnsubscribeFromFlow()
    {
        if (_sceneFlowController == null)
            return;

        _sceneFlowController.OnCombatRunPrepared -= HandleCombatRunPrepared;
    }

    private void RefreshWithCurrentRun()
    {
        if (_sceneFlowController != null && _sceneFlowController.TryGetActiveRunContext(out SceneFlowController.CombatRunContext context))
        {
            HandleCombatRunPrepared(context);
            return;
        }

        ToggleOverlay(false);
    }

    private void HandleCombatRunPrepared(SceneFlowController.CombatRunContext context)
    {
        DetachContext();
        _pendingCountdown = 0f;
        _currentContext = context;
        AttachContextListeners(context);

        UpdateCombatOverlay(context.Selection);
        UpdateWaveDebugLabel();
        ToggleOverlay(true);
    }

    private void AttachContextListeners(SceneFlowController.CombatRunContext context)
    {
        if (context.WaveManager != null)
        {
            context.WaveManager.OnWaveStarted += HandleWaveStarted;
            context.WaveManager.OnInterWaveCountdown += HandleInterWaveCountdown;
        }

        if (context.Spawner != null)
        {
            context.Spawner.OnActiveMonsterCountChanged += HandleMonsterCountChanged;
        }
    }

    private void DetachContext()
    {
        if (_currentContext.HasValue)
        {
            SceneFlowController.CombatRunContext context = _currentContext.Value;
            if (context.WaveManager != null)
            {
                context.WaveManager.OnWaveStarted -= HandleWaveStarted;
                context.WaveManager.OnInterWaveCountdown -= HandleInterWaveCountdown;
            }

            if (context.Spawner != null)
            {
                context.Spawner.OnActiveMonsterCountChanged -= HandleMonsterCountChanged;
            }
        }

        _currentContext = null;
        ToggleOverlay(false);
    }

    private void HandleWaveStarted(int waveIndex)
    {
        _pendingCountdown = 0f;
        UpdateWaveDebugLabel();
    }

    private void HandleInterWaveCountdown(int nextWaveIndex, float remainingSeconds)
    {
        _pendingCountdown = remainingSeconds;
        UpdateWaveDebugLabel();
    }

    private void HandleMonsterCountChanged(int activeCount)
    {
        UpdateWaveDebugLabel();
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

    private void UpdateCombatOverlay(SceneFlowController.RunSelection selection)
    {
        if (_combatSummary == null)
            return;

        string waveName = selection.WaveMode?.displayName ?? "Unknown";
        string areaName = selection.Area?.displayName ?? "Unknown";
        _combatSummary.text = $"Running {waveName}\nArea: {areaName}\nUse the button below to return when finished.";
    }

    private void UpdateWaveDebugLabel()
    {
        if (!_showWaveDebugOverlay || _waveDebugLabel == null)
            return;

        if (_currentContext == null || _combatCanvas == null || !_combatCanvas.gameObject.activeSelf)
        {
            _waveDebugLabel.text = "Wave debug overlay inactive outside combat.";
            return;
        }

        SceneFlowController.CombatRunContext context = _currentContext.Value;
        int currentWave = context.WaveManager?.CurrentWave ?? 0;
        string totalWaveText = context.TotalWaves > 0 ? context.TotalWaves.ToString() : "∞";
        int monsterCount = context.Spawner?.ActiveMonsterCount ?? 0;

        MonsterSpawnContext spawnContext = (context.ScalingConfig != null && currentWave > 0)
            ? context.ScalingConfig.GetContextForWave(currentWave)
            : MonsterSpawnContext.Default;

        string countdownSuffix = _pendingCountdown > 0f
            ? $"\nNext wave in: {_pendingCountdown:0.0}s"
            : string.Empty;

        _waveDebugLabel.text = "Wave Debug" +
            $"\nWave: {currentWave}/{totalWaveText}" +
            $"\nMonsters active: {monsterCount}" +
            countdownSuffix +
            "\nDifficulty multipliers:" +
            $"\n • Move Speed x{spawnContext.MoveSpeedMultiplier:0.00}" +
            $"\n • Contact Damage x{spawnContext.ContactDamageMultiplier:0.00}" +
            $"\n • Max Health x{spawnContext.MaxHealthMultiplier:0.00}" +
            $"\n • Armor x{spawnContext.ArmorMultiplier:0.00}" +
            $"\n • Dodge Chance x{spawnContext.DodgeChanceMultiplier:0.00}" +
            $"\n • XP Reward x{spawnContext.XpRewardMultiplier:0.00}";
    }

    private void ToggleOverlay(bool isVisible)
    {
        if (_combatCanvas != null)
            _combatCanvas.gameObject.SetActive(isVisible);
    }

    private void ReturnToBaseCamp()
    {
        _sceneFlowController?.ReturnToBaseCamp();
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
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image image = panelObj.GetComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        return rect;
    }

    private Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Text uiText = textObj.GetComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        uiText.fontSize = fontSize;
        uiText.alignment = alignment;
        uiText.color = Color.white;

        return uiText;
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
}
