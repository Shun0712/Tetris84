// Assets/Editor/GetBuildTarget.cs
using UnityEditor;
using UnityEngine;

public class GetBuildTarget
{
    // コマンドラインから実行するためのメソッド
    public static void GetCurrentBuildTargetCommandLine()
    {
        Debug.Log("Current Command Line Build Target: " + EditorUserBuildSettings.activeBuildTarget.ToString());
    }
}
