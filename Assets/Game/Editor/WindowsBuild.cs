using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Lithostride.EditorTools
{
    /// <summary>
    /// Gera o executável de Windows em <c>Build/Windows/</c>. O
    /// <c>build_windows.bat</c> chama <see cref="RebuildAndBuild"/> em modo
    /// batch: prepara o Pack completo, reconstrói a cena de teste e só então
    /// faz o build. Se faltar arquivo do pack, para antes de tocar na cena.
    /// </summary>
    public static class WindowsBuild
    {
        private const string OutputPath = "Build/Windows/LITHOSTRIDE.exe";

        [MenuItem("Lithostride/Pack completo/Reconstruir e gerar executável (Windows)", false, 20)]
        public static void RebuildAndBuild()
        {
            if (!PackPipeline.PrepareAndBuild())
            {
                Fail("preparação do Pack completo falhou");
                return;
            }

            Build();
        }

        public static void Build()
        {
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { PackSceneBuilder.ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("LITHOSTRIDE: build concluída em " + summary.outputPath + " (" +
                          (summary.totalSize / (1024 * 1024)) + " MB).");
            }
            else
            {
                Fail(summary.result + ", " + summary.totalErrors + " erro(s)");
            }
        }

        private static void Fail(string reason)
        {
            Debug.LogError("LITHOSTRIDE: build falhou (" + reason + ").");

            // Em modo batch, código de saída diferente de zero avisa o .bat.
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
