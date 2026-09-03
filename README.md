# XR Starter Project

This is the starting point for XR course projects built with Unity 6.5 and the XR Interaction Toolkit.

## Start here

1. Open the project with **Unity 6.5** (`6000.5.7f1`).
2. In Unity, open **XR Starter > Start Here**.
3. Open `Assets/_Project/Scenes/SampleScene.unity` and save your own copy before making changes.
4. Press Play to use the XR Interaction Simulator before building to a headset.
5. Put your work in `Assets/_Project`. Leave `Assets/Samples`, `Assets/ThirdParty`, and XR settings unchanged unless an assignment specifically says otherwise.

## Student workflow

Use the included sample scene to verify that the XR Origin, input actions, and simulator work. Build your assignment in a separate scene under `Assets/_Project/Scenes` and add that scene to **File > Build Profiles** when you are ready to test on device.

The `Assets/_Project` folders are intentionally organized by the kinds of assets you will make:

- `Art`, `Audio`, and `UI` for project content
- `Prefabs` for reusable objects
- `Scenes` for assignment scenes
- `Scripts/Runtime` for code that ships with the experience
- `Scripts/Editor` for Unity-only tools

## XR package samples

This template uses XR Interaction Toolkit **3.5.1**. Matching reference samples are available in `Assets/Samples/XR Interaction Toolkit/3.5.1`.

The template intentionally omits hand-tracking demos. Use the included controller and simulator samples for reference, and do not overwrite the starter scene.

## Before submitting

- Your assignment scene opens without Console errors.
- You have tested it in the simulator and, when required, on the headset.
- Only your intended scene is enabled in Build Profiles.
- Your work lives in `Assets/_Project` and is saved.
