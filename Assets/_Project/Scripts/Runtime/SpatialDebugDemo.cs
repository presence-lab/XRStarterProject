using UnityEngine;

/// <summary>
/// A live reference for the SpatialDebug API. This scene is normally launched from the XR Demo Hub,
/// but it also supplies a basic camera when opened on its own in the Editor.
/// </summary>
public sealed class SpatialDebugDemo : MonoBehaviour
{
    private readonly Color rayColor = new(0.15f, 0.85f, 1f);
    private readonly Color boundsColor = new(1f, 0.7f, 0.12f);
    private Transform target;
    private Collider targetCollider;
    private Vector3 origin;

    private void Start()
    {
        EnsureFallbackView();
        CreateEnvironment();

        origin = new Vector3(0f, 1.15f, 0f);
        target = CreateTarget();
        targetCollider = target.GetComponent<Collider>();
    }

    private void Update()
    {
        if (target == null)
            return;

        var time = Time.time;
        target.position = new Vector3(Mathf.Sin(time * 0.8f) * 0.75f, 1.25f + Mathf.Sin(time * 1.6f) * 0.16f, 2.4f + Mathf.Cos(time * 0.8f) * 0.25f);
        target.rotation = Quaternion.Euler(20f, time * 65f, 0f);

        var targetPosition = target.position;
        var toTarget = targetPosition - origin;

        SpatialDebug.Point(origin, Color.white, 0.035f);
        SpatialDebug.Label(origin + Vector3.up * 0.12f, "ray origin", Color.white, size: 0.028f);
        SpatialDebug.Ray(origin, toTarget, rayColor, width: 0.012f);
        SpatialDebug.Point(targetPosition, rayColor, 0.04f);
        SpatialDebug.Label(targetPosition + Vector3.up * 0.28f, "moving target", rayColor, size: 0.032f);
        SpatialDebug.Bounds(targetCollider.bounds, boundsColor, width: 0.009f);
        SpatialDebug.Label(targetCollider.bounds.max + new Vector3(0f, 0.12f, 0f), "Collider bounds", boundsColor, size: 0.026f);
        SpatialDebug.Axes(targetPosition, target.rotation, 0.22f, width: 0.012f);

        DrawReferenceBoard();
    }

    private void DrawReferenceBoard()
    {
        var board = new Vector3(-1.25f, 1.65f, 2.65f);
        var boardSize = new Vector3(0.95f, 0.7f, 0.03f);
        SpatialDebug.Bounds(new Bounds(board, boardSize), new Color(0.3f, 0.43f, 0.5f), width: 0.006f);
        SpatialDebug.Label(board + Vector3.up * 0.23f, "SPATIAL DEBUG", Color.white, size: 0.042f);
        SpatialDebug.Label(board + Vector3.up * 0.08f, "Cyan: Ray + Point", rayColor, size: 0.025f);
        SpatialDebug.Label(board - Vector3.up * 0.06f, "Amber: Collider Bounds", boundsColor, size: 0.025f);
        SpatialDebug.Label(board - Vector3.up * 0.2f, "RGB: Transform Axes", Color.white, size: 0.025f);
    }

    private static Transform CreateTarget()
    {
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "Moving Debug Target";
        target.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);
        return target.transform;
    }

    private static void CreateEnvironment()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Debug Floor";
        floor.transform.localScale = new Vector3(0.5f, 1f, 0.5f);

        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Spatial Debug Reference Board";
        board.transform.position = new Vector3(-1.25f, 1.65f, 2.7f);
        board.transform.localScale = new Vector3(1.1f, 0.85f, 0.04f);
        board.GetComponent<Renderer>().material.color = new Color(0.035f, 0.08f, 0.11f);

        if (FindAnyObjectByType<Light>() != null)
            return;

        var lightObject = new GameObject("Debug Directional Light", typeof(Light));
        var light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
    }

    private static void EnsureFallbackView()
    {
        if (Camera.main != null || FindAnyObjectByType<Camera>() != null)
            return;

        var cameraObject = new GameObject("Debug Demo Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 1.6f, -1.6f);
        cameraObject.transform.LookAt(new Vector3(0f, 1.2f, 2f));
    }
}
