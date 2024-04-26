using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class PostProcessor : IPostprocessBuildWithReport
{
    public void OnPostprocessBuild(BuildReport report)
    {
        var sourcePath = Path.Combine(Application.dataPath, @"Texts\ThirdPartyNotices.txt");
        var destPath = Path.Combine(Path.GetDirectoryName(report.summary.outputPath), Path.GetFileName(sourcePath));
        File.Copy(sourcePath, destPath, true);

        Debug.Log(sourcePath);
        Debug.Log(destPath);
    }
    public int callbackOrder { get { return 0; } }
}