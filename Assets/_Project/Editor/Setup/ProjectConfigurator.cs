using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class ProjectConfigurator
{
    private const string EnableLogsDefine = "ENABLE_LOGS";

    [MenuItem("Tools/Project Setup/Apply Development Settings")]
    public static void ApplyDevelopmentSettings()
    {
        var namedTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        var defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);

        if (!System.Array.Exists(defines.Split(';'), d => d == EnableLogsDefine))
        {
            defines = string.IsNullOrEmpty(defines) ? EnableLogsDefine : defines + ";" + EnableLogsDefine;
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
        }

        PlayerSettings.colorSpace = ColorSpace.Linear;

        Debug.Log($"[ProjectConfigurator] ENABLE_LOGS added to {namedTarget.TargetName} defines. Color space set to Linear. " +
                   "Remember to strip ENABLE_LOGS from Release build defines before shipping.");
    }
}