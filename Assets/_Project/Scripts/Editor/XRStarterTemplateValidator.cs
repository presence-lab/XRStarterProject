using System.IO;
using UnityEditor;
using UnityEngine;

namespace XRStarterProject.Editor
{
    /// <summary>
    /// Keeps the starter template honest about package/sample compatibility without
    /// modifying imported sample assets that may be referenced by the starter scene.
    /// </summary>
    internal static class XRStarterTemplateValidator
    {
        private const string XriPackageVersion = "3.5.1";
        private const string XriSampleRoot = "Assets/Samples/XR Interaction Toolkit/3.5.1";
        private const string SimulatorPrefabPath = XriSampleRoot + "/XR Interaction Simulator/XR Interaction Simulator.prefab";

        public static bool HasSimulator => AssetDatabase.LoadAssetAtPath<Object>(SimulatorPrefabPath) != null;

        public static bool HasVersionMismatch => !Directory.Exists(XriSampleRoot);

        public static string CreateReport()
        {
            var report = "[XR Starter] Template validation\n" +
                         $"Unity: {Application.unityVersion}\n" +
                         $"XR Interaction Toolkit package: {XriPackageVersion}\n" +
                         $"Sample scene: {(File.Exists("Assets/_Project/Scenes/SampleScene.unity") ? "found" : "MISSING")}\n" +
                         $"XR Interaction Simulator: {(HasSimulator ? "found" : "MISSING")}\n";

            if (HasVersionMismatch)
            {
                report += "The matching XR Interaction Toolkit sample folder was not found. Open Window > Package Manager " +
                          "and import the XR Interaction Toolkit 3.5.1 samples.";
            }
            else
            {
                report += "Matching XR Interaction Toolkit 3.5.1 sample folders were found. Hand-tracking demos are intentionally omitted.";
            }

            return report;
        }
    }
}
