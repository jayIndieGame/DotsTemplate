using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Unity.Coding.Format;
using Unity.Coding.Utils;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

#if UNITY_2019_2_OR_NEWER
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
#endif

namespace Unity.Coding.Editor
{
    [InitializeOnLoad]
    static class EditorIntegration
    {
        static EditorIntegration()
        {
            // We don't want to format code when running in batch mode or in test runs
            if (Application.isBatchMode)
                return;
            if (Environment.GetCommandLineArgs().Contains("-runTests") || Environment.GetCommandLineArgs().Contains("-runEditorTests"))
                return;
            Init();
        }

        internal static void Init()
        {
            CodingToolsInternal.Bridge.CodingToolsAssetsModifiedProcessor.AssetsModified += OnAssetsModified;
        }

        static void OnAssetsModified(string[] changedAssets, string[] addedAssets, string[] deletedAssets, UnityEditor.Experimental.AssetMoveInfo[] movedAssets, out string[] processedAssets)
        {
            var formatContext = Utility.CreateFormatContext(FormatTriggerKind.Automatic);

            var pathsToFormat = changedAssets.Concat(addedAssets)
                .Where(p => !Path.IsPathRooted(p))
                .Select(p => new NPath(p))
                .Where(p => Utility.ShouldFormat(formatContext, p))
                .UnDefer();

            var formatted = Utility.ProcessWithProgressBar(pathsToFormat, formatContext);
            Utility.lastFormattedAssets = formatted.AddRangeOverride(Utility.lastFormattedAssets);

            //report back processed assets
            processedAssets = formatted.Keys.ToArray();
        }
    }
    static class Utility
    {
        public static bool ShouldFormat(FormatContext formatContext, NPath pathToFormat)
        {
            // files only please
            if (!pathToFormat.FileExists())
            {
                return false;
            }

            var fullPath = Path.GetFullPath(pathToFormat).ToNPath();

            var libraryFolder = formatContext.BaseDir.Combine("Library");
            // ignore files under Library folder (for instance files under Library/PackageCache are Git/npm cached packages, whence immutable)
            if (fullPath.IsChildOf(libraryFolder))
            {
                return false;
            }

            if (!fullPath.IsChildOf(formatContext.BaseDir))
            {
#if UNITY_2019_2_OR_NEWER
                // Ignore any file that is not a child of the project root or a local package (which can live anywhere in the file system)
                var p = PackageInfo.FindForAssetPath(pathToFormat.ToString(SlashMode.Forward));
                if (p == null || p.source != PackageSource.Local)
                    return false;
#else
                // can't find the package source of a given asset path until 2019.2
                return false;
#endif
            }

            if (lastFormattedAssets != null && lastFormattedAssets.TryGetValue(fullPath, out var lastTimestamp))
            {
                var currentTimeStamp = File.GetLastWriteTime(fullPath).Ticks;

                // if timestamp is different, format (user may have reverted some changes through a VCS which resets the timestamp).
                var shouldFormat = lastTimestamp != currentTimeStamp;
                LogFormatDecision(fullPath, shouldFormat, formatContext.Logger, lastTimestamp, currentTimeStamp);

                if (!shouldFormat)
                    return false;
                return shouldFormat;
            }

            return true;
        }

        [Conditional("DEBUG_UNITY_CODING")]
        static void LogFormatDecision(NPath fullPath, bool shouldFormat, Utils.ILogger logger, long lastTimestamp, long currentTimeStamp)
        {
            if (!shouldFormat)
                logger.Debug($"File '{fullPath}' was not formatted - timestamp not changed (Last Written: {lastTimestamp}, Current Timestamp: {currentTimeStamp}).");
        }

        public static void CollectUnityAssetPathsRecursively(string path, [NotNull] List<string> assetPaths)
        {
            if (assetPaths == null)
                throw new ArgumentNullException($"{nameof(assetPaths)} must be non-null");
            NPath nPath = path.ToNPath();
            if (nPath.FileExists())
            {
                assetPaths.Add(nPath);
            }
            else if (nPath.DirectoryExists())
            {
                var assets = AssetDatabase.FindAssets("*", new string[] { nPath });
                assetPaths.AddRange(assets
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid)));
            }
        }

        class FormatLogger : Utils.ILogger
        {
            public void Error(string message) => UnityEngine.Debug.LogError(message);
            public void Info(string message) => UnityEngine.Debug.Log(message);
            public void Debug(string message) => Console.WriteLine(message);

            public static Utils.ILogger Instance => s_Instance;

            static FormatLogger s_Instance = new FormatLogger();
        }

        public static FormatContext CreateFormatContext(FormatTriggerKind triggerKind)
        {
            return new FormatContext
            {
                ThisPackageRoot = CodingPackageRoot,
                Logger          = FormatLogger.Instance,
                TriggerKind     = triggerKind,
            };
        }

        public static void ResetTimestamps() => lastFormattedAssets.Clear();

        public static NPath CodingPackageRoot => Path.GetFullPath("Packages/com.unity.coding").ToNPath();

        public static IDictionary<string, long> lastFormattedAssets = new Dictionary<string, long>();

        internal static IDictionary<string, long> Process(ICollection<NPath> paths, FormatContext formatContext)
        {
            return Formatter.Process(paths, formatContext);
        }

        internal static IDictionary<string, long> ProcessWithProgressBar(ICollection<NPath> paths, FormatContext formatContext)
        {
            var sw = Stopwatch.StartNew();
            int index = 0;
            long lastUpdate = 0;
            try
            {
                return Formatter.Process(paths, formatContext, formatItem =>
                {
                    string operation = (formatContext.Options & FormatOptions.DryRun) == 0 ? "Formatting" : "Validating";
                    long elapsedMilliseconds = sw.ElapsedMilliseconds;
                    //show after 2 seconds, update every 200ms (for perf)
                    if (elapsedMilliseconds > 2000 && elapsedMilliseconds - 200 > lastUpdate)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar($"{operation} files", $"{operation} \"{formatItem}\"", index / (float)paths.Count))
                            return true;

                        lastUpdate = elapsedMilliseconds;
                    }

                    index++;
                    return false;
                });
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [Flags]
        public enum FormatUtilityOptions
        {
            None = 0,
            ProgressBar = 1 << 0,
            DryRun = 1 << 1,
            Default = ProgressBar
        }
        static public IDictionary<string, long> Format(IEnumerable<string> paths,
            FormatUtilityOptions options = FormatUtilityOptions.Default)
        {
            var formatContext = CreateFormatContext(FormatTriggerKind.UserInitiated);

            bool dryRun = (options & FormatUtilityOptions.DryRun) != 0;
            if (dryRun)
            {
                formatContext.Options |= FormatOptions.DryRun;
            }
            var validPaths = paths
                .Select(obj => new NPath(obj))
                .Where(path => ShouldFormat(formatContext, path))
                .ToList();

            IDictionary<string, long> formatted = null;
            if ((options & FormatUtilityOptions.ProgressBar) == FormatUtilityOptions.ProgressBar)
                formatted = ProcessWithProgressBar(validPaths, formatContext);
            else
                formatted = Process(validPaths, formatContext);

            if (validPaths.Count > 0 && formatted.Count == 0 && !Formatter.HasEditorConfigInDirectoryTree(validPaths.Select(p => p.MakeAbsolute())))
            {
                var projectRoot = Application.dataPath.ToNPath().Parent;
                var templatePath = $"{Path.GetFullPath("Packages/com.unity.coding/Coding~/Configs/EditorConfig/.editorconfig")}";
                Debug.LogWarning($"Explicit source code formatting requested but no .editorconfig was found in the directory tree. You can copy {templatePath} to '{projectRoot}/.editorconfig' or any sub-directory you want files to be formatted.");
            }
            else
            {
                string operation = dryRun ? "Invalid" : "Formatted";
                Debug.Log($"Checked: {validPaths.Count}, {operation}: {formatted.Count} file(s).");
            }
            lastFormattedAssets = formatted.AddRangeOverride(lastFormattedAssets);
            return formatted;
        }
    }

    static class CodingMenuItems
    {
        [MenuItem("Assets/Format Code")]
        static void MenuAssetsFormat()
        {
            var assetsToFormat = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.DeepAssets).ToList();
            var paths = assetsToFormat
                .Select(obj => AssetDatabase.GetAssetPath(obj.GetInstanceID()))
                .ToList();
            Format(paths);
        }

        static void Format(List<string> input)
        {
            Utility.Format(input);
            AssetDatabase.Refresh();
        }
    }
}
