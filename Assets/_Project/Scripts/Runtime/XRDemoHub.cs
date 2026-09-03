using System.Collections;
using IngameDebugConsole;
using TMPro;
using Tayx.Graphy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Builds an in-world, controller-ray-friendly menu in the starter scene. The menu stays at a
/// comfortable room-scale position and follows the demo session between scenes so users can return.
/// </summary>
public sealed class XRDemoHub : MonoBehaviour
{
    private const string HubScenePath = "Assets/_Project/Scenes/SampleScene.unity";
    private const float CanvasScale = 0.001f;
    private const float GraphyDistance = 0.80f;
    private const float GraphyHorizontalOffset = 0.32f;
    private const float GraphyVerticalOffset = 0.20f;
    private const float GraphyPositionSmoothTime = 0.60f;
    private const float GraphyRotationSharpness = 5f;
    private const float DebugConsoleDistance = 1.15f;
    private const float DebugConsolePositionSmoothTime = 0.55f;
    private const float DebugConsoleRotationSharpness = 6f;

    private readonly Demo[] demos =
    {
        new("Starter Assets", "Locomotion, grabbing, teleportation, and controller affordances.", "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/DemoScene.unity"),
        new("World Space UI", "Direct interaction with buttons, panels, and scroll views.", "Assets/Samples/XR Interaction Toolkit/3.5.1/World Space UI/DemoScene.unity"),
        new("Spatial Keyboard", "3D text entry with the XR spatial keyboard.", "Assets/Samples/XR Interaction Toolkit/3.5.1/Spatial Keyboard/KeyboardDemo.unity"),
        new("Spatial Debug", "Live examples of rays, points, bounds, labels, and transform axes.", "Assets/_Project/Scenes/SpatialDebugDemo.unity"),
        new("Hands Interaction", "Hand-based interaction patterns and affordances.", "Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/HandsDemoScene.unity"),
        new("Hand Gestures", "Recognizing and debugging hand shapes and gestures.", "Assets/Samples/XR Hands/1.9.0/Gestures/HandGestures.unity"),
        new("Hand Capture", "Capturing and reviewing recorded hand data.", "Assets/Samples/XR Hands/1.9.0/Hand Capture/HandCapture.unity"),
        new("Hand Visualizer", "Visualizing tracked joints and hand meshes.", "Assets/Samples/XR Hands/1.9.0/HandVisualizer/HandVisualizer.unity"),
    };

    private Canvas canvas;
    private TMP_FontAsset font;
    private EventSystem persistentEventSystem;
    private Transform graphyOverlay;
    private Vector3 graphyOverlayVelocity;
    private DebugLogManager debugConsole;
    private Transform debugConsoleOverlay;
    private Vector3 debugConsoleOverlayVelocity;
    private static XRDemoHub instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        persistentEventSystem = FindAnyObjectByType<EventSystem>();
        if (persistentEventSystem != null)
            DontDestroyOnLoad(persistentEventSystem.gameObject);
    }

    private void OnEnable() => SceneManager.activeSceneChanged += OnActiveSceneChanged;

    private void OnDisable() => SceneManager.activeSceneChanged -= OnActiveSceneChanged;

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Start() => StartCoroutine(RebuildAfterSceneLoad());

    private void Update()
    {
        FollowGraphyOverlay();
        FollowDebugConsole();
    }

    private void OnActiveSceneChanged(Scene _, Scene __) => StartCoroutine(RebuildAfterSceneLoad());

    private IEnumerator RebuildAfterSceneLoad()
    {
        yield return null;

        EnsureEventSystem();
        DisableDuplicateEventSystems();
        EnsureCanvas();
        PositionCanvas();
        ConfigureGraphyOverlay();
        ConfigureDebugConsole();
        ClearCanvas();

        if (SceneManager.GetActiveScene().path == HubScenePath)
            BuildHub();
        else
            BuildReturnControl();
    }

    private void DisableDuplicateEventSystems()
    {
        if (persistentEventSystem == null)
            return;

        foreach (var eventSystem in FindObjectsByType<EventSystem>())
        {
            if (eventSystem != persistentEventSystem)
                eventSystem.enabled = false;
        }
    }

    private void EnsureEventSystem()
    {
        if (persistentEventSystem != null)
            return;

        persistentEventSystem = FindAnyObjectByType<EventSystem>();
        if (persistentEventSystem != null)
            DontDestroyOnLoad(persistentEventSystem.gameObject);
    }

    private void EnsureCanvas()
    {
        if (canvas != null)
            return;

        var canvasObject = new GameObject("XR Demo Hub Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1100f, 850f);
        rect.localScale = Vector3.one * CanvasScale;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 12f;
        font = TMP_Settings.defaultFontAsset;
    }

    private void PositionCanvas()
    {
        var viewCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        if (viewCamera == null)
            return;

        var forward = Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;

        canvas.transform.position = viewCamera.transform.position + forward * 2f;
        canvas.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    private void ConfigureGraphyOverlay()
    {
        var viewCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        var graphy = FindAnyObjectByType<GraphyManager>();
        if (viewCamera == null || graphy == null)
            return;

        graphyOverlay = graphy.transform;
        graphyOverlay.SetParent(null, true);
        graphyOverlayVelocity = Vector3.zero;
        SnapGraphyOverlay(viewCamera);

        var graphyCanvas = graphy.GetComponent<Canvas>();
        if (graphyCanvas != null)
            graphyCanvas.worldCamera = viewCamera;
    }

    private void FollowGraphyOverlay()
    {
        if (graphyOverlay == null)
            return;

        var viewCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        if (viewCamera == null)
            return;

        var targetPosition = GetGraphyTargetPosition(viewCamera);
        graphyOverlay.position = Vector3.SmoothDamp(
            graphyOverlay.position,
            targetPosition,
            ref graphyOverlayVelocity,
            GraphyPositionSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);

        var targetRotation = GetGraphyTargetRotation(viewCamera, targetPosition);
        var rotationBlend = 1f - Mathf.Exp(-GraphyRotationSharpness * Time.unscaledDeltaTime);
        graphyOverlay.rotation = Quaternion.Slerp(graphyOverlay.rotation, targetRotation, rotationBlend);
    }

    private void SnapGraphyOverlay(Camera viewCamera)
    {
        var targetPosition = GetGraphyTargetPosition(viewCamera);
        graphyOverlay.position = targetPosition;
        graphyOverlay.rotation = GetGraphyTargetRotation(viewCamera, targetPosition);
    }

    private static Vector3 GetGraphyTargetPosition(Camera viewCamera)
    {
        var forward = Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;

        var right = Vector3.Cross(Vector3.up, forward);
        return viewCamera.transform.position + forward * GraphyDistance + right * GraphyHorizontalOffset + Vector3.up * GraphyVerticalOffset;
    }

    private static Quaternion GetGraphyTargetRotation(Camera viewCamera, Vector3 targetPosition)
    {
        var facing = Vector3.ProjectOnPlane(targetPosition - viewCamera.transform.position, Vector3.up).normalized;
        return Quaternion.LookRotation(facing.sqrMagnitude < 0.01f ? Vector3.forward : facing, Vector3.up);
    }

    private void ConfigureDebugConsole()
    {
        var viewCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        debugConsole = DebugLogManager.Instance != null ? DebugLogManager.Instance : FindAnyObjectByType<DebugLogManager>();
        if (viewCamera == null || debugConsole == null)
            return;

        debugConsoleOverlay = debugConsole.transform;
        debugConsoleOverlay.SetParent(null, true);
        debugConsoleOverlayVelocity = Vector3.zero;

        var consoleCanvas = debugConsole.GetComponent<Canvas>();
        if (consoleCanvas != null)
        {
            consoleCanvas.renderMode = RenderMode.WorldSpace;
            consoleCanvas.worldCamera = viewCamera;
        }

        var screenRaycaster = debugConsole.GetComponent<GraphicRaycaster>();
        if (screenRaycaster != null)
            screenRaycaster.enabled = false;

        if (debugConsole.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            debugConsole.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        var consoleRect = debugConsole.GetComponent<RectTransform>();
        consoleRect.sizeDelta = new Vector2(1000f, 650f);
        consoleRect.localScale = Vector3.one * CanvasScale;
        debugConsole.PopupEnabled = false;
        debugConsole.HideLogWindow();
        SnapDebugConsole(viewCamera);
    }

    private void ToggleDebugConsole()
    {
        if (debugConsole == null)
            ConfigureDebugConsole();

        if (debugConsole == null)
            return;

        if (debugConsole.IsLogWindowVisible)
            debugConsole.HideLogWindow();
        else
            debugConsole.ShowLogWindow();
    }

    private void FollowDebugConsole()
    {
        if (debugConsoleOverlay == null)
            return;

        var viewCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        if (viewCamera == null)
            return;

        var targetPosition = GetDebugConsoleTargetPosition(viewCamera);
        debugConsoleOverlay.position = Vector3.SmoothDamp(
            debugConsoleOverlay.position,
            targetPosition,
            ref debugConsoleOverlayVelocity,
            DebugConsolePositionSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);

        var targetRotation = GetGraphyTargetRotation(viewCamera, targetPosition);
        var rotationBlend = 1f - Mathf.Exp(-DebugConsoleRotationSharpness * Time.unscaledDeltaTime);
        debugConsoleOverlay.rotation = Quaternion.Slerp(debugConsoleOverlay.rotation, targetRotation, rotationBlend);
    }

    private void SnapDebugConsole(Camera viewCamera)
    {
        var targetPosition = GetDebugConsoleTargetPosition(viewCamera);
        debugConsoleOverlay.position = targetPosition;
        debugConsoleOverlay.rotation = GetGraphyTargetRotation(viewCamera, targetPosition);
    }

    private static Vector3 GetDebugConsoleTargetPosition(Camera viewCamera)
    {
        var forward = Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;

        return viewCamera.transform.position + forward * DebugConsoleDistance + Vector3.up * 0.02f;
    }

    private void ClearCanvas()
    {
        for (var index = canvas.transform.childCount - 1; index >= 0; index--)
            Destroy(canvas.transform.GetChild(index).gameObject);
    }

    private void BuildHub()
    {
        var panel = CreatePanel("Demo Hub Panel", canvas.transform, new Color(0.035f, 0.08f, 0.11f, 0.96f));
        Stretch(panel.GetComponent<RectTransform>(), 20f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(34, 34, 30, 30);
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        CreateText(panel.transform, "XR DEMO HUB", 46, FontStyles.Bold, new Color(0.27f, 0.82f, 0.96f), 64f);
        CreateText(panel.transform, "Point with a controller ray and select a focused XR example. Each demo opens in its original scene; a spatial return control remains available.", 22, FontStyles.Normal, new Color(0.82f, 0.9f, 0.94f), 54f);
        var debugButton = CreateButton(panel.transform, "Open Debug Console (warnings and errors)", 52f);
        debugButton.onClick.AddListener(ToggleDebugConsole);

        var gridObject = new GameObject("Demo Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObject.transform.SetParent(panel.transform, false);
        var grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.cellSize = new Vector2(500f, 115f);
        grid.spacing = new Vector2(18f, 14f);
        grid.childAlignment = TextAnchor.UpperCenter;
        gridObject.AddComponent<LayoutElement>().preferredHeight = 510f;

        foreach (var demo in demos)
            CreateDemoButton(gridObject.transform, demo);

        CreateText(panel.transform, "Teaching tip: demo scenes are reference material. Student work should be created in Assets/_Project.", 18, FontStyles.Italic, new Color(0.62f, 0.72f, 0.77f), 28f);
    }

    private void BuildReturnControl()
    {
        var panel = CreatePanel("Return To Hub", canvas.transform, new Color(0.035f, 0.08f, 0.11f, 0.96f));
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 240f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        CreateText(panel.transform, "DEMO IN PROGRESS", 24, FontStyles.Bold, new Color(0.27f, 0.82f, 0.96f), 34f);
        var returnButton = CreateButton(panel.transform, "Return to Demo Hub", 60f);
        returnButton.onClick.AddListener(() => SceneManager.LoadScene(HubScenePath));
        var debugButton = CreateButton(panel.transform, "Open Debug Console", 52f);
        debugButton.onClick.AddListener(ToggleDebugConsole);
    }

    private void CreateDemoButton(Transform parent, Demo demo)
    {
        var button = CreateButton(parent, string.Empty, 0f);
        var image = button.GetComponent<Image>();
        image.color = new Color(0.08f, 0.18f, 0.23f, 0.98f);

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.66f, 0.92f, 1f, 1f);
        colors.pressedColor = new Color(0.42f, 0.75f, 0.9f, 1f);
        button.colors = colors;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        content.transform.SetParent(button.transform, false);
        Stretch(content.GetComponent<RectTransform>(), 18f);
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        CreateText(content.transform, demo.Title, 26, FontStyles.Bold, new Color(0.89f, 0.97f, 1f), 34f);
        CreateText(content.transform, demo.Description, 17, FontStyles.Normal, new Color(0.69f, 0.8f, 0.86f), 44f);

        button.onClick.AddListener(() => SceneManager.LoadScene(demo.ScenePath));
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private Button CreateButton(Transform parent, string label, float height)
    {
        var buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = new Color(0.1f, 0.35f, 0.44f, 1f);

        var layout = buttonObject.GetComponent<LayoutElement>();
        if (height > 0f)
            layout.preferredHeight = height;

        if (!string.IsNullOrEmpty(label))
            CreateText(buttonObject.transform, label, 22, FontStyles.Bold, Color.white, height - 10f, TextAlignmentOptions.Center);

        return buttonObject.GetComponent<Button>();
    }

    private void CreateText(Transform parent, string text, float fontSize, FontStyles fontStyle, Color color, float height, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);
        var label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = font;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        textObject.GetComponent<LayoutElement>().preferredHeight = height;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
    }

    private readonly struct Demo
    {
        public readonly string Title;
        public readonly string Description;
        public readonly string ScenePath;

        public Demo(string title, string description, string scenePath)
        {
            Title = title;
            Description = description;
            ScenePath = scenePath;
        }
    }
}
