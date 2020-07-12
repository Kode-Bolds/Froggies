using System;
using System.Collections;
using System.Linq;
using System.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class HierarchyPaneTests : BuilderIntegrationTest
    {
        /// <summary>
        /// Can click to select an element.
        /// </summary>
        [UnityTest]
        public IEnumerator ClickToSelect()
        {
            const string testElementName = "test_element_name";
            AddElementCodeOnly<TextField>(testElementName);
            Selection.ClearSelection(null);

            yield return UIETestHelpers.Pause();
            var hierarchyCreatedItem = GetHierarchyExplorerItemByElementName(testElementName);
            Assert.That(hierarchyCreatedItem, Is.Not.Null);

            var hierarchyTreeView = HierarchyPane.Q<TreeView>();
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Null);
            Assert.That(Selection.isEmpty, Is.True);

            yield return UIETestEvents.Mouse.SimulateClick(hierarchyCreatedItem);
            var documentElement = GetFirstDocumentElement();
            Assert.That(documentElement.name, Is.EqualTo(testElementName));

            var selectedItem = (TreeViewItem<VisualElement>) hierarchyTreeView.GetSelectedItem();
            Assert.That(documentElement, Is.EqualTo(selectedItem.data));
            Assert.That(Selection.selection.First(), Is.EqualTo(documentElement));
        }

        /// <summary>
        /// Can drag element onto other elements in the Hierarchy to re-parent.
        /// </summary>
        [UnityTest]
        public IEnumerator DragToReparentInHierarchy()
        {
            AddElementCodeOnly();
            AddElementCodeOnly();
            yield return UIETestHelpers.Pause();

            var documentElement1 = ViewportPane.documentElement[0];
            var documentElement2 = ViewportPane.documentElement[1];

            var hierarchyItems = BuilderTestsHelper.GetExplorerItemsWithName(HierarchyPane, nameof(VisualElement));
            var hierarchyItem1 = hierarchyItems[0];
            var hierarchyItem2 = hierarchyItems[1];

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                hierarchyItem1.worldBound.center,
                hierarchyItem2.worldBound.center);

            yield return null;
            Assert.That(ViewportPane.documentElement.childCount, Is.EqualTo(1));
            Assert.That(documentElement2.parent, Is.EqualTo(ViewportPane.documentElement));
            Assert.That(documentElement1.parent, Is.EqualTo(documentElement2));
        }

        /// <summary>
        /// Can drag an element onto other elements in the Viewport to re-parent.
        /// </summary>
        [UnityTest, Ignore("Remove ignore once reparenting bug is fixed.")]
        public IEnumerator DragToReparentInViewport()
        {
            AddElementCodeOnly();
            AddElementCodeOnly();

            var documentElement1 = ViewportPane.documentElement[0];
            var documentElement2 = ViewportPane.documentElement[1];

            var hierarchyItems = BuilderTestsHelper.GetExplorerItemsWithName(HierarchyPane, nameof(VisualElement));
            var hierarchyItem1 = hierarchyItems[0];

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                hierarchyItem1.worldBound.center,
                documentElement2.worldBound.center);

            yield return UIETestHelpers.Pause();
            Assert.That(ViewportPane.documentElement.childCount, Is.EqualTo(1));
            Assert.That(documentElement2.parent, Is.EqualTo(ViewportPane.documentElement));
            Assert.That(documentElement1.parent, Is.EqualTo(documentElement2));
        }

        /// <summary>
        /// Can drag an element between other elements to reorder, with live preview in the Canvas.
        /// </summary>
        [UnityTest]
        public IEnumerator DragBetweenAndLivePreview()
        {
            AddElementCodeOnly();
            AddElementCodeOnly();
            AddElementCodeOnly<TextField>();
            yield return UIETestHelpers.Pause();

            var textFieldCanvas = ViewportPane.documentElement[2];
            var firstVisualElementHierarchy = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(VisualElement));
            var textFieldHierarchy = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(TextField));

            Assert.That(ViewportPane.documentElement.IndexOf(textFieldCanvas), Is.EqualTo(2));

            yield return UIETestEvents.Mouse.SimulateMouseEvent(BuilderWindow, EventType.MouseDown, textFieldHierarchy.worldBound.center);
            var textFieldCenter = textFieldHierarchy.worldBound.center;
            var veBottomPosition =  new Vector2(textFieldCenter.x, firstVisualElementHierarchy.worldBound.yMax);
            yield return UIETestEvents.Mouse.SimulateMouseMove(BuilderWindow, textFieldCenter, veBottomPosition);

            Assert.That(ViewportPane.documentElement.IndexOf(textFieldCanvas), Is.EqualTo(1));
            yield return UIETestEvents.Mouse.SimulateMouseEvent(BuilderWindow, EventType.MouseUp, veBottomPosition);
        }

        /// <summary>
        /// Elements are displayed using their #name in blue. If they have no name, they are displayed using their C# type in white.
        /// Can double-click on an item to rename it.
        /// During element rename, if new name is not valid, an error message will display and rename will not be applied - keeping the focus on the rename field.
        /// </summary>
        ///
        /// Instability failure details:
        /* DisplayNameStyleAndRenameOption (1.119s)
            ---
            Expected string length 9 but was 0. Strings differ at index 0.
              Expected: "test_name"
              But was:  <string.Empty>
              -----------^
            ---
            at Unity.UI.Builder.EditorTests.HierarchyPaneTests+<DisplayNameStyleAndRenameOption>d__4.MoveNext ()[0x0016c] in C:\Prime\Repos\Builder\Builder2020.1\Packages\com.unity.ui.builder\Tests\Editor\IntegrationTests\HierarchyPaneTests.cs:131
            at UnityEngine.TestTools.TestEnumerator+<Execute>d__5.MoveNext ()[0x0004c] in C:\Prime\Repos\Builder\Builder2020.1\Library\PackageCache\com.unity.test-framework@1.1.11\UnityEngine.TestRunner\NUnitExtensions\Attributes\TestEnumerator.cs:31
        */
        [UnityTest, Ignore("This is unstable. I got it to fail consistently by just having a floating UI Builder window open at the same time.")]
        public IEnumerator DisplayNameStyleAndRenameOption()
        {
            const string testItemName = "test_name";
            AddElementCodeOnly();
            var hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(VisualElement));
            var documentElement = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItem);
            var nameLabel = hierarchyItem.Q<Label>(className: BuilderConstants.ExplorerItemLabelClassName);

            Assert.That(nameLabel.text, Is.EqualTo(nameof(VisualElement)));
            Assert.That(nameLabel.classList, Contains.Item(BuilderConstants.ElementTypeClassName));

            yield return UIETestEvents.Mouse.SimulateDoubleClick(hierarchyItem);
            yield return UIETestEvents.KeyBoard.SimulateTyping(BuilderWindow, testItemName);
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(BuilderWindow, KeyCode.Return);

            Assert.That(documentElement.name, Is.EqualTo(testItemName));

            hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, BuilderConstants.UssSelectorNameSymbol + testItemName);
            nameLabel =  hierarchyItem.Q<Label>(className: BuilderConstants.ExplorerItemLabelClassName);
            Assert.That(nameLabel.classList, Contains.Item(BuilderConstants.ElementNameClassName));

            hierarchyItem = GetFirstExplorerItem();
            yield return UIETestEvents.Mouse.SimulateDoubleClick(hierarchyItem);
            yield return UIETestEvents.KeyBoard.SimulateTyping(BuilderWindow, "invalid&name");
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(BuilderWindow, KeyCode.Return);
            Assert.That(documentElement.name, Is.EqualTo(testItemName));
        }

        /// <summary>
        /// When editing name of element in Hierarchy, clicking somewhere else will commit the change (if the new name is valid).
        /// </summary>
        [UnityTest]
        public IEnumerator OutsideClickWillCommitRename()
        {
            const string testItemName = "test_name";
            AddElementCodeOnly();
            var hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(VisualElement));
            var documentElement = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItem);
            var nameLabel = hierarchyItem.Q<Label>(className: BuilderConstants.ExplorerItemLabelClassName);

            Assert.That(nameLabel.text, Is.EqualTo(nameof(VisualElement)));
            Assert.That(nameLabel.classList, Contains.Item(BuilderConstants.ElementTypeClassName));

            yield return UIETestEvents.Mouse.SimulateDoubleClick(hierarchyItem);
            yield return UIETestEvents.KeyBoard.SimulateTyping(BuilderWindow, testItemName);
            yield return UIETestEvents.Mouse.SimulateClick(ViewportPane);

            Assert.That(documentElement.name, Is.EqualTo(testItemName));

            hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, BuilderConstants.UssSelectorNameSymbol + testItemName);
            nameLabel =  hierarchyItem.Q<Label>(className: BuilderConstants.ExplorerItemLabelClassName);
            Assert.That(nameLabel.classList, Contains.Item(BuilderConstants.ElementNameClassName));
        }

        /// <summary>
        /// When editing name of element in Hierarchy, hitting the Esc key will cancel the edit and revert to value before the edit started.
        /// </summary>
        [UnityTest]
        public IEnumerator EscKeyWillCancelRename()
        {
            const string testItemName = "test_name";
            AddElementCodeOnly();
            var hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(VisualElement));
            var documentElement = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItem);
            Assert.That(string.IsNullOrEmpty(documentElement.name));

            yield return UIETestEvents.Mouse.SimulateDoubleClick(hierarchyItem);
            yield return UIETestEvents.KeyBoard.SimulateTyping(BuilderWindow, testItemName);
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(BuilderWindow, KeyCode.Escape);

            // Test that not only the name has not changed to the new value entered...
            hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(VisualElement));
            documentElement = BuilderTestsHelper.GetLinkedDocumentElement(hierarchyItem);
            Assert.AreNotEqual(documentElement.name, testItemName);
            // But is also equal to its original name
            Assert.That(string.IsNullOrEmpty(documentElement.name));
        }

        /// <summary>
        /// Elements are displayed grayed out if they are children of a template instance or C# type.
        /// </summary>
        [UnityTest]
        public IEnumerator CSharpTypeTemplateChildrenMustBeGrayedOutAndNotEditable()
        {
            AddElementCodeOnly<TextField>();
            var hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(TextField));
            yield return UIETestHelpers.ExpandTreeViewItem(hierarchyItem);

            var textFieldDocumentElement = GetFirstDocumentElement();
            Assert.That(textFieldDocumentElement.childCount, Is.GreaterThan(0));
            BuilderExplorerItem lastChild = null;
            foreach (var child in textFieldDocumentElement.Children())
            {
                lastChild = BuilderTestsHelper.GetLinkedExplorerItem(child);
                Assert.That(lastChild.row().classList, Contains.Item(BuilderConstants.ExplorerItemHiddenClassName));
            }

            yield return UIETestEvents.Mouse.SimulateClick(lastChild);
            InspectorPane.Query<ToggleButtonStrip>().ForEach(toggleButtonStrip =>
            {
                Assert.That(toggleButtonStrip.enabledInHierarchy, Is.False);
            });

            InspectorPane.Query<PercentSlider>().ForEach(percentSlider =>
            {
                Assert.That(percentSlider.enabledInHierarchy, Is.False);
            });
        }

        /// <summary>
        /// Selecting an style selector or a the main StyleSheet in the StyleSheets pane should deselect any selected tree items in the Hierarchy.
        /// </summary>
#if UNITY_2019_2
        [UnityTest, Ignore("Fails on 2019.2 only (but all functionality works when manually doing the same steps). We'll drop 2019.2 support soon anyway.")]
#else
        [UnityTest]
#endif
        public IEnumerator SelectingStyleSelectorOrStyleSheetDeselectsHierarchyItems()
        {
            AddElementCodeOnly();
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();
            yield return AddSelector(StyleSheetsPaneTests.TestSelectorName);

            // Deselect
            yield return UIETestEvents.Mouse.SimulateClick(HierarchyPane);
            var hierarchyTreeView = HierarchyPane.Q<TreeView>();
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Null);

            // Select hierarchy item
            var hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(VisualElement));
            yield return UIETestEvents.Mouse.SimulateClick(hierarchyItem);
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Not.Null);

            // Select test selector
            var selector = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, StyleSheetsPaneTests.TestSelectorName);
            yield return UIETestEvents.Mouse.SimulateClick(selector);
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Null);

            // Select hierarchy item
            yield return UIETestEvents.Mouse.SimulateClick(hierarchyItem);
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Not.Null);

            // Select Uss file name header
            var header = BuilderTestsHelper.GetHeaderItem(StyleSheetsPane);
            yield return UIETestEvents.Mouse.SimulateClick(header);
            Assert.That(hierarchyTreeView.GetSelectedItem(), Is.Null);
        }

        readonly string m_ExpectedUXMLString
            = WebUtility.UrlDecode("%3Cui%3AUXML+xmlns%3Aui%3D%22UnityEngine.UIElements%22+xmlns%3Auie%3D%22UnityEditor.UIElements%22%3E%0A++++%3Cui%3AVisualElement%3E%0A++++++++%3Cui%3AVisualElement+%2F%3E%0A++++%3C%2Fui%3AVisualElement%3E%0A%3C%2Fui%3AUXML%3E%0A");

        /// <summary>
        /// Can copy/paste the UXML for the element to/from a text file.
        /// </summary>
        [UnityTest]
        public IEnumerator CopyPasteUXML()
        {
            AddElementCodeOnly();
            AddElementCodeOnly();
            yield return  UIETestHelpers.Pause();

            var hierarchyItems = BuilderTestsHelper.GetExplorerItemsWithName(HierarchyPane, nameof(VisualElement));
            var hierarchyItem1 = hierarchyItems[0];
            var hierarchyItem2 = hierarchyItems[1];

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                hierarchyItem1.worldBound.center,
                hierarchyItem2.worldBound.center);

            var complexItem =  GetFirstExplorerItem();

            var newlineFixedExpectedUXML = m_ExpectedUXMLString;
            if (BuilderConstants.NewlineChar != BuilderConstants.NewlineCharFromEditorSettings)
                newlineFixedExpectedUXML = newlineFixedExpectedUXML.Replace(
                    BuilderConstants.NewlineChar,
                    BuilderConstants.NewlineCharFromEditorSettings);

            // Copy to UXML
            yield return UIETestEvents.Mouse.SimulateClick(complexItem);
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Copy);
            Assert.That(BuilderEditorUtility.SystemCopyBuffer, Is.EqualTo(newlineFixedExpectedUXML));

            ForceNewDocument();
            BuilderEditorUtility.SystemCopyBuffer = string.Empty;
            yield return UIETestEvents.Mouse.SimulateClick(HierarchyPane);
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Paste);
            var explorerItems = BuilderTestsHelper.GetExplorerItems(HierarchyPane);
            Assert.That(explorerItems, Is.Empty);

            BuilderEditorUtility.SystemCopyBuffer = newlineFixedExpectedUXML;
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Paste);
            // var newItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(VisualElement));
            var hierarchyTreeView = HierarchyPane.Q<TreeView>();
            hierarchyTreeView.ExpandItem(hierarchyTreeView.items.ToList()[1].id);

            explorerItems = BuilderTestsHelper.GetExplorerItems(HierarchyPane);
            Assert.That(explorerItems.Count, Is.EqualTo(2));
            Assert.That(BuilderTestsHelper.GetLinkedDocumentElement(explorerItems[1]).parent, Is.EqualTo(BuilderTestsHelper.GetLinkedDocumentElement(explorerItems[0])));
        }

        /// <summary>
        /// Dragging an element onto a template instance or C# type element in the Viewport re-parents it to the parent instance or C# element.
        /// Dragging an element onto a template instance or C# type element in the Hierarchy re-parents it to the parent instance or C# element.
        /// </summary>
        [UnityTest]
        public IEnumerator ReparentFlowWhenDraggingOntoCSharpTypeElement()
        {
            AddElementCodeOnly<TextField>();
            AddElementCodeOnly();
            yield return UIETestHelpers.Pause();

            var textFieldItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(TextField));
            var visualElementItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(VisualElement));
            var visualElementDocItem = BuilderTestsHelper.GetLinkedDocumentElement(visualElementItem);

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                visualElementItem.worldBound.center,
                textFieldItem.worldBound.center);
            Assert.That(visualElementDocItem.parent, Is.InstanceOf<TextField>());

            ForceNewDocument();
            AddElementCodeOnly<TextField>();
            AddElementCodeOnly();
            yield return UIETestHelpers.Pause();

            textFieldItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(TextField));
            visualElementItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(VisualElement));
            visualElementDocItem = BuilderTestsHelper.GetLinkedDocumentElement(visualElementItem);
            var textFieldDocItem = BuilderTestsHelper.GetLinkedDocumentElement(textFieldItem);

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                visualElementItem.worldBound.center,
                textFieldDocItem.worldBound.center);
            Assert.That(visualElementDocItem.parent, Is.InstanceOf<TextField>());
        }

        /// <summary>
        /// Dragging child elements of a template instance or C# type element within the element or outside does not work.
        /// </summary>
        [UnityTest]
        public IEnumerator DraggingChildElementsOfATemplateShouldNotWork()
        {
            AddElementCodeOnly<TextField>();
            AddElementCodeOnly();

            var hierarchyItem = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(TextField));
            yield return UIETestHelpers.ExpandTreeViewItem(hierarchyItem);

            yield return UIETestHelpers.Pause();
            var textField = ViewportPane.documentElement[0];
            var textFieldLabel = textField.Q<Label>();
            var visualElement = ViewportPane.documentElement[1];
            var textFieldLabelExplorer  = BuilderTestsHelper.GetLinkedExplorerItem(textFieldLabel);
            var visualElementExplorer  = BuilderTestsHelper.GetLinkedExplorerItem(visualElement);

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                visualElementExplorer.worldBound.center,
                textFieldLabelExplorer.worldBound.center);
            Assert.That(visualElement.parent, Is.EqualTo(ViewportPane.documentElement));

            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                textFieldLabelExplorer.worldBound.center,
                visualElementExplorer.worldBound.center);
            Assert.That(textFieldLabel.parent, Is.EqualTo(textField));
        }

        /// <summary>
        /// With an element selected, you can use the standard short-cuts and Edit menu to copy/paste/duplicate/delete it.  The copied element is pasted at the same level of the hierarchy as the source element. If the source element's parent is deleted, the copied element is pasted at the root.
        /// </summary>
        ///
        /// Instability failure details:
        /* StandardShortCuts (1.280s)
            ---
            Expected: 2
              But was:  1
            ---
            at Unity.UI.Builder.EditorTests.HierarchyPaneTests+<StandardShortCuts>d__12.MoveNext () [0x0011d] in C:\Prime\Repos\Builder\Builder2020.1\Packages\com.unity.ui.builder\Tests\Editor\IntegrationTests\HierarchyPaneTests.cs:358
            at UnityEngine.TestTools.TestEnumerator+<Execute>d__5.MoveNext () [0x0004c] in C:\Prime\Repos\Builder\Builder2020.1\Library\PackageCache\com.unity.test-framework@1.1.11\UnityEngine.TestRunner\NUnitExtensions\Attributes\TestEnumerator.cs:31
        */
        [UnityTest, Ignore("This is unstable. I got it to fail consistently by just having a floating UI Builder window open at the same time.")]
        public IEnumerator StandardShortCuts()
        {
            yield return AddVisualElement();

            var explorerItems = BuilderTestsHelper.GetExplorerItems(HierarchyPane);
            Assert.That(explorerItems.Count, Is.EqualTo(1));
            yield return UIETestEvents.Mouse.SimulateClick(explorerItems[0]);

            // Rename
            const string renameString = "renameString";
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Rename);
            yield return UIETestEvents.KeyBoard.SimulateTyping(BuilderWindow, renameString);
            yield return UIETestEvents.Mouse.SimulateClick(ViewportPane);

            explorerItems = BuilderTestsHelper.GetExplorerItems(HierarchyPane);
            var explorerItemLabel = explorerItems[0].Q<Label>();
            Assert.That(explorerItemLabel.text, Is.EqualTo("#" + renameString));

            yield return UIETestEvents.Mouse.SimulateClick(explorerItems[0]);

            // Duplicate
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Duplicate);
            explorerItems = BuilderTestsHelper.GetExplorerItems(HierarchyPane);
            Assert.That(explorerItems.Count, Is.EqualTo(2));

            // Copy/Paste
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Copy);
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Paste);

            explorerItems = BuilderTestsHelper.GetExplorerItems(HierarchyPane);
            Assert.That(explorerItems.Count, Is.EqualTo(3));

            // Delete
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(BuilderWindow, KeyCode.Delete);

            explorerItems = BuilderTestsHelper.GetExplorerItems(HierarchyPane);
            Assert.That(explorerItems.Count, Is.EqualTo(2));

            // Pasted as children of the parent of the currently selected element.

            AddElementCodeOnly<TextField>();
            var textField = ViewportPane.documentElement.Q<TextField>();
            Assert.That(textField.childCount, Is.EqualTo(2));

            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Paste);
            Assert.That(textField.childCount, Is.EqualTo(2));
        }
    }
}