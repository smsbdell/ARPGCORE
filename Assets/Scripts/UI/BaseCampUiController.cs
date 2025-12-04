using UnityEngine;
using UnityEngine.UI;

public class BaseCampUiController : MonoBehaviour
{
    [SerializeField] private SceneFlowController _sceneFlowController;

    private Canvas _baseCampCanvas;
    private Text _waveModeLabel;
    private Text _areaLabel;
    private Text _detailsLabel;
    private Text _lootDetailsLabel;
    private Text _skillDetailsLabel;
    private Button _startButton;

    private void Awake()
    {
        if (_sceneFlowController == null)
            _sceneFlowController = SceneFlowController.Instance;

        BuildBaseCampUi();
    }

    private void OnEnable()
    {
        SubscribeToFlow();
        UpdateBaseCampUi();
    }

    private void OnDisable()
    {
        UnsubscribeFromFlow();
    }

    private void SubscribeToFlow()
    {
        if (_sceneFlowController == null)
            return;

        _sceneFlowController.OnSelectionChanged += HandleSelectionChanged;
        _sceneFlowController.OnRunLoadingStateChanged += HandleRunLoadingStateChanged;
    }

    private void UnsubscribeFromFlow()
    {
        if (_sceneFlowController == null)
            return;

        _sceneFlowController.OnSelectionChanged -= HandleSelectionChanged;
        _sceneFlowController.OnRunLoadingStateChanged -= HandleRunLoadingStateChanged;
    }

    private void HandleSelectionChanged(SceneFlowController.RunSelection selection)
    {
        UpdateBaseCampUi();
    }

    private void HandleRunLoadingStateChanged(bool isLoading)
    {
        if (_startButton != null)
            _startButton.interactable = !isLoading;
    }

    private void BuildBaseCampUi()
    {
        if (_baseCampCanvas != null || _sceneFlowController == null)
            return;

        _baseCampCanvas = CreateCanvas("BaseCampCanvas");
        _baseCampCanvas.gameObject.SetActive(true);

        RectTransform panel = CreatePanel(_baseCampCanvas.transform, new Vector2(1080, 720));
        _detailsLabel = CreateText(panel, "Details", string.Empty, 16, TextAnchor.UpperLeft, new Vector2(-500f, 180f), new Vector2(1000f, 120f));

        CreateText(panel, "Title", "Base Camp", 24, TextAnchor.UpperLeft, new Vector2(-500f, 300f), new Vector2(300f, 40f));

        _waveModeLabel = CreateText(panel, "WaveLabel", string.Empty, 18, TextAnchor.MiddleLeft, new Vector2(-240f, 110f), new Vector2(480f, 32f));
        CreateButton(panel, "PrevWave", "<", new Vector2(-500f, 110f), new Vector2(50f, 32f), () => _sceneFlowController.GoToPreviousWaveMode());
        CreateButton(panel, "NextWave", ">", new Vector2(500f - 50f, 110f), new Vector2(50f, 32f), () => _sceneFlowController.GoToNextWaveMode());

        _areaLabel = CreateText(panel, "AreaLabel", string.Empty, 18, TextAnchor.MiddleLeft, new Vector2(-240f, 60f), new Vector2(480f, 32f));
        CreateButton(panel, "PrevArea", "<", new Vector2(-500f, 60f), new Vector2(50f, 32f), () => _sceneFlowController.GoToPreviousArea());
        CreateButton(panel, "NextArea", ">", new Vector2(500f - 50f, 60f), new Vector2(50f, 32f), () => _sceneFlowController.GoToNextArea());

        _startButton = CreateButton(panel, "StartCombat", "Enter Combat", new Vector2(0f, -10f), new Vector2(240f, 50f), StartCombatRun);

        Button lootButton = CreateButton(panel, "LootButton", "Inspect Loot", new Vector2(-200f, -120f), new Vector2(200f, 40f), ShowLootPreview);
        lootButton.GetComponent<Image>().color = new Color(0.24f, 0.35f, 0.25f, 0.9f);
        _lootDetailsLabel = CreateText(panel, "LootDetails", "Select 'Inspect Loot' to preview upcoming treasure.", 14, TextAnchor.UpperLeft, new Vector2(-500f, -190f), new Vector2(480f, 180f));

        Button skillButton = CreateButton(panel, "SkillButton", "Manage Skill Trees", new Vector2(200f, -120f), new Vector2(200f, 40f), ShowSkillTreePreview);
        skillButton.GetComponent<Image>().color = new Color(0.25f, 0.32f, 0.42f, 0.9f);
        _skillDetailsLabel = CreateText(panel, "SkillDetails", "Select 'Manage Skill Trees' to plan upgrades.", 14, TextAnchor.UpperLeft, new Vector2(20f, -190f), new Vector2(480f, 180f));
    }

    private void UpdateBaseCampUi()
    {
        if (_sceneFlowController == null)
            return;

        SceneFlowController.RunSelection selection = _sceneFlowController.BuildSelection();

        if (selection.WaveMode != null && _waveModeLabel != null)
        {
            _waveModeLabel.text = $"Wave Mode: {selection.WaveMode.displayName}";
            if (_detailsLabel != null)
                _detailsLabel.text = selection.WaveMode.description;
        }

        if (selection.Area != null && _areaLabel != null)
        {
            _areaLabel.text = $"Area: {selection.Area.displayName}";
            if (_detailsLabel != null)
                _detailsLabel.text += $"\n\nArea Notes: {selection.Area.description}";
        }

        if (_startButton != null)
        {
            _startButton.interactable = !_sceneFlowController.IsCombatLoading;
        }
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
        _sceneFlowController?.StartCombatRun();
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
