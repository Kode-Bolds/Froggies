using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    abstract class BuilderIntegrationTest
    {
        protected const string k_TestUSSFileName = "MyTestVisualTreeAsset.uss";
        protected const string k_TestUXMLFileName = "MyTestVisualTreeAsset.uxml";
        protected const string k_TestEmptyUSSFileNameNoExt = "EmptyTestStyleSheet";
        protected const string k_TestNoUSSDocumentUXMLFileNameNoExt = "NoUSSDocument";
        protected const string k_TestMultiUSSDocumentUXMLFileNameNoExt = "MultiUSSDocument";

        protected const string k_TestEmptyUSSFileName = k_TestEmptyUSSFileNameNoExt + ".uss";
        protected const string k_TestNoUSSDocumentUXMLFileName = k_TestNoUSSDocumentUXMLFileNameNoExt + ".uxml";
        protected const string k_TestMultiUSSDocumentUXMLFileName = k_TestMultiUSSDocumentUXMLFileNameNoExt + ".uxml";

        protected const string k_TestUSSFilePath = "Assets/" + k_TestUSSFileName;
        protected const string k_TestUXMLFilePath = "Assets/" + k_TestUXMLFileName;
        protected const string k_TestEmptyUSSFilePath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_TestEmptyUSSFileName;
        protected const string k_TestNoUSSDocumentUXMLFilePath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_TestNoUSSDocumentUXMLFileName;
        protected const string k_TestMultiUSSDocumentUXMLFilePath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_TestMultiUSSDocumentUXMLFileName;

        // TODO: This needs to be converted to an actual file.
        protected static readonly string k_TestUXMLFileContent
            = WebUtility.UrlDecode("%3Cui%3AUXML+xmlns%3Aui%3D%22UnityEngine.UIElements%22+xmlns%3Auie%3D%22UnityEditor.UIElements%22%3E%0D%0A++++%3Cui%3AVisualElement%3E%0D%0A++++++++%3Cui%3AVisualElement+%2F%3E%0D%0A++++%3C%2Fui%3AVisualElement%3E%0D%0A%3C%2Fui%3AUXML%3E%0D%0A");

        protected Builder BuilderWindow { get; private set; }
        protected BuilderSelection Selection { get; private set; }
        protected BuilderLibrary LibraryPane { get; private set; }
        protected BuilderHierarchy HierarchyPane { get; private set; }
        protected BuilderStyleSheets StyleSheetsPane { get; private set; }
        protected BuilderViewport ViewportPane { get; private set; }
        protected BuilderInspector InspectorPane { get; private set; }

        [SetUp]
        public virtual void Setup()
        {
            if (EditorApplication.isPlaying)
                BuilderWindow = EditorWindow.GetWindow<Builder>();
            else
                BuilderWindow = BuilderTestsHelper.MakeNewBuilderWindow();

            Selection = BuilderWindow.selection;
            LibraryPane = BuilderWindow.rootVisualElement.Q<BuilderLibrary>();
            HierarchyPane = BuilderWindow.rootVisualElement.Q<BuilderHierarchy>();
            StyleSheetsPane = BuilderWindow.rootVisualElement.Q<BuilderStyleSheets>();
            ViewportPane = BuilderWindow.rootVisualElement.Q<BuilderViewport>();
            InspectorPane = BuilderWindow.rootVisualElement.Q<BuilderInspector>();

            if (EditorApplication.isPlaying)
                return;

            ForceNewDocument();
            var createSelectorField = StyleSheetsPane.Q<TextField>();
            createSelectorField.visualInput.Blur();
            LibraryPane.SetViewMode(BuilderLibrary.LibraryViewMode.TreeView);
        }

        [UnityTearDown]
        protected virtual IEnumerator TearDown()
        {
            ForceNewDocument();
            MouseCaptureController.ReleaseMouse();

            yield return null;
            BuilderWindow?.Close();
            yield return null;
        }

        protected void ForceNewDocument()
        {
            if (BuilderWindow == null)
                return;

            BuilderWindow.rootVisualElement.Q<BuilderToolbar>().NewDocument(false);
        }

        protected IEnumerator CodeOnlyAddUSSToDocument(string path)
        {
            var builderWindow = BuilderWindow;

            // Need to have at least one element in the asset.
            if (builderWindow.document.visualTreeAsset.IsEmpty())
                AddElementCodeOnly("TestElement");

            yield return UIETestHelpers.Pause(1);

            // Make sure there's no modified version in memory.
            AssetDatabase.ImportAsset(
                k_TestEmptyUSSFilePath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            yield return UIETestHelpers.Pause(1);

            BuilderStyleSheetsUtilities.AddUSSToAsset(builderWindow, path);

            yield return UIETestHelpers.Pause(1);
        }

        protected void AddElementCodeOnly(string name = "")
        {
            AddElementCodeOnly<VisualElement>(name);
        }

        protected void AddElementCodeOnly<T>(string name = "") where T : VisualElement, new()
        {
            var element = BuilderLibraryContent.GetLibraryItemForType(typeof(T)).MakeVisualElementCallback.Invoke();

            if (!string.IsNullOrEmpty(name))
                element.name = name;

            BuilderWindow.documentRootElement.Add(element);
            BuilderAssetUtilities.AddElementToAsset(BuilderWindow.document, element);
            BuilderWindow.OnEnableAfterAllSerialization();
            Selection.NotifyOfHierarchyChange();
        }

        protected IEnumerator EnsureSelectorsCanBeAddedAndReloadBuilder()
        {
            var builderWindow = BuilderWindow;

            // Need to have at least one element in the asset.
            if (builderWindow.document.visualTreeAsset.IsEmpty())
                AddElementCodeOnly("TestElement");

            yield return UIETestHelpers.Pause(1);

            // If the builder currently has no stylesheets,
            // we add the test one so we can add selectors.
            if (builderWindow.document.firstStyleSheet == null)
            {
                // Make sure there's no modified version in memory.
                AssetDatabase.ImportAsset(
                    k_TestEmptyUSSFilePath,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

                yield return UIETestHelpers.Pause(1);

                BuilderStyleSheetsUtilities.AddUSSToAsset(builderWindow, k_TestEmptyUSSFilePath);
            }

            yield return UIETestHelpers.Pause(1);
        }

        protected IEnumerator AddVisualElement()
        {
            yield return AddElement(nameof(VisualElement));
        }

        protected IEnumerator AddTextFieldElement()
        {
            yield return AddElement("Text Field");
        }

        protected BuilderLibraryTreeItem FindLibraryItemWithData(string data)
        {
            var libraryTreeView = LibraryPane.Q<TreeView>();
            foreach (var item in libraryTreeView.items)
            {
                if (item is BuilderLibraryTreeItem libraryTreeItem)
                {
                    if (libraryTreeItem.data.Equals(data))
                        return libraryTreeItem;
                }
            }

            return null;
        }

        protected IEnumerator SelectLibraryTreeItemWithName(string elementLabel)
        {
            var builderLibraryTreeItem = FindLibraryItemWithData(elementLabel);
            Assert.IsNotNull(builderLibraryTreeItem);
            var libraryTreeView = LibraryPane.Q<TreeView>();
            yield return libraryTreeView.SelectAndScrollToItemWithId(builderLibraryTreeItem.id);
        }

        protected IEnumerator AddElement(string elementLabel)
        {
            yield return SelectLibraryTreeItemWithName(elementLabel);
            var label = BuilderTestsHelper.GetLabelWithName(LibraryPane, elementLabel);
            Assert.IsNotNull(label);

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                label.worldBound.center,
                HierarchyPane.worldBound.center);

            yield return UIETestHelpers.Pause(1);
        }

        protected IEnumerator AddSelector(string selectorName)
        {
            // TODO: No idea why but the artificial way of adding selectors with AddSelector() produces
            // selector elements that have no layout. I don't know why they don't layout even
            // though they are part of the hierarchy and have a panel! The Inspector remains blank because
            // it needs elements to be layed out.

            var builderWindow = BuilderWindow;

            var inputField = StyleSheetsPane.Q<TextField>("new-selector-field");
            inputField.visualInput.Focus();

            // Make
            yield return UIETestEvents.KeyBoard.SimulateTyping(builderWindow, selectorName);
            // TODO: I noticed many times the same key events being sent again (twice).
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(builderWindow, KeyCode.Return);

            // TODO: This does not always fire. Most of the time, the Blur event never makes
            // it to the control.
            inputField.visualInput.Blur();

            yield return UIETestHelpers.Pause(1);
        }

        protected void CreateTestUSSFile()
        {
            // We have tests that _wait_ for the AssetModificationProcessor to kick in with
            // the new asset being created here. If we, for some reason, leak the asset
            // from a previous run and we don't _re_create it, those some tests may way
            // forever. It is very important to delete the file if it's already there
            // and re-create it.
            AssetDatabase.DeleteAsset(k_TestUSSFilePath);
            AssetDatabase.Refresh();

            File.WriteAllText(k_TestUSSFilePath, string.Empty);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(k_TestUSSFilePath, ImportAssetOptions.ForceUpdate);
        }

        protected void CreateTestUXMLFile()
        {
            // We have tests that _wait_ for the AssetModificationProcessor to kick in with
            // the new asset being created here. If we, for some reason, leak the asset
            // from a previous run and we don't _re_create it, those some tests may way
            // forever. It is very important to delete the file if it's already there
            // and re-create it.
            AssetDatabase.DeleteAsset(k_TestUXMLFilePath);
            AssetDatabase.Refresh();

            File.WriteAllText(k_TestUXMLFilePath, k_TestUXMLFileContent);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(k_TestUXMLFilePath, ImportAssetOptions.ForceUpdate);
        }

        protected void DeleteTestUSSFile()
        {
            AssetDatabase.DeleteAsset(k_TestUSSFilePath);
        }

        protected void DeleteTestUXMLFile()
        {
            AssetDatabase.DeleteAsset(k_TestUXMLFilePath);
        }

        internal BuilderExplorerItem GetStyleSelectorNodeWithName(string selectorName)
        {
            return BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, selectorName);
        }

        internal BuilderExplorerItem GetHierarchyExplorerItemByElementName(string name)
        {
            return HierarchyPane.Query<BuilderExplorerItem>()
                .Where(item => BuilderTestsHelper.GetLinkedDocumentElement(item).name == name).ToList().First();
        }

        internal BuilderExplorerItem GetFirstExplorerVisualElementNode(string nodeName)
        {
            return BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nodeName);
        }

        internal VisualElement GetFirstDocumentElement()
        {
            return ViewportPane.documentElement[0];
        }

        internal BuilderExplorerItem GetFirstExplorerItem()
        {
            var firstDocumentElement = ViewportPane.documentElement[0];
            return BuilderTestsHelper.GetLinkedExplorerItem(firstDocumentElement);
        }
    }
}