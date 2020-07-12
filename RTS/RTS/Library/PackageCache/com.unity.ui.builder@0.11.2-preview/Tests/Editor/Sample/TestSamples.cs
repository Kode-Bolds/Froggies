using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    /// <summary>
    /// Contains the most common Integration Test actions.
    /// <see cref="UIETestEvents"/> - Use to simulate user input actions.
    /// <see cref="UIETestHelpers"/>  - Various UIE testing helper methods.
    /// <see cref="BuilderTestsHelper"/> - Builder specific helper methods.
    /// <see cref="BuilderIntegrationTest"/> - Base integration test class. Will create new Builder window instance
    /// during setup and contains some helper methods and quick access methods.
    /// </summary>
    class TestSamples : BuilderIntegrationTest
    {
        // [SetUp] - This is not necessary here because base class has this attribute.
        public override void Setup()
        {
            // Add additional Setup steps here.
            base.Setup();
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            // Add time sliced SetUp steps here.
            yield return UIETestHelpers.Pause();
        }

        // [UnityTearDown] - This is not necessary here because base class has this attribute.
        protected override IEnumerator TearDown()
        {
            // Add time sliced Tear Down steps here.
            yield return base.TearDown();

            // In case your tests are adding test uxml files, make sure to remove it
            DeleteTestUXMLFile();
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Add One Time SetUp instructions here.
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // One Time SetUp instructions here.
        }

        [UnityTest]
        public IEnumerator SampleTest()
        {
            // A new Builder window created when the test started.
            // The created window will be destroyed during the teardown stage.
            Assert.That(BuilderWindow, Is.Not.Null);

            // Fast access to the main builder panes.
            Assert.That(StyleSheetsPane, Is.Not.Null);
            Assert.That(HierarchyPane, Is.Not.Null);
            Assert.That(LibraryPane, Is.Not.Null);
            Assert.That(ViewportPane, Is.Not.Null);
            Assert.That(InspectorPane, Is.Not.Null);

            // Use the following methods to add standard controls to the document.
            AddElementCodeOnly();
            AddElementCodeOnly<TextField>();
            AddElementCodeOnly<Button>();

            // Get created element.
            var createdButton = ViewportPane.documentElement.Q<Button>();

            // Get linked hierarchy item.
            var buttonHierarchyNode = BuilderTestsHelper.GetLinkedExplorerItem(createdButton);

            // Create New selector.
            // There's a requirement that a USS file has already been added to
            // the document in order to add selectors. This function will make
            // sure the document is ready to have new selectors created.
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();
            yield return AddSelector(".my-selector");

            // Retrieve created selector explorer node.
            var mySelectorNode = GetStyleSelectorNodeWithName(".my-selector");

            // Drag and drop sample. Drag selector onto created button.
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                mySelectorNode.Q<Label>().worldBound.center,
                buttonHierarchyNode.worldBound.center);

            // Selected created TextField node and inline change style.
            // Avoid using ListView / TreeView / VisualElement API to select / focus an item.
            // Simulate how a user would do it with UIETestEvents API.
            var textFieldHierarchyNode =  BuilderTestsHelper.GetLinkedExplorerItem(ViewportPane.documentElement.Q<TextField>());
            yield return UIETestEvents.Mouse.SimulateClick(textFieldHierarchyNode);

            // Make sure you unfold style properties group before accessing the properties.
            // Keep in mind the amount of time that will be spent by your test. Try to keep it as low as possible.
            // In the code sample below, instead of simulating user interaction, we will just set controls values directly.
            // However, there are no strict rules when you should skip user interaction simulation, use your judgment.
            var displayFoldout = InspectorPane.Query<PersistedFoldout>().Where(f => f.text.Equals("Display")).First();
            displayFoldout.value = true;

            var percentSlider = displayFoldout.Query<PercentSlider>().Where(t => t.label.Equals("Opacity")).First();
            percentSlider.value = 0.5f;

            yield return UIETestHelpers.Pause();

            var textFieldDocumentItem = BuilderTestsHelper.GetLinkedDocumentElement(textFieldHierarchyNode);
            Assert.That(textFieldDocumentItem.opacity, Is.EqualTo(percentSlider.value));
        }

        [UnityTest]
        public IEnumerator CreateUXML()
        {
            // If you need specific UXML resource to be available in the project during the test, use the similar approach as:
            // The default test UXML file will be removed during the tear down stage.
            // If your test creates any other custom files, make sure to clean up.
            CreateTestUXMLFile();

            // Wait for the builder library to refresh.
            yield return UIETestHelpers.Pause();
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestUXMLFilePath);
            Assert.That(asset, Is.Not.Null);
        }
    }
}
