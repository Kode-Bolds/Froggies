using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class BuilderDocumentTests : BuilderIntegrationTest
    {
        const string k_NewUxmlFilePath = "Assets/BuildDocumentTests__TestUI.uxml";

        public override void Setup()
        {
            base.Setup();

            // Make sure there's no modified version in memory.
            if (!EditorApplication.isPlaying)
            {
                AssetDatabase.ImportAsset(k_TestNoUSSDocumentUXMLFilePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(k_TestMultiUSSDocumentUXMLFilePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
        }

        protected override IEnumerator TearDown()
        {
            ForceNewDocument();
            AssetDatabase.DeleteAsset(k_NewUxmlFilePath);
            var guid = AssetDatabase.AssetPathToGUID(k_NewUxmlFilePath);
            if (!string.IsNullOrEmpty(guid))
            {
                var folderPath = BuilderConstants.BuilderDocumentDiskSettingsJsonFolderAbsolutePath;
                var fileName = guid + ".json";
                var path = folderPath + "/" + fileName;
                File.Delete(path);
            }

            yield return base.TearDown();
            DeleteTestUXMLFile();
            DeleteTestUSSFile();
        }

        void CheckNoUSSDocument()
        {
            var document = BuilderWindow.document;

            Assert.Null(document.firstStyleSheet);
            Assert.AreEqual(document.openUSSFiles.Count, 0);

            Assert.False(document.visualTreeAsset.IsEmpty());
            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));

            var labelInDocument = BuilderWindow.documentRootElement.Children().First();
            Assert.That(labelInDocument.GetType(), Is.EqualTo(typeof(Label)));
        }

        IEnumerator CheckMultiUSSDocument()
        {
            var document = BuilderWindow.document;

            Assert.NotNull(document.firstStyleSheet);
            Assert.AreEqual(document.openUSSFiles.Count, 2);

            Assert.False(document.visualTreeAsset.IsEmpty());
            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));

            yield return UIETestHelpers.Pause(1);

            var labelInDocument = BuilderWindow.documentRootElement.Children().First();
            Assert.That(labelInDocument.GetType(), Is.EqualTo(typeof(Label)));
            Assert.AreEqual(labelInDocument.resolvedStyle.width, 60);
            Assert.AreEqual(labelInDocument.resolvedStyle.backgroundColor, Color.green);
        }

        void UndoRedoCheckWithTextField()
        {
            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(2));
            Undo.PerformUndo();
            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));
            Undo.PerformRedo();
            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator SaveNewDocument()
        {
            var labelName = "test-label";

            AddElementCodeOnly<Label>(labelName);

            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));
            var labelInDocument = BuilderWindow.documentRootElement.Children().First();
            Assert.That(labelInDocument.GetType(), Is.EqualTo(typeof(Label)));
            Assert.AreEqual(labelInDocument.name, labelName);

            BuilderWindow.document.SaveUnsavedChanges(k_NewUxmlFilePath);

            var document = BuilderWindow.document;
            Assert.AreEqual(document.uxmlPath, k_NewUxmlFilePath);
            Assert.AreEqual(document.uxmlOldPath, k_NewUxmlFilePath);

            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));
            labelInDocument = BuilderWindow.documentRootElement.Children().First();
            Assert.That(labelInDocument.GetType(), Is.EqualTo(typeof(Label)));
            Assert.AreEqual(labelInDocument.name, labelName);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SaveAsWithNoUSS()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestNoUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);

            BuilderWindow.document.SaveUnsavedChanges(k_NewUxmlFilePath, true);

            var document = BuilderWindow.document;
            Assert.AreEqual(document.uxmlPath, k_NewUxmlFilePath);
            Assert.AreEqual(document.uxmlOldPath, k_NewUxmlFilePath);

            yield return UIETestHelpers.Pause(1);

            CheckNoUSSDocument();
        }

        [UnityTest]
        public IEnumerator SaveAsWithMoreThanOneUSS()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);

            BuilderWindow.document.SaveUnsavedChanges(k_NewUxmlFilePath, true);

            var document = BuilderWindow.document;
            Assert.AreEqual(document.uxmlPath, k_NewUxmlFilePath);
            Assert.AreEqual(document.uxmlOldPath, k_NewUxmlFilePath);

            yield return UIETestHelpers.Pause(1);

            yield return CheckMultiUSSDocument();
        }

        [Test]
        public void LoadExistingDocumentWithNoUSS()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestNoUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);

            CheckNoUSSDocument();
        }

        [UnityTest]
        public IEnumerator LoadExistingDocumentWithMoreThanOneUSS()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);

            yield return CheckMultiUSSDocument();
        }

        [Test]
        public void EnsureChangesAreUndoneIfOpeningNewDocWithoutSaving()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            var assetCount = asset.visualElementAssets.Count;
            BuilderWindow.LoadDocument(asset);
            Assert.AreEqual(BuilderWindow.document.visualTreeAsset, asset);

            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));

            AddElementCodeOnly<TextField>();
            // Test restoration of backup.
            Assert.AreNotEqual(asset.visualElementAssets.Count, assetCount);
            ForceNewDocument();
            Assert.AreEqual(asset.visualElementAssets.Count, assetCount);
            var asset2 = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            Assert.AreEqual(asset2.visualElementAssets.Count, assetCount);
        }

        [UnityTest]
        public IEnumerator EnsureChangesToUXMLMadeExternallyAreReloaded()
        {
            const string testLabelName = "externally-added-label";

            CreateTestUXMLFile();

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestUXMLFilePath);
            var assetCount = asset.visualElementAssets.Count;
            BuilderWindow.LoadDocument(asset);
            Assert.AreEqual(BuilderWindow.document.visualTreeAsset, asset);

            yield return AddTextFieldElement();
            Assert.AreEqual(asset.visualElementAssets.Count, assetCount + 1);

            // Save
            BuilderWindow.document.SaveUnsavedChanges(k_TestUXMLFilePath, false);

            var vtaCopy = BuilderWindow.document.visualTreeAsset.DeepCopy();
            var newElement = new VisualElementAsset(typeof(Label).ToString());
            newElement.AddProperty("name", testLabelName);
            vtaCopy.AddElement(vtaCopy.GetRootUXMLElement(), newElement);
            var vtaCopyUXML = vtaCopy.GenerateUXML(k_TestUXMLFilePath, true);
            File.WriteAllText(k_TestUXMLFilePath, vtaCopyUXML);
            AssetDatabase.ImportAsset(k_TestUXMLFilePath, ImportAssetOptions.ForceUpdate);

            yield return UIETestHelpers.Pause(1);

            // Make sure the UI Builder reloaded.
            var label = BuilderWindow.documentRootElement.Q<Label>(testLabelName);
            Assert.NotNull(label);
        }

        [UnityTest]
        public IEnumerator EnsureChangesToUSSMadeExternallyAreReloaded()
        {
            const string testSelector = ".externally-added-selector";

            CreateTestUXMLFile();
            CreateTestUSSFile();

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestUXMLFilePath);
            var assetCount = asset.visualElementAssets.Count;
            BuilderWindow.LoadDocument(asset);
            Assert.AreEqual(BuilderWindow.document.visualTreeAsset, asset);

            yield return CodeOnlyAddUSSToDocument(k_TestUSSFilePath);
            Assert.NotNull(BuilderWindow.document.activeStyleSheet);

            // Save
            BuilderWindow.document.SaveUnsavedChanges(k_TestUXMLFilePath, false);

            var styleSheetCopy = BuilderWindow.document.activeStyleSheet.DeepCopy();
            styleSheetCopy.AddSelector(testSelector);
            var styleSheetCopyUSS = styleSheetCopy.GenerateUSS();
            File.WriteAllText(k_TestUSSFilePath, styleSheetCopyUSS);
            AssetDatabase.ImportAsset(k_TestUSSFilePath, ImportAssetOptions.ForceUpdate);

            yield return UIETestHelpers.Pause(1);

            // Make sure the UI Builder reloaded.
            var activeStyleSheet = BuilderWindow.document.activeStyleSheet;
            var complexSelector = activeStyleSheet.complexSelectors.First();
            Assert.NotNull(complexSelector);
            Assert.AreEqual(StyleSheetToUss.ToUssSelector(complexSelector), testSelector);
        }

        [Test]
        public void UndoRedoCreationOfTextFieldInMultiUSSDocument()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);
            AddElementCodeOnly<TextField>();

            UndoRedoCheckWithTextField();
        }

        [UnityTest]
        public IEnumerator UndoRedoBeforeAndAfterGoingIntoPlaymode()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);
            BuilderWindow.LoadDocument(asset);

            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));

            AddElementCodeOnly<TextField>();

            UndoRedoCheckWithTextField();

            yield return new EnterPlayMode();

            UndoRedoCheckWithTextField();

            yield return new ExitPlayMode();

            UndoRedoCheckWithTextField();

            yield return null;
        }

        [UnityTest]
        public IEnumerator UndoRedoBeforeAndAfterGoingIntoPlaymodeWithSceneReference()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestMultiUSSDocumentUXMLFilePath);

            var newObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var component = newObject.AddComponent<Tests.UIBuilderUXMLReferenceForTests>();
            component.visualTreeAssetRef = asset;

            BuilderWindow.LoadDocument(asset);

            Assert.That(BuilderWindow.documentRootElement.childCount, Is.EqualTo(1));

            AddElementCodeOnly<TextField>();

            UndoRedoCheckWithTextField();

            yield return new EnterPlayMode();

            UndoRedoCheckWithTextField();

            yield return new ExitPlayMode();

            UndoRedoCheckWithTextField();

            yield return null;
        }

        [UnityTest]
        public IEnumerator SettingsCopiedFromUnsavedDocument()
        {
            var documentHierarchyHeader = HierarchyPane.Q<BuilderExplorerItem>();
            yield return UIETestEvents.Mouse.SimulateClick(documentHierarchyHeader);

            var colorButton = InspectorPane.Q<Button>("Color");
            yield return UIETestEvents.Mouse.SimulateClick(colorButton);

            var colorField = InspectorPane.Q<ColorField>("background-color-field");
            colorField.value = Color.green;
            yield return UIETestHelpers.Pause(1);

            BuilderWindow.document.SaveUnsavedChanges(k_NewUxmlFilePath);
            Assert.That(BuilderWindow.document.settings.CanvasBackgroundMode, Is.EqualTo(BuilderCanvasBackgroundMode.Color));
            Assert.That(BuilderWindow.document.settings.CanvasBackgroundColor, Is.EqualTo(Color.green));
        }
    }
}
