using System;
using UnityEditor;
using UnityEngine;

public class Builder
{
    public static void Build(BuildTarget target)
    {
        try
        {
            BuildPipeline.BuildPlayer(
                new[] {"Assets/Scenes/SampleScene.unity"},
                $"Builds/{target.ToString()}/{target.ToString()}",
                target,
                BuildOptions.EnableHeadlessMode
            );
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            throw;
        }
    }

    public static void BuildStandaloneLinux64() => Build(BuildTarget.StandaloneLinux64);
    public static void BuildStandaloneOSX() => Build(BuildTarget.StandaloneOSX);
    public static void BuildStandaloneWindows64() => Build(BuildTarget.StandaloneWindows64);
}
