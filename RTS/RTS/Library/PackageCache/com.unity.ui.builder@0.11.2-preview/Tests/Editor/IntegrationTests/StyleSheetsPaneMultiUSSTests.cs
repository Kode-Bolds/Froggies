using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class StyleSheetsPaneMultiUSSTests : BuilderIntegrationTest
    {
        const string k_ColorsTestUSSFileNameNoExt = "ColorsTestStyleSheet";
        const string k_LayoutTestUSSFileNameNoExt = "LayoutTestStyleSheet";

        const string k_ColorsTestUSSFileName = k_ColorsTestUSSFileNameNoExt + ".uss";
        const string k_LayoutTestUSSFileName = k_LayoutTestUSSFileNameNoExt + ".uss";

        const string k_ColorsTestUSSPath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_ColorsTestUSSFileName;
        const string k_LayoutTestUSSPath = BuilderConstants.UIBuilderTestsTestFilesPath + "/" + k_LayoutTestUSSFileName;

        protected override IEnumerator TearDown()
        {
            BuilderStyleSheetsUtilities.RestoreTestCallbacks();

            yield return base.TearDown();
            AssetDatabase.DeleteAsset(k_TestUSSFilePath);
        }

        StyleSheet GetStyleSheetFromExplorerItem(VisualElement explorerItem, string ussPath)
        {
            Assert.That(explorerItem, Is.Not.Null);
            var documentElement = explorerItem.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;
            Assert.That(documentElement, Is.Not.Null);
            var styleSheet = documentElement.GetStyleSheet();
            Assert.That(styleSheet, Is.Not.Null);
            var styleSheetPath = AssetDatabase.GetAssetPath(styleSheet);
            Assert.That(styleSheetPath, Is.EqualTo(ussPath));

            return styleSheet;
        }

        /// <summary>
        /// If there is no USS in document, the Save Dialog Option to create a new USS file will be prompted and selector will be added to the newly created and added USS file.
        /// </summary>
        [UnityTest]
        public IEnumerator NewSelectorWithNoUSSCreatesNewUSS()
        {
            AddElementCodeOnly("TestElement");

            var createSelectorField = StyleSheetsPane.Q<TextField>();
            createSelectorField.visualInput.Blur();
            Assert.That(createSelectorField.text, Is.EqualTo(BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage));

            createSelectorField.visualInput.Focus();
            Assert.That(createSelectorField.text, Is.EqualTo("."));
            Assert.That(createSelectorField.cursorIndex, Is.EqualTo(1));

            bool hasSaveDialogBeenOpened = false;
            BuilderStyleSheetsUtilities.s_SaveFileDialogCallback = () =>
            {
                hasSaveDialogBeenOpened = true;
                return k_TestUSSFilePath;
            };

            yield return UIETestEvents.KeyBoard.SimulateTyping(BuilderWindow, ".new-selector");
            yield return UIETestEvents.KeyBoard.SimulateKeyDown(BuilderWindow, KeyCode.Return);
            Assert.That(hasSaveDialogBeenOpened, Is.True);

            yield return UIETestHelpers.Pause(1);

            var unityButtonSelectors = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, ".new-selector");
            Assert.That(unityButtonSelectors.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Right-clicking anywhere in the TreeView should display the standard copy/paste/duplicate/delete menu with the additional options to:
        /// </summary>
        [UnityTest]
        public IEnumerator RightClickingInStyleSheetsPaneOpensMenu()
        {
            AddElementCodeOnly("TestElement");

            var panel = BuilderWindow.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            yield return UIETestEvents.Mouse.SimulateClick(StyleSheetsPane, MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);

            var newUSS = menu.FindMenuAction(BuilderConstants.ExplorerStyleSheetsPaneCreateNewUSSMenu);
            var existingUSS = menu.FindMenuAction(BuilderConstants.ExplorerStyleSheetsPaneAddExistingUSSMenu);
            var removeUSS = menu.FindMenuAction(BuilderConstants.ExplorerStyleSheetsPaneRemoveUSSMenu);

            Assert.That(newUSS, Is.Not.Null);
            Assert.That(existingUSS, Is.Not.Null);
            Assert.That(removeUSS, Is.Not.Null);

            Assert.That(newUSS.status, Is.EqualTo(DropdownMenuAction.Status.Normal));
            Assert.That(existingUSS.status, Is.EqualTo(DropdownMenuAction.Status.Normal));
            Assert.That(removeUSS.status, Is.EqualTo(DropdownMenuAction.Status.Disabled));
        }

        /// <summary>
        /// **Create New USS** - this will open a Save File Dialog allowing you to create a new USS Asset in your project.
        /// </summary>
        [UnityTest]
        public IEnumerator CreateNewUSSViaRightClickMenu()
        {
            AddElementCodeOnly("TestElement");

            var panel = BuilderWindow.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            yield return UIETestEvents.Mouse.SimulateClick(StyleSheetsPane, MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);

            var newUSS = menu.FindMenuAction(BuilderConstants.ExplorerStyleSheetsPaneCreateNewUSSMenu);
            Assert.That(newUSS, Is.Not.Null);

            bool hasSaveDialogBeenOpened = false;
            BuilderStyleSheetsUtilities.s_SaveFileDialogCallback = () =>
            {
                hasSaveDialogBeenOpened = true;
                return k_TestUSSFilePath;
            };

            newUSS.Execute();
            Assert.That(hasSaveDialogBeenOpened, Is.True);

            yield return UIETestHelpers.Pause(1);

            var newUSSExplorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, k_TestUSSFileName);
            Assert.That(newUSSExplorerItems.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// **Add Existing USS** - this will open the Open File Dialog allowing you to add an existing USS Asset to the UXML document.
        /// </summary>
        [UnityTest]
        public IEnumerator AddExistingUSSViaRightClickMenu()
        {
            AddElementCodeOnly("TestElement");

            var panel = BuilderWindow.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            yield return UIETestEvents.Mouse.SimulateClick(StyleSheetsPane, MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);
            var existingUSS = menu.FindMenuAction(BuilderConstants.ExplorerStyleSheetsPaneAddExistingUSSMenu);
            Assert.That(existingUSS, Is.Not.Null);

            bool hasOpenDialogBeenOpened = false;
            BuilderStyleSheetsUtilities.s_OpenFileDialogCallback = () =>
            {
                hasOpenDialogBeenOpened = true;
                return k_ColorsTestUSSPath;
            };

            existingUSS.Execute();
            Assert.That(hasOpenDialogBeenOpened, Is.True);

            yield return UIETestHelpers.Pause(1);

            var newUSSExplorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, k_ColorsTestUSSFileName);
            Assert.That(newUSSExplorerItems.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// **Remove USS** (only enabled if right-clicking on a StyleSheet) - this will remove the StyleSheet from the UXML document.
        /// This should prompt to save unsaved changes.
        /// </summary>
        [UnityTest]
        public IEnumerator RemoveUSSViaRightClickMenu()
        {
            yield return CodeOnlyAddUSSToDocument(k_ColorsTestUSSPath);

            var panel = BuilderWindow.rootVisualElement.panel as BaseVisualElementPanel;
            var menu = panel.contextualMenuManager as BuilderTestContextualMenuManager;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuIsDisplayed, Is.False);

            var newUSSExplorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, k_ColorsTestUSSFileName);
            Assert.That(newUSSExplorerItems.Count, Is.EqualTo(1));

            yield return UIETestEvents.Mouse.SimulateClick(newUSSExplorerItems[0], MouseButton.RightMouse);
            Assert.That(menu.menuIsDisplayed, Is.True);
            var removeUSS = menu.FindMenuAction(BuilderConstants.ExplorerStyleSheetsPaneRemoveUSSMenu);
            Assert.That(removeUSS, Is.Not.Null);
            Assert.That(removeUSS.status, Is.EqualTo(DropdownMenuAction.Status.Normal));

            bool checkedForUnsavedChanges = false;
            BuilderStyleSheetsUtilities.s_CheckForUnsavedChanges = BuilderPaneWindow => checkedForUnsavedChanges = true;
            removeUSS.Execute();
            Assert.That(checkedForUnsavedChanges, Is.True);

            yield return UIETestHelpers.Pause(1);

            newUSSExplorerItems = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, k_ColorsTestUSSFileName);
            Assert.That(newUSSExplorerItems.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Selecting a StyleSheet or a selector within it will set the current *active* StyleSheet to this StyleSheet, updating the highlight (bold) of the *active* StyleSheet.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectingStyleSheetOrSelectorChangesActiveStyleSheet()
        {
            // Active StyleSheet is null when no USS are added.
            Assert.That(BuilderWindow.document.firstStyleSheet, Is.Null);
            Assert.That(BuilderWindow.document.activeStyleSheet, Is.Null);

            yield return CodeOnlyAddUSSToDocument(k_ColorsTestUSSPath);
            yield return CodeOnlyAddUSSToDocument(k_LayoutTestUSSPath);

            // First StyleSheet should be active by default.
            var colorsExplorerItem = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_ColorsTestUSSFileName);
            Assert.That(colorsExplorerItem, Is.Not.Null);
            var colorStyleSheet = GetStyleSheetFromExplorerItem(colorsExplorerItem, k_ColorsTestUSSPath);
            Assert.That(BuilderWindow.document.firstStyleSheet, Is.EqualTo(colorStyleSheet));
            Assert.That(BuilderWindow.document.activeStyleSheet, Is.EqualTo(colorStyleSheet));

            // Click on the second StyleSheet.
            var layoutExplorerItem = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_LayoutTestUSSFileName);
            Assert.That(layoutExplorerItem, Is.Not.Null);
            var layoutStyleSheet = GetStyleSheetFromExplorerItem(layoutExplorerItem, k_LayoutTestUSSPath);
            yield return UIETestEvents.Mouse.SimulateClick(layoutExplorerItem);
            Assert.That(BuilderWindow.document.firstStyleSheet, Is.EqualTo(colorStyleSheet));
            Assert.That(BuilderWindow.document.activeStyleSheet, Is.EqualTo(layoutStyleSheet));

            // Re-select first StyleSheet.
            colorsExplorerItem = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_ColorsTestUSSFileName);
            Assert.That(colorsExplorerItem, Is.Not.Null);
            yield return UIETestEvents.Mouse.SimulateClick(colorsExplorerItem);
            Assert.That(BuilderWindow.document.activeStyleSheet, Is.EqualTo(colorStyleSheet));

            // Selector selector in second StyleSheet.
            var unityButtonSelectors = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, ".unity-button");
            Assert.That(unityButtonSelectors, Is.Not.Empty);
            yield return UIETestEvents.Mouse.SimulateClick(unityButtonSelectors[1]);
            Assert.That(BuilderWindow.document.activeStyleSheet, Is.EqualTo(layoutStyleSheet));

            // Selector selector in first StyleSheet.
            unityButtonSelectors = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, ".unity-button");
            yield return UIETestEvents.Mouse.SimulateClick(unityButtonSelectors[0]);
            Assert.That(BuilderWindow.document.activeStyleSheet, Is.EqualTo(colorStyleSheet));
        }

        /// <summary>
        /// When pasting a selector in the StyleSheets pane, it will be added to the *active* StyleSheet.
        /// </summary>
        [UnityTest]
        public IEnumerator PastingAddsSelectorToActiveStyleSheet()
        {
            yield return CodeOnlyAddUSSToDocument(k_ColorsTestUSSPath);
            yield return CodeOnlyAddUSSToDocument(k_LayoutTestUSSPath);

            // Copy Selector.
            var unityButtonSelectors = BuilderTestsHelper.GetExplorerItemsWithName(StyleSheetsPane, ".unity-button");
            yield return UIETestEvents.Mouse.SimulateClick(unityButtonSelectors[0]);
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Copy);

            // Click on the second StyleSheet.
            var layoutExplorerItem = BuilderTestsHelper.GetExplorerItemWithName(StyleSheetsPane, k_LayoutTestUSSFileName);
            Assert.That(layoutExplorerItem, Is.Not.Null);
            var layoutStyleSheet = GetStyleSheetFromExplorerItem(layoutExplorerItem, k_LayoutTestUSSPath);
            var previousNumberOfSelectors = layoutStyleSheet.complexSelectors.Length;
            yield return UIETestEvents.Mouse.SimulateClick(layoutExplorerItem);
            Assert.That(BuilderWindow.document.activeStyleSheet, Is.EqualTo(layoutStyleSheet));

            // Paste Selector.
            yield return UIETestEvents.ExecuteCommand(BuilderWindow, UIETestEvents.Command.Paste);
            Assert.That(layoutStyleSheet.complexSelectors.Length, Is.EqualTo(previousNumberOfSelectors + 1));
        }
    }
}
