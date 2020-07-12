using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class StyleSheetsPaneTests : BuilderIntegrationTest
    {
        public const string TestSelectorName = ".test";
        public const string TestSelectorName2 = ".test2";

        /// <summary>
        /// Global > Can delete element via Delete key.
        /// Global > Can cut/copy/duplicate/paste element via keyboard shortcut. The copied element and its children are pasted as children of the parent of the currently selected element. If nothing is selected, they are pasted at the root.
        /// </summary>
        ///
        /// Instability failure details:
        /* SelectorCopyPasteDuplicateDelete (1.790s)
            ---
            Expected: 2
              But was:  1
            ---
            at Unity.UI.Builder.EditorTests.StyleSheetsPaneTests+<SelectorCopyPasteDuplicateDelete>d__2.MoveNext () [0x00123] in C:\work\com.unity.ui.builder\Tests\Editor\IntegrationTests\StyleSheetsPaneTests.cs:44
            at UnityEngine.TestTools.TestEnumerator+<Execute>d__5.MoveNext () [0x0004c] in C:\work\1230407\Library\PackageCache\com.unity.test-framework@1.1.13\UnityEngine.TestRunner\NUnitExtensions\Attributes\TestEnumerator.cs:31
        */
        [UnityTest, Ignore("This is unstable. I got it to fail consistently by just having a floating UI Builder window open at the same time.")]
        public IEnumerator SelectorCopyPasteDuplicateDelete()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName);

            var explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(1));

            yield return UIETestEvents.Mouse.SimulateClick(explorerItems[0]);

            // Duplicate
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Duplicate);

            explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(2));

            // Copy
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Copy);
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Paste);

            explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(3));

            var styleSheetElement = BuilderWindow.documentRootElement.parent.Q(k_TestEmptyUSSFileNameNoExt);
            Assert.That(styleSheetElement, Is.Not.Null);
            Assert.That(styleSheetElement.childCount, Is.EqualTo(3));
            Assert.That(
                styleSheetElement.GetProperty(BuilderConstants.ElementLinkedStyleSheetVEPropertyName) as StyleSheet,
                Is.EqualTo(BuilderWindow.document.firstStyleSheet));

            var selectedSelectorElement = styleSheetElement[2];
            var selectedSelector = selectedSelectorElement.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
            Assert.That(selectedSelector, Is.Not.Null);
            Assert.That(Selection.selection.Any(), Is.True);
            Assert.That(Selection.selection.First(), Is.EqualTo(selectedSelectorElement));
            Assert.That(selectedSelectorElement.GetClosestStyleSheet(), Is.EqualTo(BuilderWindow.document.firstStyleSheet));

            // Delete
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(BuilderWindow, KeyCode.Delete);

            yield return UIETestHelpers.Pause(1);
            explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// StyleSheets > With a selector selected, you can use standard short-cuts or the Edit menu to copy/paste/duplicate/delete it. You can also copy/paste the USS for the selector to/from a text file.
        /// </summary>
#if UNITY_2019_2
        [UnityTest, Ignore("Fails on 2019.2 only (but all functionality works when manually doing the same steps). We'll drop 2019.2 support soon anyway.")]
#else
        [UnityTest]
#endif
        public IEnumerator DeleteSelectorViaRightClickMenu()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName);

            var explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(1));

            var panel = BuilderWindow.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            yield return UIETestEvents.Mouse.SimulateClick(explorerItems[0], MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);

            var deleteMenuItem = menu.FindMenuAction("Delete");
            Assert.That(deleteMenuItem, Is.Not.Null);

            deleteMenuItem.Execute();

            yield return UIETestHelpers.Pause(1);

            var newUSSExplorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, TestSelectorName);
            Assert.That(newUSSExplorerItems.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// In the toolbar of the StyleSheets pane there's a field that lets you create new selectors.
        /// 1. After the field is focused, the explanation text is replaced with a default `.`
        /// and the cursor is set right after the `.` to let you quickly add a class-based selector.
        /// 2. You can commit and add your selector to the *active* StyleSheet by pressing **Enter**.
        /// </summary>
        [UnityTest]
        public IEnumerator CreateSelectorFieldBehaviour()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            var createSelectorField = StyleSheetsPane.Q<TextField>();
            createSelectorField.visualInput.Blur();
            Assert.That(createSelectorField.text, Is.EqualTo(BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage));

            createSelectorField.visualInput.Focus();
            Assert.That(createSelectorField.text, Is.EqualTo("."));
            Assert.That(createSelectorField.cursorIndex, Is.EqualTo(1));

            yield return UIETestEvents.KeyBoard.SimulateTyping(BuilderWindow, TestSelectorName);
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(BuilderWindow, KeyCode.Return);
            createSelectorField.visualInput.Blur();

            var newSelector = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, TestSelectorName);
            Assert.That(newSelector, Is.Not.Null);

            createSelectorField.visualInput.Focus();
            yield return UIETestEvents.KeyBoard.SimulateTyping(BuilderWindow, TestSelectorName2);

            var addMenu = StyleSheetsPane.Q<ToolbarMenu>("add-new-selector-menu");
            var addMenuItems = addMenu.menu.MenuItems();
            Assert.AreEqual(addMenuItems.Count, 1);
            var actionMenuItem = addMenuItems[0] as DropdownMenuAction;
            Assert.AreEqual(actionMenuItem.name, k_TestEmptyUSSFileName);
            actionMenuItem.Execute();

            newSelector = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, TestSelectorName2);
            Assert.That(newSelector, Is.Not.Null);
        }

        /// <summary>
        ///  If the selector string contains invalid characters, an error message will display and the new selector will not be created - keeping the focus on the rename field.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectorNameValidation()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            var createSelectorField = StyleSheetsPane.Q<TextField>();
            createSelectorField.visualInput.Focus();
            yield return UIETestEvents.KeyBoard.SimulateTyping(BuilderWindow, "invalid%%selector@$name");
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(BuilderWindow, KeyCode.Return);

            yield return UIETestHelpers.Pause(2);
            Assert.That(createSelectorField.text, Is.EqualTo(".invalid%%selector@$name"));

            // 1 because title is BuilderExplorerItem as well. So 1 means empty in this context
            Assert.That(StyleSheetsPane.Query<BuilderExplorerItem>().ToList().Count, Is.EqualTo(1));

            // Test that we haven't lost field focus and can type valid name.
            yield return UIETestEvents.KeyBoard.SimulateTyping(BuilderWindow, TestSelectorName);
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(BuilderWindow, KeyCode.Return);

            var explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// In the StyleSheets pane, you can select selectors by clicking on the row or a style class pill.
        /// </summary>
#if UNITY_2019_2
        [UnityTest, Ignore("Fails on 2019.2 only (but all functionality works when manually doing the same steps). We'll drop 2019.2 support soon anyway.")]
#else
        [UnityTest]
#endif
        public IEnumerator SelectSelectorWithRowAndPillClick()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName);
            var stylesTreeView = StyleSheetsPane.Q<TreeView>();

            Assert.That(stylesTreeView.GetSelectedItem(), Is.Null);

            //Select by clicking on the row
            var createdSelector = GetStyleSelectorNodeWithName(TestSelectorName);
            yield return UIETestEvents.Mouse.SimulateClick(createdSelector);
            Assert.That(stylesTreeView.GetSelectedItem(), Is.Not.Null);

            //Deselect
            yield return UIETestEvents.Mouse.SimulateClick(StyleSheetsPane);
            Assert.That(stylesTreeView.GetSelectedItem(), Is.Null);

            //Select by clicking on the style class pill
            yield return UIETestEvents.Mouse.SimulateClick(createdSelector.Q<Label>());
            Assert.That(stylesTreeView.GetSelectedItem(), Is.Not.Null);
        }

        /// <summary>
        /// Can drag a style class pill from the StyleSheets pane onto an element in the Viewport to add the class.
        /// Selectors get draggable style class pills for each selector part that is a style class name.
        /// </summary>
        [UnityTest]
        public IEnumerator DragStylePillToViewport()
        {
            AddElementCodeOnly<TextField>();

            // Ensure we can add selectors.
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName + " " + TestSelectorName2);
            var createdSelector = GetStyleSelectorNodeWithName(TestSelectorName);

            // Now it's save to get a reference to an element in the canvas.
            var documentElement = GetFirstDocumentElement();

            yield return UIETestHelpers.Pause(1);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                createdSelector.Q<Label>().worldBound.center,
                documentElement.worldBound.center);

            var currentClassCount = documentElement.classList.Count;
            Assert.That(documentElement.classList, Contains.Item(TestSelectorName.TrimStart('.')));

            var secondClassNameLabel = BuilderTestsHelper.GetLabelWithName(createdSelector, TestSelectorName2);
            yield return UIETestHelpers.Pause(100);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                secondClassNameLabel.worldBound.center,
                documentElement.worldBound.center);

            Assert.That(documentElement.classList.Count, Is.EqualTo(currentClassCount + 1));
            Assert.That(documentElement.classList, Contains.Item(TestSelectorName2.TrimStart('.')));
        }

        /// <summary>
        /// Can drag a style class pill from the StyleSheets pane onto an element in the Hierarchy to add the class.
        /// </summary>
#if UNITY_2019_2
        [UnityTest, Ignore("Fails on 2019.2 only (but all functionality works when manually doing the same steps). We'll drop 2019.2 support soon anyway.")]
#else
        [UnityTest]
#endif
        public IEnumerator DragStylePillToHierarchy()
        {
            AddElementCodeOnly();

            // Ensure we can add selectors.
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName);
            var createdSelector = GetStyleSelectorNodeWithName(TestSelectorName);

            var hierarchyCreatedItem = GetFirstExplorerVisualElementNode(nameof(VisualElement));

            yield return UIETestHelpers.Pause(1);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                createdSelector.Q<Label>().worldBound.center,
                hierarchyCreatedItem.worldBound.center);

            var documentElement =
                (VisualElement) hierarchyCreatedItem.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName);

            Assert.That(documentElement.classList.Count, Is.EqualTo(1));
            Assert.That(documentElement.classList[0], Is.EqualTo(TestSelectorName.TrimStart('.')));
        }

        /// <summary>
        /// Dragging a style class onto an element inside a template instance or C# type in the Viewport adds it to the parent instance or C# element.
        /// </summary>
        [UnityTest]
        public IEnumerator DragStylePillOntoTemplateElementInViewport()
        {
            AddElementCodeOnly<TextField>();

            // Ensure we can add selectors.
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName);
            var createdSelector = GetStyleSelectorNodeWithName(TestSelectorName);

            // Now it's safe to get a reference to an element in the canvas.
            var documentElement = GetFirstDocumentElement();

            yield return UIETestHelpers.Pause(1);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                createdSelector.Q<Label>().worldBound.center,
                documentElement.worldBound.center);

            yield return UIETestHelpers.Pause(1);
            Assert.That(documentElement.classList, Contains.Item(TestSelectorName.TrimStart('.')));
        }

        /// <summary>
        /// Dragging a style class onto an element inside a template instance or C# type in the Hierarchy does nothing.
        /// </summary>
        [UnityTest]
        public IEnumerator DragStylePillOntoTemplateElementInHierarchy()
        {
            AddElementCodeOnly<TextField>();

            // Ensure we can add selectors.
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            yield return AddSelector(TestSelectorName);
            var createdSelector = GetStyleSelectorNodeWithName(TestSelectorName);

            yield return UIETestHelpers.Pause(1);
            var hierarchyTreeView = HierarchyPane.Q<TreeView>();
            hierarchyTreeView.ExpandItem(hierarchyTreeView.items.ToList()[1].id);

            var textFieldLabel = BuilderTestsHelper.GetExplorerItemWithName(HierarchyPane, nameof(Label)).Q<Label>();

            yield return UIETestHelpers.Pause(1);
            yield return UIETestEvents.Mouse.SimulateDragAndDrop(BuilderWindow,
                createdSelector.Q<Label>().worldBound.center,
                  textFieldLabel.worldBound.center);

            var documentElement = GetFirstDocumentElement();
            Assert.That(documentElement.classList, Is.Not.Contain(TestSelectorName.TrimStart('.')));
        }

        /// <summary>
        ///  While the text field is selected, you should see a large tooltip displaying the selector cheatsheet.
        /// </summary>
        [Test]
        public void SelectorCheatsheetTooltip()
        {
            var builderTooltipPreview = BuilderWindow.rootVisualElement.Q<BuilderTooltipPreview>("stylesheets-pane-tooltip-preview");
            var builderTooltipPreviewEnabler =
                builderTooltipPreview.Q<VisualElement>(BuilderTooltipPreview.s_EnabledElementName);

            var createSelectorField = StyleSheetsPane.Q<TextField>("new-selector-field");

            // Everything StyleSheet is disabled now if there are no elements to contain the <Style> tag.
            Assert.That(createSelectorField.enabledInHierarchy, Is.False);
            AddElementCodeOnly("TestElement");
            Assert.That(createSelectorField.enabledInHierarchy, Is.True);

            createSelectorField.visualInput.Focus();
            Assert.That(builderTooltipPreviewEnabler, Style.Display(DisplayStyle.Flex));

            createSelectorField.visualInput.Blur();
            Assert.That(builderTooltipPreviewEnabler, Style.Display(DisplayStyle.None));
        }

        // TODO: Convert to block-comment.
        readonly string m_ExpectedSelectorString
            = WebUtility.UrlDecode($"{TestSelectorName}%20%7B%0A%20%20%20%20display:%20flex;%0A%20%20%20%20visibility:%20hidden;%0A%7D%0A");

        /// <summary>
        ///  With a selector selected, you can use standard short-cuts or the Edit menu to copy/paste/duplicate/delete it. You can also copy/paste the USS for the selector to/from a text file.
        /// </summary>
#if UNITY_2019_2
        [UnityTest, Ignore("Fails on 2019.2 only (but all functionality works when manually doing the same steps). We'll drop 2019.2 support soon anyway.")]
#else
        [UnityTest]
#endif
        public IEnumerator SelectorToAndFromUSSConversion()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            // Create and new selector and select
            yield return AddSelector(TestSelectorName);
            var selector = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, TestSelectorName);
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            // Set style
            var displayFoldout = InspectorPane.Query<PersistedFoldout>().Where(f => f.text.Equals("Display")).First();
            displayFoldout.value = true;

            var displayStrip = displayFoldout.Query<ToggleButtonStrip>().Where(t => t.label.Equals("Display")).First();
            yield return UIETestEvents.Mouse.SimulateClick(displayStrip.Q<Button>("flex"));

            var visibilityStrip = displayFoldout.Query<ToggleButtonStrip>().Where(t => t.label.Equals("Visibility")).First();
            yield return UIETestEvents.Mouse.SimulateClick(visibilityStrip.Q<Button>("hidden"));
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            var newlineFixedExpectedUSS = m_ExpectedSelectorString;
            if (BuilderConstants.NewlineChar != BuilderConstants.NewlineCharFromEditorSettings)
                newlineFixedExpectedUSS = newlineFixedExpectedUSS.Replace(
                    BuilderConstants.NewlineChar,
                    BuilderConstants.NewlineCharFromEditorSettings);

            // Copy to USS
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Copy);
            Assert.That(BuilderEditorUtility.SystemCopyBuffer, Is.EqualTo(newlineFixedExpectedUSS));

            // Paste from USS
            ForceNewDocument();
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();
            BuilderEditorUtility.SystemCopyBuffer = string.Empty;
            yield return UIETestEvents.Mouse.SimulateClick(StyleSheetsPane);
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Paste);
            var explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(0));

            BuilderEditorUtility.SystemCopyBuffer = newlineFixedExpectedUSS;
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Paste);
            explorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, TestSelectorName);
            Assert.That(explorerItems.Count, Is.EqualTo(1));

            // Foldout out state should be persisted, so we assume it is open already.
            displayFoldout = InspectorPane.Query<PersistedFoldout>().Where(f => f.text.Equals("Display")).First();
            displayStrip = displayFoldout.Query<ToggleButtonStrip>().Where(t => t.label.Equals("Display")).First();
            Assert.True(displayStrip.Q<Button>("flex").pseudoStates.HasFlag(PseudoStates.Checked));

            visibilityStrip = displayFoldout.Query<ToggleButtonStrip>().Where(t => t.label.Equals("Visibility")).First();
            Assert.True(visibilityStrip.Q<Button>("hidden").pseudoStates.HasFlag(PseudoStates.Checked));
        }

        /// <summary>
        ///  Selecting an element or a the main document (VisualTreeAsset) should deselect any selected tree items in the StyleSheets pane.
        /// </summary>
#if UNITY_2019_2
        [UnityTest, Ignore("Fails on 2019.2 only (but all functionality works when manually doing the same steps). We'll drop 2019.2 support soon anyway.")]
#else
        [UnityTest]
#endif
        public IEnumerator StyleSheetsItemsDeselect()
        {
            yield return EnsureSelectorsCanBeAddedAndReloadBuilder();

            var styleSheetsTreeView = StyleSheetsPane.Q<TreeView>();
            Assert.That(styleSheetsTreeView.GetSelectedItem(), Is.Null);

            // Create and new selector and select
            yield return AddSelector(TestSelectorName);
            var selector = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, TestSelectorName);
            yield return UIETestEvents.Mouse.SimulateClick(selector);

            Assert.That(styleSheetsTreeView.GetSelectedItem(), Is.Not.Null);

            AddElementCodeOnly();
            var documentElement = GetFirstDocumentElement();
            yield return UIETestEvents.Mouse.SimulateClick(documentElement);
            Assert.That(styleSheetsTreeView.GetSelectedItem(), Is.Null);
        }
    }
}