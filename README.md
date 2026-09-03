# XR Starter Project

This is the starting point for XR course projects built with Unity 6.5 and the XR Interaction Toolkit.

## Start here

1. Open the project with **Unity 6.5** (`6000.5.7f1`).
2. In Unity, open **XR Starter > Start Here**.
3. Open `Assets/_Project/Scenes/SampleScene.unity` and press Play to use the Demo Hub. It launches the retained reference demos and provides a return button.
4. Save your own copy of the starter scene before making assignment changes, then use the XR Interaction Simulator before building to a headset.
5. Put your work in `Assets/_Project`. Leave `Assets/Samples`, `Assets/ThirdParty`, and XR settings unchanged unless an assignment specifically says otherwise.

## Student workflow

Use the included in-world Demo Hub to verify the XR Origin, input actions, and simulator, then use a controller ray to launch Starter Assets, World Space UI, Spatial Keyboard, or the hand-interaction examples. Build your assignment in a separate scene under `Assets/_Project/Scenes` and add that scene to **File > Build Profiles** when you are ready to test on device.

The `Assets/_Project` folders are intentionally organized by the kinds of assets you will make:

- `Art`, `Audio`, and `UI` for project content
- `Prefabs` for reusable objects
- `Scenes` for assignment scenes
- `Scripts/Runtime` for code that ships with the experience
- `Scripts/Editor` for Unity-only tools

## XR package samples

This template uses XR Interaction Toolkit **3.5.1**. Matching reference samples are available in `Assets/Samples/XR Interaction Toolkit/3.5.1`.

The hand-interaction examples are also included under `Assets/Samples/XR Hands/1.9.0`.

## Spatial debugging

Use `SpatialDebug` from `Assets/_Project/Scripts/Runtime/SpatialDebug.cs` to make rays, points, bounds, coordinate axes, and labels visible in the Game view or headset. It is intended for Play Mode and development builds; its calls do nothing in a non-development player build.

Launch **Spatial Debug** from the Demo Hub to see each call type animated around a moving target. The scene also opens on its own with a fallback desktop camera for quick script experimentation.

```csharp
SpatialDebug.Ray(ray.origin, ray.direction * 3f, Color.cyan, duration: 1f);
SpatialDebug.Point(hit.point, Color.green, duration: 1f);
SpatialDebug.Bounds(targetCollider.bounds, Color.yellow, duration: 1f);
SpatialDebug.Axes(transform.position, transform.rotation, duration: 1f);
SpatialDebug.Label(transform.position + Vector3.up * 0.1f, "Target", Color.white, duration: 1f);
```

Pass a positive `duration` when calling from a one-off event. The default duration is one frame, which is useful when calling it continuously from `Update`. Call `SpatialDebug.Clear()` to remove every active visual, or set `SpatialDebug.Enabled = false` to temporarily suppress new visuals.

## Before submitting

- Your assignment scene opens without Console errors.
- You have tested it in the simulator and, when required, on the headset.
- Only your intended scene is enabled in Build Profiles.
- Your work lives in `Assets/_Project` and is saved.
