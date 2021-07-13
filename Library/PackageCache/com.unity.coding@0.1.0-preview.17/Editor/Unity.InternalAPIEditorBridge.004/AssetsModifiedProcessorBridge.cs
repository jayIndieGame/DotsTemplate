using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental;
using Unity.Coding.Editor;
using Unity.Coding.Format;
using Unity.Coding.Utils;

namespace CodingToolsInternal.Bridge
{
    class CodingToolsAssetsModifiedProcessor : AssetsModifiedProcessor
    {
        public delegate void AssetsModifiedCallback(string[] changedAssets, string[] addedAssets, string[] deletedAssets, AssetMoveInfo[] movedAssets, out string[] processedAssets);

        public static AssetsModifiedCallback AssetsModified;


        protected override void OnAssetsModified(string[] changedAssets, string[] addedAssets, string[] deletedAssets, AssetMoveInfo[] movedAssets)
        {
            if (AssetsModified == null)
                return;

            string[] processedAssets;
            AssetsModified.Invoke(changedAssets, addedAssets, deletedAssets, movedAssets, out processedAssets);

            if (processedAssets == null)
                return;

            foreach (var asset in processedAssets)
            {
                ReportAssetChanged(asset);
            }
        }
    }
}
