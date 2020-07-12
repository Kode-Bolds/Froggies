using UnityEditor;
using UnityEditor.StyleSheets;

namespace Unity.UI.Builder
{
    internal class BuilderStyleSheetImporter : StyleSheetImporterImpl
    {
        public BuilderStyleSheetImporter()
        {

        }

        public override UnityEngine.Object DeclareDependencyAndLoad(string path)
        {
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }
    }
}