using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Draws simple, world-space debug visuals in Play Mode and development builds.
/// Use this instead of Gizmos or Debug.DrawLine when students need to see spatial information
/// in the Game view or a headset. Calls are no-ops in non-development player builds.
/// </summary>
public static class SpatialDebug
{
    public const float DefaultLineWidth = 0.008f;
    public const float DefaultPointRadius = 0.025f;

    /// <summary>Turns all spatial debugging on or off without removing draw calls.</summary>
    public static bool Enabled
    {
        get => SpatialDebugRenderer.Instance != null && SpatialDebugRenderer.Instance.Enabled;
        set => SpatialDebugRenderer.GetOrCreate().Enabled = value;
    }

    /// <summary>Draws a line. A duration of zero keeps it visible for one rendered frame.</summary>
    public static void Line(Vector3 start, Vector3 end, Color color, float duration = 0f, float width = DefaultLineWidth)
    {
        if (!CanDraw())
            return;

        SpatialDebugRenderer.GetOrCreate().DrawLine(start, end, color, duration, width);
    }

    /// <summary>Draws a ray from an origin in a direction. The direction magnitude is its length.</summary>
    public static void Ray(Vector3 origin, Vector3 direction, Color color, float duration = 0f, float width = DefaultLineWidth)
    {
        Line(origin, origin + direction, color, duration, width);
    }

    /// <summary>Draws three colored axes: X red, Y green, and Z blue.</summary>
    public static void Axes(Vector3 origin, Quaternion rotation, float size = 0.15f, float duration = 0f, float width = DefaultLineWidth)
    {
        if (!CanDraw())
            return;

        Line(origin, origin + rotation * Vector3.right * size, Color.red, duration, width);
        Line(origin, origin + rotation * Vector3.up * size, Color.green, duration, width);
        Line(origin, origin + rotation * Vector3.forward * size, Color.blue, duration, width);
    }

    /// <summary>Draws a small solid point marker.</summary>
    public static void Point(Vector3 position, Color color, float radius = DefaultPointRadius, float duration = 0f)
    {
        if (!CanDraw())
            return;

        SpatialDebugRenderer.GetOrCreate().DrawPoint(position, color, radius, duration);
    }

    /// <summary>Draws the twelve edges of world-space bounds.</summary>
    public static void Bounds(Bounds bounds, Color color, float duration = 0f, float width = DefaultLineWidth)
    {
        if (!CanDraw())
            return;

        var min = bounds.min;
        var max = bounds.max;
        var a = new Vector3(min.x, min.y, min.z);
        var b = new Vector3(max.x, min.y, min.z);
        var c = new Vector3(max.x, min.y, max.z);
        var d = new Vector3(min.x, min.y, max.z);
        var e = new Vector3(min.x, max.y, min.z);
        var f = new Vector3(max.x, max.y, min.z);
        var g = new Vector3(max.x, max.y, max.z);
        var h = new Vector3(min.x, max.y, max.z);

        Line(a, b, color, duration, width); Line(b, c, color, duration, width);
        Line(c, d, color, duration, width); Line(d, a, color, duration, width);
        Line(e, f, color, duration, width); Line(f, g, color, duration, width);
        Line(g, h, color, duration, width); Line(h, e, color, duration, width);
        Line(a, e, color, duration, width); Line(b, f, color, duration, width);
        Line(c, g, color, duration, width); Line(d, h, color, duration, width);
    }

    /// <summary>Draws a camera-facing world-space label.</summary>
    public static void Label(Vector3 position, string text, Color color, float duration = 0f, float size = 0.035f)
    {
        if (!CanDraw() || string.IsNullOrWhiteSpace(text))
            return;

        SpatialDebugRenderer.GetOrCreate().DrawLabel(position, text, color, duration, size);
    }

    /// <summary>Removes all active spatial debug visuals immediately.</summary>
    public static void Clear()
    {
        if (SpatialDebugRenderer.Instance != null)
            SpatialDebugRenderer.Instance.Clear();
    }

    private static bool CanDraw()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return SpatialDebugRenderer.GetOrCreate().Enabled;
#else
        return false;
#endif
    }
}

internal sealed class SpatialDebugRenderer : MonoBehaviour
{
    private const string HostName = "[Spatial Debug]";
    private const float OneFrameLifetime = 0.02f;

    private readonly List<Visual> visuals = new();
    private readonly Stack<LineRenderer> availableLines = new();
    private readonly Stack<GameObject> availablePoints = new();
    private readonly Stack<TextMeshPro> availableLabels = new();
    private Material lineMaterial;
    private Material markerMaterial;
    private MaterialPropertyBlock markerProperties;

    internal static SpatialDebugRenderer Instance { get; private set; }

    internal bool Enabled { get; set; } = true;

    internal static SpatialDebugRenderer GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        var host = new GameObject(HostName) { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(host);
        Instance = host.AddComponent<SpatialDebugRenderer>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void LateUpdate()
    {
        var camera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        var now = Time.unscaledTime;

        for (var index = visuals.Count - 1; index >= 0; index--)
        {
            var visual = visuals[index];
            if (visual.ExpiresAt <= now)
            {
                Release(visual);
                visuals.RemoveAt(index);
                continue;
            }

            if (visual.Label != null && camera != null)
                visual.Label.transform.rotation = Quaternion.LookRotation(visual.Label.transform.position - camera.transform.position, Vector3.up);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    internal void DrawLine(Vector3 start, Vector3 end, Color color, float duration, float width)
    {
        var line = GetLine();
        var lineObject = line.gameObject;
        lineObject.SetActive(true);
        line.material = GetLineMaterial();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startColor = color;
        line.endColor = color;
        line.startWidth = Mathf.Max(0.0001f, width);
        line.endWidth = Mathf.Max(0.0001f, width);
        line.alignment = LineAlignment.View;
        line.numCapVertices = 3;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        Track(lineObject, duration, null, VisualKind.Line);
    }

    internal void DrawPoint(Vector3 position, Color color, float radius, float duration)
    {
        var pointObject = GetPoint();
        pointObject.SetActive(true);
        pointObject.transform.position = position;
        pointObject.transform.localScale = Vector3.one * Mathf.Max(0.0001f, radius * 2f);

        var renderer = pointObject.GetComponent<Renderer>();
        renderer.sharedMaterial = GetMarkerMaterial();
        markerProperties ??= new MaterialPropertyBlock();
        markerProperties.SetColor("_BaseColor", color);
        markerProperties.SetColor("_Color", color);
        renderer.SetPropertyBlock(markerProperties);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Track(pointObject, duration, null, VisualKind.Point);
    }

    internal void DrawLabel(Vector3 position, string text, Color color, float duration, float size)
    {
        var label = GetLabel();
        var labelObject = label.gameObject;
        labelObject.SetActive(true);
        labelObject.transform.position = position;

        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.color = color;
        label.fontSize = 1f;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.transform.localScale = Vector3.one * Mathf.Max(0.0001f, size);
        label.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Track(labelObject, duration, label, VisualKind.Label);
    }

    internal void Clear()
    {
        foreach (var visual in visuals)
            Release(visual);

        visuals.Clear();
    }

    private LineRenderer GetLine()
    {
        if (availableLines.Count > 0)
            return availableLines.Pop();

        var lineObject = new GameObject("Line") { hideFlags = HideFlags.HideInHierarchy };
        lineObject.transform.SetParent(transform, false);
        return lineObject.AddComponent<LineRenderer>();
    }

    private GameObject GetPoint()
    {
        if (availablePoints.Count > 0)
            return availablePoints.Pop();

        var pointObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pointObject.name = "Point";
        pointObject.hideFlags = HideFlags.HideInHierarchy;
        pointObject.transform.SetParent(transform, false);

        var collider = pointObject.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        return pointObject;
    }

    private TextMeshPro GetLabel()
    {
        if (availableLabels.Count > 0)
            return availableLabels.Pop();

        var labelObject = new GameObject("Label") { hideFlags = HideFlags.HideInHierarchy };
        labelObject.transform.SetParent(transform, false);
        return labelObject.AddComponent<TextMeshPro>();
    }

    private void Track(GameObject visual, float duration, TextMeshPro label, VisualKind kind)
    {
        visuals.Add(new Visual(visual, label, Time.unscaledTime + (duration > 0f ? duration : OneFrameLifetime), kind));
    }

    private void Release(Visual visual)
    {
        visual.GameObject.SetActive(false);

        switch (visual.Kind)
        {
            case VisualKind.Line:
                availableLines.Push(visual.GameObject.GetComponent<LineRenderer>());
                break;
            case VisualKind.Point:
                availablePoints.Push(visual.GameObject);
                break;
            case VisualKind.Label:
                availableLabels.Push(visual.Label);
                break;
        }
    }

    private Material GetLineMaterial()
    {
        if (lineMaterial != null)
            return lineMaterial;

        lineMaterial = new Material(FindUnlitShader()) { hideFlags = HideFlags.HideAndDontSave };
        return lineMaterial;
    }

    private Material GetMarkerMaterial()
    {
        if (markerMaterial != null)
            return markerMaterial;

        markerMaterial = new Material(FindUnlitShader()) { hideFlags = HideFlags.HideAndDontSave };
        return markerMaterial;
    }

    private static Shader FindUnlitShader()
    {
        return Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Sprites/Default")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Standard");
    }

    private enum VisualKind
    {
        Line,
        Point,
        Label,
    }

    private readonly struct Visual
    {
        internal readonly GameObject GameObject;
        internal readonly TextMeshPro Label;
        internal readonly float ExpiresAt;
        internal readonly VisualKind Kind;

        internal Visual(GameObject gameObject, TextMeshPro label, float expiresAt, VisualKind kind)
        {
            GameObject = gameObject;
            Label = label;
            ExpiresAt = expiresAt;
            Kind = kind;
        }
    }
}
