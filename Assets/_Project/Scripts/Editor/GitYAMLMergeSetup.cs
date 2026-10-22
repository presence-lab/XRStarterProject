using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace XRStarterProject.Editor
{
    /// <summary>
    /// Checks on Unity startup that git's "unityyamlmerge" driver (declared in
    /// .gitattributes) points at a UnityYAMLMerge that actually exists, and
    /// offers to configure it if not.
    ///
    /// The driver lives in the LOCAL git config (.git/config), which a clone
    /// does not carry, so every machine has to set it once. A driver that is set
    /// but names a missing program (a moved or upgraded Editor, or an older
    /// broken setup) counts as NOT configured, so it gets repaired instead of
    /// failing at the first merge with "cannot exec" / "not recognized".
    /// </summary>
    [InitializeOnLoad]
    public static class GitYAMLMergeSetup
    {
        private const string SkipCheckKey = "GitYAMLMergeSetup_SkipCheck";
        private const string LastCheckKey = "GitYAMLMergeSetup_LastCheck";
        private const int CheckIntervalDays = 7; // Re-check every 7 days even if skipped

        // Older versions of this template shipped a .gitconfig with a driver
        // that could never run, and told students to include it. The file is
        // gone; any include of it is removed when we configure.
        private const string LegacyIncludePath = "../.gitconfig";

        static GitYAMLMergeSetup()
        {
            // Delay the check to avoid blocking editor startup
            EditorApplication.delayCall += CheckGitMergeConfiguration;
        }

        private static void CheckGitMergeConfiguration()
        {
            if (!IsGitRepository())
                return;

            if (ShouldSkipCheck())
                return;

            if (IsYAMLMergeConfigured())
                return;

            ShowSetupDialog();
        }

        private static bool IsGitRepository()
        {
            string projectPath = GetProjectPath();
            if (string.IsNullOrEmpty(projectPath))
                return false;

            string gitPath = Path.Combine(projectPath, ".git");
            return Directory.Exists(gitPath) || File.Exists(gitPath); // .git can be a file for worktrees
        }

        private static string GetProjectPath()
        {
            return Directory.GetParent(Application.dataPath)?.FullName;
        }

        private static bool ShouldSkipCheck()
        {
            if (!EditorPrefs.GetBool(SkipCheckKey, false))
                return false;

            string lastCheckStr = EditorPrefs.GetString(LastCheckKey, "");
            if (DateTime.TryParse(lastCheckStr, out DateTime lastCheck))
            {
                if ((DateTime.Now - lastCheck).TotalDays < CheckIntervalDays)
                    return true;
            }

            EditorPrefs.SetBool(SkipCheckKey, false);
            return false;
        }

        private static string GetConfiguredDriver()
        {
            try
            {
                return RunGit("config", "--get", "merge.unityyamlmerge.driver");
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Configured means: a driver is set AND the program it names exists.
        /// </summary>
        private static bool IsYAMLMergeConfigured()
        {
            string driver = GetConfiguredDriver();
            if (string.IsNullOrWhiteSpace(driver))
                return false;

            string program = DriverProgram(driver);
            return !string.IsNullOrEmpty(program) && File.Exists(program);
        }

        /// <summary>
        /// The program a driver command runs: the first word, honouring the
        /// single or double quotes git's shell would strip.
        /// </summary>
        private static string DriverProgram(string driver)
        {
            driver = driver.Trim();
            if (driver.Length == 0)
                return null;

            char first = driver[0];
            if (first == '\'' || first == '"')
            {
                int end = driver.IndexOf(first, 1);
                return end > 1 ? driver.Substring(1, end - 1) : null;
            }

            int space = driver.IndexOf(' ');
            return space < 0 ? driver : driver.Substring(0, space);
        }

        private static void ShowSetupDialog()
        {
            string current = GetConfiguredDriver();
            string problem = string.IsNullOrWhiteSpace(current)
                ? "The UnityYAMLMerge tool is not configured for this repository."
                : "The UnityYAMLMerge tool is configured for this repository, but the program it points at " +
                  "does not exist, so merges of scenes and prefabs will fail:\n\n" + current;

            int choice = EditorUtility.DisplayDialogComplex(
                "Git YAML Merge Not Configured",
                problem + "\n\nThis tool helps resolve merge conflicts in Unity files (scenes, prefabs, etc.).\n\n" +
                "Would you like to configure it now?",
                "Configure Automatically",
                "Skip for Now",
                "Don't Ask Again (7 days)"
            );

            switch (choice)
            {
                case 0: // Configure
                    ConfigureYAMLMerge();
                    break;
                case 1: // Skip: ask again next startup
                    break;
                case 2: // Don't ask again
                    EditorPrefs.SetBool(SkipCheckKey, true);
                    EditorPrefs.SetString(LastCheckKey, DateTime.Now.ToString("o"));
                    break;
            }
        }

        private static void ConfigureYAMLMerge()
        {
            string yamlMergePath = GetYAMLMergePath();
            if (string.IsNullOrEmpty(yamlMergePath))
            {
                EditorUtility.DisplayDialog(
                    "Configuration Failed",
                    "Could not find the UnityYAMLMerge tool inside this Unity Editor install:\n\n" +
                    EditorApplication.applicationContentsPath + "\n\n" +
                    "Reinstall this Editor version from Unity Hub, then use Tools > Git > Configure YAML Merge Tool.",
                    "OK"
                );
                return;
            }

            string driverCommand = BuildDriverCommand(yamlMergePath);
            try
            {
                RemoveLegacyInclude();
                RunGit("config", "--local", "--replace-all", "merge.unityyamlmerge.name", "Unity YAML Merge");
                RunGit("config", "--local", "--replace-all", "merge.unityyamlmerge.driver", driverCommand);
                RunGit("config", "--local", "--replace-all", "merge.unityyamlmerge.recursive", "binary");

                if (!IsYAMLMergeConfigured())
                    throw new Exception("git saved the driver as: " + GetConfiguredDriver());

                EditorUtility.DisplayDialog(
                    "Configuration Successful",
                    "UnityYAMLMerge has been configured for this repository.\n\n" +
                    $"Tool path: {yamlMergePath}",
                    "OK"
                );
                Debug.Log($"[GitYAMLMergeSetup] Configured UnityYAMLMerge: {driverCommand}");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "Configuration Failed",
                    $"Failed to configure UnityYAMLMerge:\n{ex.Message}\n\n" +
                    "To configure it by hand, run this from the project folder:\n\n" +
                    $"git config merge.unityyamlmerge.driver \"{driverCommand}\"",
                    "OK"
                );
                Debug.LogError($"[GitYAMLMergeSetup] Failed to configure: {ex}");
            }
        }

        /// <summary>
        /// git runs a merge driver through a POSIX shell (Git for Windows
        /// bundles one), so the path is single-quoted: that survives spaces such
        /// as "C:/Program Files". Forward slashes keep Windows paths free of
        /// backslash escapes; a single quote in the path is closed, escaped and
        /// reopened the usual shell way.
        /// </summary>
        private static string BuildDriverCommand(string yamlMergePath)
        {
            string path = yamlMergePath.Replace('\\', '/').Replace("'", "'\\''");
            return $"'{path}' merge -p %O %B %A %A";
        }

        private static void RemoveLegacyInclude()
        {
            try
            {
                foreach (string line in RunGit("config", "--local", "--get-all", "include.path")
                             .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.Trim() == LegacyIncludePath)
                    {
                        RunGit("config", "--local", "--fixed-value", "--unset-all", "include.path", LegacyIncludePath);
                        Debug.Log("[GitYAMLMergeSetup] Removed the obsolete include of ../.gitconfig");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Harmless if it stays: the file it points at no longer exists.
                Debug.LogWarning($"[GitYAMLMergeSetup] Could not remove the old ../.gitconfig include: {ex.Message}");
            }
        }

        /// <summary>
        /// UnityYAMLMerge relative to the Editor's contents folder, which Unity
        /// reports directly: Unity.app/Contents on macOS, Editor/Data on
        /// Windows and Linux. Unity 6.5 ships it in Helpers on macOS; Tools is
        /// where older Editors and the other platforms keep it.
        /// </summary>
        private static string GetYAMLMergePath()
        {
            string contents = EditorApplication.applicationContentsPath;
            if (string.IsNullOrEmpty(contents))
                return null;

            string[] candidates = Application.platform == RuntimePlatform.WindowsEditor
                ? new[] { Path.Combine(contents, "Tools", "UnityYAMLMerge.exe") }
                : new[]
                {
                    Path.Combine(contents, "Helpers", "UnityYAMLMerge"),
                    Path.Combine(contents, "Tools", "UnityYAMLMerge"),
                };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
            }
            return null;
        }

        /// <summary>
        /// Runs git with each argument passed intact. Arguments are joined into
        /// one command line, so each is wrapped in double quotes; the values
        /// here never contain double quotes or backslashes (driver paths use
        /// forward slashes), which keeps that safe under the Windows and the
        /// Mono/.NET Unix command-line parsers alike.
        /// </summary>
        private static string RunGit(params string[] args)
        {
            var quoted = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].IndexOf('"') >= 0 || args[i].IndexOf('\\') >= 0)
                    throw new ArgumentException($"git argument cannot contain a double quote or backslash: {args[i]}");
                quoted[i] = "\"" + args[i] + "\"";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = string.Join(" ", quoted),
                WorkingDirectory = GetProjectPath(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    // Exit code 1 with no error is git config's "key not set"
                    if (process.ExitCode == 1 && string.IsNullOrWhiteSpace(error))
                        return "";

                    throw new Exception($"git {string.Join(" ", args)} failed: {error.Trim()}");
                }

                return output.Trim();
            }
        }

        [MenuItem("Tools/Git/Configure YAML Merge Tool")]
        public static void ManualConfigure()
        {
            EditorPrefs.SetBool(SkipCheckKey, false);

            if (IsYAMLMergeConfigured())
            {
                bool reconfigure = EditorUtility.DisplayDialog(
                    "Already Configured",
                    "UnityYAMLMerge is already configured for this repository:\n\n" +
                    GetConfiguredDriver() + "\n\nWould you like to reconfigure it?",
                    "Reconfigure",
                    "Cancel"
                );

                if (reconfigure)
                    ConfigureYAMLMerge();
            }
            else
            {
                ShowSetupDialog();
            }
        }

        [MenuItem("Tools/Git/Check YAML Merge Status")]
        public static void CheckStatus()
        {
            string driver = GetConfiguredDriver();
            string message;
            if (IsYAMLMergeConfigured())
                message = $"UnityYAMLMerge is configured and the tool exists.\n\nDriver: {driver}";
            else if (string.IsNullOrWhiteSpace(driver))
                message = "UnityYAMLMerge is NOT configured.\n\nUse 'Tools > Git > Configure YAML Merge Tool' to set it up.";
            else
                message = "UnityYAMLMerge is configured, but the program it names does not exist, " +
                          $"so merges will fail.\n\nDriver: {driver}\n\n" +
                          "Use 'Tools > Git > Configure YAML Merge Tool' to repair it.";

            EditorUtility.DisplayDialog("YAML Merge Status", message, "OK");
        }
    }
}
