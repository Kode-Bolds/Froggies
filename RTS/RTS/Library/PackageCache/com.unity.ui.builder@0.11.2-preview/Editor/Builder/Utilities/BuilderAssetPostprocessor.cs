using System.Collections.Generic;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal interface IBuilderAssetPostprocessor
    {
        void OnPostProcessAsset(string assetPath);
    }

    internal class BuilderAssetPostprocessor : AssetPostprocessor
    {
        private static readonly HashSet<IBuilderAssetPostprocessor> m_Processors =
            new HashSet<IBuilderAssetPostprocessor>();

        public static void Register(IBuilderAssetPostprocessor processor)
        {
            m_Processors.Add(processor);
        }

        public static void Unregister(IBuilderAssetPostprocessor processor)
        {
            m_Processors.Remove(processor);
        }

        static bool IsBuilderFile(string assetPath)
        {
            if (assetPath.EndsWith(".uxml") || assetPath.EndsWith(".uss"))
                return true;

            return false;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                if (!IsBuilderFile(assetPath))
                    continue;

                foreach (var processor in m_Processors)
                    processor.OnPostProcessAsset(assetPath);
            }
        }
    }
}