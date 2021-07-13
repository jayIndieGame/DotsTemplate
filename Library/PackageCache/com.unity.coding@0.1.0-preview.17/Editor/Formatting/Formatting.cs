using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using UnityEditor;

namespace Unity.Coding.Editor.Formatting
{
    public static class Formatting
    {
        /// <summary>
        /// Format files in your project based on .editorconfig rules.
        ///
        /// Note: Only files under "Assets/" and "Packages/" will be considered for formating.
        /// Please see full documentation for detail on formatters and EditorConfig.
        /// </summary>
        /// <returns>Collection of files that were formatted</returns>
        public static ICollection<string> Format()
        {
            var pathsToFormat = new List<string>();
            Utility.CollectUnityAssetPathsRecursively("Assets", pathsToFormat);
            Utility.CollectUnityAssetPathsRecursively("Packages", pathsToFormat);

            var formatted = Utility.Format(pathsToFormat);

            return formatted.Keys;
        }

        /// <summary>
        /// Recursively format files in a specific project folder based on .editorconfig rules.
        ///
        /// Note: Only files under "Assets/" and "Packages/" will be considered for formating.
        /// Please see full documentation for detail on formatters and EditorConfig.
        /// </summary>
        /// <param name="path">Path to file or folder to format.</param>
        /// <returns>Collection of files that were formatted</returns>
        public static ICollection<string> Format(string path)
        {
            var pathsToFormat = new List<string>();
            Utility.CollectUnityAssetPathsRecursively(path, pathsToFormat);
            var formatted = Utility.Format(pathsToFormat);

            return formatted.Keys;
        }

        /// <summary>
        /// Recursively validates if files in a specific project folder are correctly formatted based on .editorconfig rules.
        ///
        /// Note: Only files under "Assets/" and "Packages/" will be considered for formating.
        /// Please see full documentation for detail on formatters and EditorConfig.
        /// </summary>
        /// <param name="failedFileList">List of files that have failed validation.</param>
        /// <returns>True if no files have failed validation, false otherwise</returns>
        public static bool ValidateAllFilesFormatted(string path, List<string> failedFileList = null)
        {
            var pathsToFormat = new List<string>();
            Utility.CollectUnityAssetPathsRecursively(path, pathsToFormat);
            var formatted = Utility.Format(pathsToFormat, Utility.FormatUtilityOptions.Default | Utility.FormatUtilityOptions.DryRun);

            if (failedFileList != null)
                failedFileList.AddRange(formatted.Keys);

            return formatted.Count == 0;
        }
    }
}
