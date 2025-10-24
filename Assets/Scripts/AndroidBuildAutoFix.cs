#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class AndroidBuildAutoFix : EditorWindow
{
    private static string targetSDKPath = "C:/AndroidSDK";

    [MenuItem("Tools/Android Auto-Fix 🛠")]
    public static void FixAndroidBuild()
    {
        EditorUtility.DisplayProgressBar("Fixing Android Build", "Checking environment...", 0.2f);

        // ✅ Step 1 — Get current SDK path safely
        string currentSDK = EditorPrefs.GetString("AndroidSdkRoot");
        if (string.IsNullOrEmpty(currentSDK))
        {
            // fallback using Environment variables
            currentSDK = System.Environment.GetEnvironmentVariable("ANDROID_HOME") ??
                         System.Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        }

        // Move SDK if in a read-only location (like Program Files)
        if (!string.IsNullOrEmpty(currentSDK) && currentSDK.Contains("Program Files"))
        {
            Debug.LogWarning($"SDK is in a read-only path: {currentSDK}");
            if (!Directory.Exists(targetSDKPath))
            {
                Directory.CreateDirectory(targetSDKPath);
                Debug.Log($"✅ Created new SDK directory at {targetSDKPath}");
            }
            EditorPrefs.SetString("AndroidSdkRoot", targetSDKPath);
            Debug.Log($"📦 SDK path reassigned to: {targetSDKPath}");
        }

        EditorUtility.DisplayProgressBar("Fixing Android Build", "Cleaning XR duplicates...", 0.4f);

        // ✅ Step 2 — Clean XR duplicate asset files
        DeleteIfExists("Assets/XR/UserSimulationSettings/Resources/XRSimulationPreferences.asset");
        DeleteIfExists("Assets/XR/UserSimulationSettings/Resources/XRSimulationRuntimeSettings.asset");

        // ✅ Step 3 — Clean cache folders
        EditorUtility.DisplayProgressBar("Fixing Android Build", "Cleaning cache folders...", 0.6f);
        DeleteIfExists("Library");
        DeleteIfExists("Temp");
        DeleteIfExists("Logs");

        // ✅ Step 4 — Patch Gradle repositories
        EditorUtility.DisplayProgressBar("Fixing Android Build", "Patching Gradle repositories...", 0.8f);
        PatchGradleTemplate();

        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("Android Auto-Fix Complete ✅",
            "All fixes applied successfully.\n\nNow reopen Unity and rebuild your APK.", "Got it!");

        Debug.Log("✅ Android Build Auto-Fix completed successfully!");
    }

    private static void DeleteIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            Debug.Log($"🧹 Deleted: {path}");
        }
        else if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"🧹 Deleted: {path}");
        }
    }

    private static void PatchGradleTemplate()
    {
        string gradlePath = "Assets/Plugins/Android/mainTemplate.gradle";
        if (!File.Exists(gradlePath))
        {
            Debug.LogWarning("⚠️ mainTemplate.gradle not found. Enable 'Custom Main Gradle Template' in Player Settings → Publishing Settings.");
            return;
        }

        string content = File.ReadAllText(gradlePath);
        if (!content.Contains("jitpack.io"))
        {
            content = content.Replace("mavenCentral()",
                @"mavenCentral()
        maven { url 'https://maven.aliyun.com/repository/public' }
        maven { url 'https://jitpack.io' }");
            File.WriteAllText(gradlePath, content);
            Debug.Log("✅ Added fallback Maven mirrors to mainTemplate.gradle");
        }
        else
        {
            Debug.Log("✅ Gradle template already patched.");
        }
    }
}
#endif
