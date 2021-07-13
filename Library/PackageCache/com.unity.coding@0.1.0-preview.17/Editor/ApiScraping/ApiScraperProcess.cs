using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Coding.Utils;
using Debug = UnityEngine.Debug;

namespace Unity.Coding.Editor.ApiScraping
{
    class ApiScraperProcess : IApiScraperProcess
    {
        internal const string k_ConfigFilePath = "Library/com.unity.coding/ApiScraper/apiscraper.rsp";
        static Profiling.ProfilerMarker s_WriteConProfilerMarker = new Profiling.ProfilerMarker("WriteBatchConfig");
        Process m_BackgroundProcess;

        public event Action<string> verifyNoWriteFailed;
        public void Start(IEnumerable<(string assemblyFullPath, string outputPath)> scrapeList, IEnumerable<string> referenceDirectories, ScrapeMode scrapeMode)
        {
            string configFilePath;
            using (s_WriteConProfilerMarker.Auto())
            {
                configFilePath = WriteConfigurationFile(scrapeList, referenceDirectories);
            }

            NPath monoPath = UnityEditor.EditorApplication.applicationContentsPath.ToNPath().Combine("MonoBleedingEdge", "bin", "mono");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                monoPath = monoPath.ChangeExtension(".exe");

            var apiScraperAbsolutePath = Path.GetFullPath("Packages/com.unity.coding/Tools~/ApiScraper/ApiScraper.exe");

            var args = new[] { $"\"{apiScraperAbsolutePath}\"", $"-batch-config=\"{configFilePath}\"", "-parse-docs" };

            if (scrapeMode == ScrapeMode.EnsureSuccess || scrapeMode == ScrapeMode.VerifyNoWrite)
            {
                var previousScrapedContent = new Dictionary<string, string>();
                foreach (var tbs in scrapeList.Where(c => File.Exists(c.outputPath)))
                {
                    previousScrapedContent[tbs.outputPath] = File.ReadAllText(tbs.outputPath).Replace("\r", "");
                }

                List<string> stderr = new List<string>();
                List<string> stdout = new List<string>();
                var exitCode = ProcessUtility.ExecuteCommandLine(monoPath, args, null, stdout, stderr);
                if (exitCode != 0)
                {
                    UnityEngine.Debug.LogError($"ApiScraper failed with exit code {exitCode}. \nStderror:\n{string.Join("\n",stderr)}\nStdout:\n{string.Join("\n", stdout)}\nCommand line:\n{monoPath} {string.Join(" ",args)}");
                    return;
                }

                if (scrapeMode == ScrapeMode.VerifyNoWrite)
                {
                    foreach (var tbs in scrapeList)
                    {
                        var newContent = File.ReadAllText(tbs.outputPath).Replace("\r", "");;
                        if (!previousScrapedContent.TryGetValue(tbs.outputPath, out var previousContent) || previousContent != newContent)
                            verifyNoWriteFailed?.Invoke(tbs.outputPath);
                    }
                }
            }
            else
            {
                var psi = new ProcessStartInfo(monoPath)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    Arguments = string.Join(" ", args),
                    RedirectStandardError = true
                };

                if (m_BackgroundProcess != null)
                {
                    throw new InvalidOperationException($"{nameof(m_BackgroundProcess)} is not null which means the handle was not released properly");
                }

                m_BackgroundProcess = new Process() { StartInfo = psi };
                m_BackgroundProcess.Exited += (sender, eventArgs) =>
                {
                    if (m_BackgroundProcess?.ExitCode != 0)
                    {
                        var error = m_BackgroundProcess?.StandardError.ReadToEnd();
                        if (!string.IsNullOrEmpty(error))
                            Debug.Log("Api Scraper failed:\n" + error);
                    }
                    m_BackgroundProcess?.Dispose();
                    m_BackgroundProcess = null;
                };
                m_BackgroundProcess.EnableRaisingEvents = true;
                m_BackgroundProcess.Start();
            }
        }

        static string WriteConfigurationFile(IEnumerable<(string assemblyFullPath, string outputPath)> scrapeList, IEnumerable<string> referenceDirectories)
        {
            /*
             * assembly1 path
             * output1 path
             * assembly2 path
             * output2 path
             * -r:assembly ref path
             * -r:assembly ref path
             * -r:assembly ref path
             */
            if (File.Exists(k_ConfigFilePath))
                File.Delete(k_ConfigFilePath);

            Directory.CreateDirectory(Path.GetDirectoryName(k_ConfigFilePath));

            using (var writer = new StreamWriter(File.OpenWrite(k_ConfigFilePath)))
            {
                foreach (var tbs in scrapeList)
                {
                    writer.WriteLine(tbs.assemblyFullPath);
                    writer.WriteLine(tbs.outputPath);
                }

                foreach (var refDir in referenceDirectories)
                {
                    writer.WriteLine($"-r:{refDir}");
                }
            }

            return k_ConfigFilePath;
        }

        public void StopIfExecuting()
        {
            var localTemp = m_BackgroundProcess;
            if (localTemp != null && !localTemp.HasExited)
            {
                try
                {
                    localTemp.Kill();
                    localTemp.WaitForExit();
                }
                catch{}
                finally
                {
                    m_BackgroundProcess?.Dispose();
                    m_BackgroundProcess = null;
                }
            }
        }
    }
}
