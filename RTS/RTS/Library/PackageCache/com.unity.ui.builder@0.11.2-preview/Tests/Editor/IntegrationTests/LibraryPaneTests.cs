using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class LibraryPaneTests : BuilderIntegrationTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            var contentUpdated = false;
            BuilderLibraryContent.ResetProjectUxmlPathsHash();
            BuilderLibraryContent.OnLibraryContentUpdated += () =>
            {
                contentUpdated = true;
            };

            CreateTestUXMLFile();

            // TODO: We need to reconsider this pattern as it is very dangerous.
            // For one, [UniteSetUp] runs BEFORE [SetUp], so at this point
            // there is no UI Builder window open. As such, our
            // BuilderLibraryContent.AssetModificationProcessor.OnAssetChange() would
            // check for the "ActiveWindow" and because it was null it would never
            // call RegenerateLibraryContent(), and therefore OnLibraryContentUpdated.
            //
            // And that's how we get to an infinite loop.
            //
            // This would also happen if the test UXML file already existed in the project
            // (because of a previous test not cleaning up, for example).
            //
            // I've plugged all the holes:
            // - BuilderLibraryContent.AssetModificationProcessor.OnAssetChange() uses delayCall
            // - We always try to delete the text UXML file before creating a new one
            // - Added a "timeout" loop safety counter below so we never go infinite
            //
            // But we need to reconsider this pattern and be very careful how we override
            // or new [UnitySetUp] and [SetUp]. I would suggest even just sticking to
            // only using [UnitySetUp] and makeing [SetUp] non-overridable. It's still
            // a big problem either way because if you call base.UnitySetUp() _after_ your
            // new code, you're still going to run into problems.
            int count = 5;
            while (!contentUpdated)
            {
                count--;
                if (count == 0)
                {
                    Assert.Fail("We waited too long for the BuilderLibraryContent to update. Something is very wrong.");
                    break;
                }
                yield return UIETestHelpers.Pause();
            }
        }

        protected override IEnumerator TearDown()
        {
            // Switch back to the controls mode
            if (LibraryPane != null)
                yield return SwitchLibraryTab(BuilderLibrary.BuilderLibraryTab.Controls);

            yield return base.TearDown();
            DeleteTestUXMLFile();
        }

        /// <summary>
        /// Displays project-defined factory elements and UXML files (with `.uxml` extension) under a **Project** heading. This includes assets inside the `Assets/` and `Packages/` folders.
        /// </summary>
        [UnityTest]
        public IEnumerator DisplaysProjectDefinedUXMLFilesInsideAssets()
        {
            yield return SwitchLibraryTab(BuilderLibrary.BuilderLibraryTab.Project);
            var testUXML = GetTestAssetsUXMLFileNode();
            Assert.That(testUXML, Is.Not.Null);
        }

        ITreeViewItem GetTestAssetsUXMLFileNode(string nodeName = k_TestUXMLFileName)
        {
            var libraryTreeView = LibraryPane.Q<TreeView>();
            var projectNode = (BuilderLibraryTreeItem)libraryTreeView.items
                .First(item => ((BuilderLibraryTreeItem)item).Name.Equals(BuilderConstants.LibraryAssetsSectionHeaderName));
            libraryTreeView.ExpandItem(projectNode.id);

            var assetsNode = (BuilderLibraryTreeItem)projectNode.children?.FirstOrDefault();
            if (assetsNode == null || !assetsNode.Name.Equals(BuilderConstants.LibraryAssetsSectionHeaderName))
                return null;

            libraryTreeView.ExpandItem(assetsNode.id);
            var testUXML = assetsNode.children
                .FirstOrDefault(item => ((BuilderLibraryTreeItem)item).Name.Equals(nodeName));
            return testUXML;
        }

        /// <summary>
        /// Can double click to create a new element instance at the root.
        /// </summary>
        [UnityTest]
        public IEnumerator CanDoubleClickToCreateNewElement()
        {
            Assert.That(ViewportPane.documentElement.childCount, Is.EqualTo(0));
            var label = BuilderTestsHelper.GetLabelWithName(LibraryPane, nameof(VisualElement));

            Assert.That(label, Is.Not.Null);
            yield return UIETestEvents.Mouse.SimulateDoubleClick(label);
            Assert.That(ViewportPane.documentElement.childCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Items that have corresponding `.uxml` assets have an "Open" button visible that opens the asset for editing in UI Builder. The currently open `.uxml` asset in the Library is grayed out and is not instantiable to prevent infinite recursion.
        /// </summary>
        [UnityTest, Ignore("If you have enough folders in the Assets folder, the SelectAndScrollToItemWithId() will not work and the test will incorrectly fail.")]
        public IEnumerator UxmlAssetsOpenButtonTest()
        {
            var testUXMLTreeViewItem = GetTestAssetsUXMLFileNode();
            var libraryTreeView = LibraryPane.Q<TreeView>();
            yield return libraryTreeView.SelectAndScrollToItemWithId(testUXMLTreeViewItem.id);
            yield return UIETestHelpers.Pause();
            var testUXMLLabel = libraryTreeView.Query<Label>(null, "unity-builder-library__tree-item-label")
                .Where(label => label.text.Equals(k_TestUXMLFileName)).First();
            Assert.That(testUXMLLabel, Is.Not.Null);
            var treeViewItem = testUXMLLabel.parent;
            var openButton = treeViewItem.Q<Button>();
            Assert.That(openButton, Style.Display(DisplayStyle.Flex));
            Assert.True(openButton.enabledInHierarchy);

            yield return UIETestEvents.Mouse.SimulateClick(openButton);
            Assert.False(openButton.enabledInHierarchy);
        }

        /// <summary>
        /// Can click and drag onto a Viewport element to create new instance as a child. This will also focus the Viewport pane.
        /// </summary>
        /// ///
        /// Instability failure details:
        /* DragOntoViewportElementToCreateNewInstanceAsChild (2.400s)
            ---
            Expected: <BuilderViewport  (x:0.00, y:0.00, width:500.00, height:760.00) world rect: (x:300.00, y:41.00, width:500.00, height:760.00)>
              But was:  null
            ---
            at Unity.UI.Builder.EditorTests.LibraryPaneTests+<DragOntoViewportElementToCreateNewInstanceAsChild>d__7.MoveNext () [0x00138] in C:\Prime\Repos\Builder\Builder2020.1\Packages\com.unity.ui.builder\Tests\Editor\IntegrationTests\LibraryPaneTests.cs:123
            at UnityEngine.TestTools.TestEnumerator+<Execute>d__5.MoveNext () [0x0004c] in C:\Prime\Repos\Builder\Builder2020.1\Library\PackageCache\com.unity.test-framework@1.1.11\UnityEngine.TestRunner\NUnitExtensions\Attributes\TestEnumerator.cs:31
        */
        [UnityTest, Ignore("This is unstable. I got it to fail consistently by just having a floating UI Builder window open at the same time.")]
        public IEnumerator DragOntoViewportElementToCreateNewInstanceAsChild()
        {
            AddElementCodeOnly();
            var documentElement = ViewportPane.documentElement[0];

            var veLabel = BuilderTestsHelper.GetLabelWithName(LibraryPane, nameof(VisualElement));
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                veLabel.worldBound.center,
                documentElement.worldBound.center);

            var hierarchyItems = BuilderTestsHelper.GetExplorerItemsWithName(HierarchyPane, nameof(VisualElement));
            documentElement = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItems[0]);
            var documentElement1 = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItems[1]);

            Assert.That(documentElement1.parent, Is.EqualTo(documentElement));
            Assert.That(BuilderWindow.rootVisualElement.focusController.focusedElement, Is.EqualTo(ViewportPane));
        }

        /// <summary>
        /// Can click and drag onto a Hierarchy element to create new instance as a child, or between elements to create as a sibling.
        /// </summary>
        [UnityTest]
        public IEnumerator DragOntoHierarchyElementToCreateAsChild()
        {
            AddElementCodeOnly();
            yield return UIETestHelpers.Pause();
            var explorerItem = GetFirstExplorerItem();

            var veLabel = BuilderTestsHelper.GetLabelWithName(LibraryPane, nameof(VisualElement));
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                veLabel.worldBound.center,
                explorerItem.worldBound.center);

            var hierarchyItems = BuilderTestsHelper.GetExplorerItemsWithName(HierarchyPane, nameof(VisualElement));
            var documentElement1 = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItems[0]);
            var documentElement2 = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItems[1]);
            Assert.That(documentElement2.parent, Is.EqualTo(documentElement1));
        }

        /// <summary>
        /// Can click and drag onto a Hierarchy element to create new instance as a child, or between elements to create as a sibling.
        /// </summary>
        [UnityTest]
        public IEnumerator DragOntoHierarchyElementToCreateAsSibling()
        {
            AddElementCodeOnly();
            AddElementCodeOnly();

            var firstVisualElementItem = GetFirstExplorerItem();
            yield return SelectLibraryTreeItemWithName("Text Field");
            var textFieldLibrary = BuilderTestsHelper.GetLabelWithName(LibraryPane, "Text Field");

            var veBottomPosition = new Vector2(firstVisualElementItem.worldBound.center.x, firstVisualElementItem.worldBound.yMin);
            yield return UIETestEvents.Mouse.SimulateMouseEvent(BuilderWindow, EventType.MouseDown, textFieldLibrary.worldBound.center);
            yield return UIETestEvents.Mouse.SimulateMouseMove(BuilderWindow, textFieldLibrary.worldBound.center, firstVisualElementItem.worldBound.center);
            yield return UIETestEvents.Mouse.SimulateMouseMove(BuilderWindow, firstVisualElementItem.worldBound.center, veBottomPosition);
            yield return UIETestEvents.Mouse.SimulateMouseEvent(BuilderWindow, EventType.MouseUp, veBottomPosition);

            Assert.That(ViewportPane.documentElement.childCount, Is.EqualTo(3));
            Assert.That(ViewportPane.documentElement[0], Is.TypeOf<TextField>());
        }

        /// <summary>
        /// Can create (double-click or drag) template instances from other `.uxml` files.
        /// </summary>
        [UnityTest, Ignore("If you have enough folders in the Assets folder, the SelectAndScrollToItemWithId() will not work and the test will incorrectly fail.")]
        public IEnumerator CreateTemplateInstancesFromUXML()
        {
            var testUXMLTreeViewItem = GetTestAssetsUXMLFileNode();
            var libraryTreeView = LibraryPane.Q<TreeView>();
            yield return libraryTreeView.SelectAndScrollToItemWithId(testUXMLTreeViewItem.id);
            yield return UIETestHelpers.Pause();
            var testUXMLLabel = libraryTreeView.Query<Label>(null, "unity-builder-library__tree-item-label")
                .Where(label => label.text.Equals(k_TestUXMLFileName)).First();
            Assert.That(testUXMLLabel, Is.Not.Null);

            yield return UIETestEvents.Mouse.SimulateDoubleClick(testUXMLLabel);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                testUXMLLabel.worldBound.center,
                HierarchyPane.worldBound.center);

            Assert.That(ViewportPane.documentElement.childCount, Is.EqualTo(2));
            foreach (var child in ViewportPane.documentElement.Children())
            {
                Assert.That(child, Is.TypeOf<TemplateContainer>());
            }
        }

        /// <summary>
        /// When creating a new empty VisualElement, it has an artificial minimum size and border which is reset as soon as you parent a child element under it or change its styling.
        /// </summary>
        [UnityTest]
        public IEnumerator VisualElementArtificialSizeResetWhenChildIsAdded()
        {
            yield return AddVisualElement();
            var explorerItem = GetFirstExplorerItem();
            var documentElement = GetFirstDocumentElement();

            Assert.That(documentElement[0].classList, Contains.Item(BuilderConstants.SpecialVisualElementInitialMinSizeClassName));

            // Add child.
            var veLabel = BuilderTestsHelper.GetLabelWithName(LibraryPane, nameof(VisualElement));
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                veLabel.worldBound.center,
                explorerItem.worldBound.center);

            Assert.That(documentElement.childCount, Is.EqualTo(1));
            Assert.False(documentElement[0].classList.Contains(BuilderConstants.SpecialVisualElementInitialMinSizeClassName));
        }

        /// <summary>
        /// When creating a new empty VisualElement, it has an artificial minimum size and border which is reset as soon as you parent a child element under it or change its styling.
        /// </summary>
        [UnityTest]
        public IEnumerator VisualElementArtificialSizeResetOnStyleChange()
        {
            yield return AddVisualElement();
            var documentElement = GetFirstDocumentElement();
            Assert.That(documentElement.childCount, Is.EqualTo(1));

            // Change style.
            var displayFoldout = InspectorPane.Query<PersistedFoldout>().Where(f => f.text.Equals("Display")).First();
            displayFoldout.value = true;

            var percentSlider = displayFoldout.Query<PercentSlider>().Where(t => t.label.Equals("Opacity")).First();
            percentSlider.value = 0.5f;

            Assert.That(documentElement.childCount, Is.EqualTo(0));
        }

        /// <summary>
        /// Hovering over items in the Library shows a preview of that element in a floating preview box. The preview uses the current Theme selected for the Canvas.
        /// </summary>
        [UnityTest, Ignore("Let finalize our decision about what kind of items should have a preview")]
        public IEnumerator HoveringOverItemShowsFloatingPreviewBox()
        {
            var veLabel = BuilderTestsHelper.GetLabelWithName(LibraryPane, nameof(VisualElement));
            var preview = BuilderWindow.rootVisualElement.Q<BuilderTooltipPreview>("library-tooltip-preview");
            Assert.That(preview.worldBound.size, Is.EqualTo(Vector2.zero));

            yield return UIETestEvents.Mouse.SimulateMouseEvent(BuilderWindow, EventType.MouseMove, veLabel.worldBound.center);
            yield return UIETestHelpers.Pause();
            Assert.That(preview.worldBound.size, Is.Not.EqualTo(Vector2.zero));
        }

        /// <summary>
        /// Library pane updates if new `.uxml` files are added/deleted/moved/renamed to/from the project.
        /// </summary>
        [UnityTest]
        public IEnumerator LibraryUpdatesWhenUXMLFilesAreAddedDeletedMoved()
        {
            // Switch to the project mode
            yield return SwitchLibraryTab(BuilderLibrary.BuilderLibraryTab.Project);

            // Add
            var uxmlTreeViewItem = GetTestAssetsUXMLFileNode();
            Assert.That(uxmlTreeViewItem, Is.Not.Null);

            // Move
            var contentUpdated = false;
            BuilderLibraryContent.OnLibraryContentUpdated += () =>
            {
                contentUpdated = true;
            };
            AssetDatabase.MoveAsset(k_TestUXMLFilePath, "Assets/NewName.uxml");
            while (!contentUpdated)
                yield return UIETestHelpers.Pause();

            uxmlTreeViewItem = GetTestAssetsUXMLFileNode("NewName.uxml");
            Assert.That(uxmlTreeViewItem, Is.Not.Null);

            // Delete
            contentUpdated = false;
            BuilderLibraryContent.OnLibraryContentUpdated += () =>
            {
                contentUpdated = true;
            };
            AssetDatabase.DeleteAsset("Assets/NewName.uxml");
            while (!contentUpdated)
                yield return UIETestHelpers.Pause();

            yield return UIETestHelpers.Pause();
            uxmlTreeViewItem = GetTestAssetsUXMLFileNode("NewName.uxml");
            Assert.That(uxmlTreeViewItem, Is.Null);
        }

        IEnumerator SwitchLibraryTab(BuilderLibrary.BuilderLibraryTab tabName)
        {
            var controlsViewButton = LibraryPane.Q<Button>(tabName.ToString());
            yield return UIETestEvents.Mouse.SimulateClick(controlsViewButton);
        }

        /// <summary>
        /// Can switch between **Controls** and **Project** view using tabs in the Library header.
        /// </summary>
        [UnityTest]
        public IEnumerator SwitchLibraryBetweenTabs()
        {
            yield return SwitchLibraryTab(BuilderLibrary.BuilderLibraryTab.Controls);
            var controlsNode = FindLibraryItemWithData(BuilderConstants.LibraryControlsSectionHeaderName);
            Assert.That(controlsNode, Is.Not.Null);

            var containersNode = FindLibraryItemWithData(BuilderConstants.LibraryContainersSectionHeaderName);
            Assert.That(containersNode, Is.Not.Null);

            yield return SwitchLibraryTab(BuilderLibrary.BuilderLibraryTab.Project);
            var projectNode = FindLibraryItemWithData(BuilderConstants.LibraryAssetsSectionHeaderName);
            Assert.That(projectNode, Is.Not.Null);
        }

        /// <summary>
        /// Controls view mode can be switched to the tree view representation using **Tree View** option from the `...` options menu in the top right of the Library pane.
        /// </summary>
        [UnityTest]
        public IEnumerator SwitchLibraryBetweenViewModes()
        {
            yield return SwitchLibraryTab(BuilderLibrary.BuilderLibraryTab.Controls);

            LibraryPane.SetViewMode(BuilderLibrary.LibraryViewMode.IconTile);
            yield return UIETestHelpers.Pause();
            Assert.That(LibraryPane.Q<BuilderLibraryPlainView>(), Is.Not.Null);

            LibraryPane.SetViewMode(BuilderLibrary.LibraryViewMode.TreeView);
            yield return UIETestHelpers.Pause();
            Assert.That(LibraryPane.Q<BuilderLibraryTreeView>(), Is.Not.Null);
        }
    }
}