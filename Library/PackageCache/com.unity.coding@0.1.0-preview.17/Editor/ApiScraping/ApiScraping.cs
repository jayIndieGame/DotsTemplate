using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;
using File = System.IO.File;

namespace Unity.Coding.Editor.ApiScraping
{
    public static class ApiScraping
    {
        /// <summary>
        /// Validate if all .api files match the current state of public API in the codebase. If false, the .api files
        /// can be regenerated with ApiScraping.Scrape method or by configuring API Scraper to run automatically.
        /// </summary>
        /// <param name="failedFileList">List of paths to files that failed validation.</param>
        /// <returns></returns>
        public static bool ValidateAllFilesScraped(List<string> failedFileList)
        {
            return ApiScrapingEditorIntegration.ValidateAll(failedFileList);
        }

        /// <summary>
        /// Scrape public API based on .editorconfig rules.
        /// Rules in .editorconfig are defined as sections with glob patterns matching .asmdef files in the project.
        ///
        /// Examples enabling API scraping for all assemblies:
        /// [*.asmdef]
        /// scrape_api = true
        ///
        /// Examples disabling API scraping for assemblies in Test folders:
        /// [**/Tests/**.asmdef]
        /// scrape_api = false
        /// </summary>
        public static void Scrape()
        {
            ApiScrapingEditorIntegration.ForceScanAndScrape();
            AssetDatabase.Refresh();
        }

        private static void ScrapeWithDocs()
        {
            GenerateXmlDocFiles();
            CompilationPipeline.compilationFinished += (a) =>
            {
                Scrape();
            };
        }

        private static void GenerateXmlDocFiles()
        {
            foreach (var assembly in CompilationPipeline.GetAssemblies())
            {
                var asmDefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly.name);
                if (asmDefPath == null)
                    continue;
                var asmOutDirPath = Path.GetDirectoryName(assembly.outputPath);
                var docXmlOutputPath = Path.Combine(asmOutDirPath, Path.GetFileNameWithoutExtension(asmDefPath) + ".xml");
                var asmDefDirPath = Path.GetDirectoryName(asmDefPath);
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(asmDefPath);
                if (packageInfo != null && packageInfo.source != PackageSource.Embedded)
                {
                    continue;
                }
                var rspFilePath = Path.Combine(asmDefDirPath, "csc.rsp");
                File.WriteAllText(rspFilePath, $"-doc:{docXmlOutputPath}\n");
            }
            AssetDatabase.Refresh();
        }
    }
}
