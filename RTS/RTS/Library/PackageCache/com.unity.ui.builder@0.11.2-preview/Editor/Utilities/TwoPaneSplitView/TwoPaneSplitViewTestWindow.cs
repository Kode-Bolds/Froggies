using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class TwoPaneSplitViewTestWindow : EditorWindow
    {
        //[MenuItem("Tests/UI Builder/TwoPaneSplitViewTest")]
        static void ShowWindow()
        {
            var window = GetWindow<TwoPaneSplitViewTestWindow>();
            window.titleContent = new GUIContent("TwoPaneSplitViewTest");
            window.Show();
        }

        void OnEnable()
        {
            var root = rootVisualElement;

            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(BuilderConstants.UtilitiesPath + "/TwoPaneSplitViewTestWindow/TwoPaneSplitViewTestWindow.uss"));

            var xmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UtilitiesPath + "/TwoPaneSplitViewTestWindow/TwoPaneSplitViewTestWindow.uxml");
            xmlAsset.CloneTree(root);
        }
    }
}