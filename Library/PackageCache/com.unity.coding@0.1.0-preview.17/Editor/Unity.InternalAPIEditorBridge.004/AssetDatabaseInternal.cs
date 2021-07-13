using UnityEditor.Compilation;
using UnityEditor.PackageManager;

namespace CodingToolsInternal.Bridge
{
    static internal class AssetDatabaseInternal
    {
        static public bool IsFolderImmutable(string path)
        {
            UnityEditor.AssetDatabase.GetAssetFolderInfo(path, out var isRoot, out var isImmutable);
            return isImmutable;
        }
    }
}
