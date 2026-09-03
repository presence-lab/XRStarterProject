using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace XRStarterProject.Editor
{
    /// <summary>
    /// A small, reopenable first-run guide for students. It deliberately points students
    /// toward the starter scene and simulator without editing their project for them.
    /// </summary>
    public sealed class XRStarterOnboarding : EditorWindow
    {
        private const string SeenKey = "XRStarterProject.OnboardingSeen.v1";
        private const string SampleScenePath = "Assets/_Project/Scenes/SampleScene.unity";
        private static readonly Color LabBlue = new(0.18f, 0.62f, 0.78f);
        private static readonly Color ReadyGreen = new(0.32f, 0.72f, 0.53f);
        private static readonly Color AttentionAmber = new(0.92f, 0.63f, 0.22f);

        [MenuItem("XR Starter/Start Here")]
        public static void ShowWindow()
        {
            var window = GetWindow<XRStarterOnboarding>("XR Starter: Start Here");
            window.minSize = new Vector2(520f, 470f);
            window.Show();
        }

        [MenuItem("XR Starter/Validate Template Setup")]
        public static void ValidateTemplateSetup()
        {
            var report = XRStarterTemplateValidator.CreateReport();
            Debug.Log(report);
            EditorUtility.DisplayDialog(
                "XR Starter Validation",
                "The detailed report was written to the Console.\n\n" +
                (XRStarterTemplateValidator.HasVersionMismatch
                    ? "Package samples need attention. Import matching samples from Package Manager before using them as reference."
                    : "The starter scene and installed XR package versions are aligned."),
                "OK");
        }

        internal static void OpenOnFirstProjectUse()
        {
            if (EditorPrefs.GetBool(SeenKey, false) || Application.isBatchMode)
                return;

            ShowWindow();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(8f);

            DrawStep("01", "Open the lab scene", "Use SampleScene to verify that XR is working before you begin.", OpenSampleScene, "Open SampleScene");
            DrawStep("02", "Test without a headset", "Press Play and use the XR Interaction Simulator to check movement and interactions.", SelectSimulator, "Find Simulator");
            DrawStep("03", "Make your own scene", "Save a copy under Assets/_Project/Scenes, then add it to Build Profiles when it is ready.", OpenBuildProfiles, "Open Build Profiles");

            EditorGUILayout.Space(10f);
            DrawStatus();

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Read student guide"))
                    OpenReadme();

                if (GUILayout.Button("Validate template setup"))
                    ValidateTemplateSetup();

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Don't show on startup"))
                {
                    EditorPrefs.SetBool(SeenKey, true);
                    Close();
                }
            }
        }

        private static void DrawHeader()
        {
            var header = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 22,
                normal = { textColor = LabBlue },
                margin = new RectOffset(10, 10, 12, 4)
            };
            EditorGUILayout.LabelField("XR Starter Lab Bench", header);
            EditorGUILayout.LabelField("Three checks before you build: scene, simulator, headset.", EditorStyles.wordWrappedMiniLabel);
        }

        private static void DrawStep(string number, string title, string description, Action action, string buttonLabel)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var numberStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, normal = { textColor = LabBlue } };
                    GUILayout.Label(number, numberStyle, GUILayout.Width(34f));
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
                    }
                    if (GUILayout.Button(buttonLabel, GUILayout.Width(132f), GUILayout.Height(30f)))
                        action();
                }
            }
        }

        private static void DrawStatus()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Bench check", EditorStyles.boldLabel);
                DrawStatusLine(File.Exists(SampleScenePath), "Starter scene is available", "Starter scene is missing");
                DrawStatusLine(XRStarterTemplateValidator.HasSimulator, "XR Interaction Simulator is installed", "XR Interaction Simulator was not found");
                DrawStatusLine(!XRStarterTemplateValidator.HasVersionMismatch, "XR package samples are version-aligned", "Imported XR samples are older than the installed packages");
            }
        }

        private static void DrawStatusLine(bool ready, string readyText, string attentionText)
        {
            var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = ready ? ReadyGreen : AttentionAmber } };
            EditorGUILayout.LabelField((ready ? "● " : "▲ ") + (ready ? readyText : attentionText), style);
        }

        private static void OpenSampleScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(SampleScenePath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath);
            Debug.Log($"[XR Starter] Opened {scene.path}. Save a copy before making assignment changes.");
        }

        private static void SelectSimulator()
        {
            var simulator = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                "Assets/Samples/XR Interaction Toolkit/3.3.1/XR Interaction Simulator/XR Interaction Simulator.prefab");
            if (simulator != null)
            {
                Selection.activeObject = simulator;
                EditorGUIUtility.PingObject(simulator);
                return;
            }

            EditorUtility.DisplayDialog("XR Interaction Simulator", "The simulator prefab was not found. Use XR Starter > Validate Template Setup for details.", "OK");
        }

        private static void OpenBuildProfiles() => SettingsService.OpenProjectSettings("Project/EditorBuildSettings");

        private static void OpenReadme()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (projectRoot == null)
                return;

            var readmePath = Path.Combine(projectRoot, "README.md");
            if (File.Exists(readmePath))
                InternalEditorUtility.OpenFileAtLineExternal(readmePath, 1);
        }
    }

    [InitializeOnLoad]
    internal static class XRStarterOnboardingLauncher
    {
        static XRStarterOnboardingLauncher() => EditorApplication.delayCall += XRStarterOnboarding.OpenOnFirstProjectUse;
    }
}
