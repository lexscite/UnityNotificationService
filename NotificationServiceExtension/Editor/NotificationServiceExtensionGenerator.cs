#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace PaperStag
{
public static class NotificationServiceExtensionGenerator
{
    private const string TargetName = "NotificationService";

    private const string FilesRelPath =
        "NotificationServiceExtension/Editor/Files";

    [PostProcessBuild(45)]
    public static void OnPostProcessBuild(BuildTarget target,
        string pathToBuiltProject)
    {
        CreateFiles(pathToBuiltProject);
        CreateExtension(pathToBuiltProject);
        ModifyPodFile(pathToBuiltProject);
    }

    private static void CreateFiles(string pathToBuiltProject)
    {
        var directoryInfo = Directory.CreateDirectory(Path.Combine(
            pathToBuiltProject,
            TargetName));
        var srcPath = $"{Application.dataPath}/{FilesRelPath}";
        var outPath = directoryInfo.FullName;
        File.Copy($"{srcPath}/Info.plist",
            $"{outPath}/Info.plist");
        File.Copy($"{srcPath}/NotificationService.h",
            $"{outPath}/NotificationService.h");
        File.Copy($"{srcPath}/NotificationService.m",
            $"{outPath}/NotificationService.m");
    }

    private static void CreateExtension(string pathToBuiltProject)
    {
        var projPath =
            PBXProject.GetPBXProjectPath(pathToBuiltProject);
        var proj = new PBXProject();
        proj.ReadFromFile(projPath);
        var mainTarget = proj.GetUnityMainTargetGuid();

        var plistPath = $"{pathToBuiltProject}/Info.plist";
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        var nsPlistPath = $"{pathToBuiltProject}/{TargetName}/Info.plist";
        var nsPlist = new PlistDocument();
        nsPlist.ReadFromFile(nsPlistPath);

        nsPlist.root.SetString("CFBundleShortVersionString",
            PlayerSettings.bundleVersion);
        nsPlist.root.SetString("CFBundleVersion",
            PlayerSettings.iOS.buildNumber);

        var nsTarget = proj.AddAppExtension(mainTarget,
            TargetName,
            PlayerSettings.GetApplicationIdentifier(BuildTargetGroup
                .iOS)
            + ".notificationservice",
            nsPlistPath);

        var section = proj.AddSourcesBuildPhase(nsTarget);

        proj.AddFile($"{TargetName}/Info.plist",
            $"{TargetName}/Info.plist");

        proj.AddFile($"{TargetName}/NotificationService.h",
            $"{TargetName}/NotificationService.h");

        proj.AddFileToBuildSection(nsTarget,
            section,
            proj.AddFile($"{TargetName}/NotificationService.m",
                $"{TargetName}/NotificationService.m"));

        proj.SetBuildProperty(nsTarget,
            "TARGETED_DEVICE_FAMILY",
            "1,2");

        proj.SetBuildProperty(nsTarget,
            "ARCHS",
            "arm64");

        proj.SetBuildProperty(nsTarget,
            "DEVELOPMENT_TEAM",
            PlayerSettings.iOS.appleDeveloperTeamID);

        nsPlist.WriteToFile(nsPlistPath);
        proj.WriteToFile(projPath);
        plist.WriteToFile(plistPath);
    }

    private static void ModifyPodFile(string pathToBuildProject)
    {
        const string platformValue = "platform :ios, '11.0'";
        const string platformNewValue = "platform :ios, '11.0'\n\n"
            + "def google_utilities\n"
            + "  pod 'GoogleUtilities/AppDelegateSwizzler'\n"
            + "  pod 'GoogleUtilities/MethodSwizzler'\n"
            + "  pod 'GoogleUtilities/Network'\n"
            + "  pod 'GoogleUtilities/NSData+zlib'\n"
            + "  pod 'GoogleUtilities/AppDelegateSwizzler'\n"
            + "  pod 'GoogleUtilities/Environment'\n"
            + "  pod 'GoogleUtilities/Logger'\n"
            + "  pod 'GoogleUtilities/UserDefaults'\n"
            + "  pod 'GoogleUtilities/Reachability'\n"
            + "end";

        const string unityFrameworkValue = "target 'UnityFramework' do";
        const string unityFrameworkNewValue = "target 'UnityFramework' do\n"
            + "  google_utilities";

        const string useFrameworksValue = "use_frameworks!";
        var useFrameworksNewValue = $"target '{TargetName}' do\n"
            + "  google_utilities\n"
            + "  pod 'Firebase/Messaging', '7.11.0'\n"
            + "end"
            + "\nuse_frameworks!";

        var path = $"{pathToBuildProject}/Podfile";
        if (!File.Exists(path))
        {
            Debug.LogError($"Cant modify dependencies"
                + " for NotificationService "
                + "Podfile at \"{path}\" doesn't exist");
            return;
        }

        var contents = File.ReadAllText(path);
        contents = contents
            .Replace(useFrameworksValue, useFrameworksNewValue)
            .Replace(platformValue, platformNewValue)
            .Replace(unityFrameworkValue, unityFrameworkNewValue);
        File.WriteAllText(path, contents);
    }
}
}
#endif