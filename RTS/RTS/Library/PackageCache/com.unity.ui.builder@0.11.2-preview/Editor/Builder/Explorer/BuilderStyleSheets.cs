using System;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal class BuilderStyleSheets : BuilderExplorer
    {
        static readonly string kToolbarPath = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderStyleSheetsNewSelectorControls.uxml";
        static readonly string kHelpTooltipPath = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderStyleSheetsNewSelectorHelpTips.uxml";

        ToolbarMenu m_AddUSSMenu;
        TextField m_NewSelectorTextField;
        VisualElement m_NewSelectorTextInputField;
        ToolbarMenu m_PseudoStatesMenu;
        ToolbarMenu m_NewSelectorAddMenu;
        BuilderTooltipPreview m_TooltipPreview;

        bool m_FieldFocusedFromStandby;
        bool m_ShouldRefocusSelectorFieldOnBlur;

        BuilderDocument document => m_PaneWindow?.document;

        static readonly List<string> kNewSelectorPseudoStatesNames = new List<string>()
        {
            ":hover", ":active", ":selected", ":checked", ":focus"
        };

        public BuilderStyleSheets(
            BuilderPaneWindow paneWindow,
            BuilderViewport viewport,
            BuilderSelection selection,
            BuilderClassDragger classDragger,
            BuilderHierarchyDragger hierarchyDragger,
            HighlightOverlayPainter highlightOverlayPainter,
            BuilderTooltipPreview tooltipPreview)
            : base(
                  paneWindow,
                  viewport,
                  selection,
                  classDragger,
                  hierarchyDragger,
                  new BuilderStyleSheetsContextMenu(paneWindow, selection),
                  viewport.styleSelectorElementContainer,
                  false,
                  highlightOverlayPainter,
                  kToolbarPath)
        {
            m_TooltipPreview = tooltipPreview;
            if (m_TooltipPreview != null)
            {
                var helpTooltipTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(kHelpTooltipPath);
                var helpTooltipContainer = helpTooltipTemplate.CloneTree();
                m_TooltipPreview.Add(helpTooltipContainer); // We are the only ones using it so just add the contents and be done.
            }

            viewDataKey = "builder-style-sheets";
            AddToClassList(BuilderConstants.ExplorerStyleSheetsPaneClassName);

            var parent = this.Q("new-selector-item");

            // Init text field.
            m_NewSelectorTextField = parent.Q<TextField>("new-selector-field");
            m_NewSelectorTextField.SetValueWithoutNotify(BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage);
            m_NewSelectorTextInputField = m_NewSelectorTextField.Q("unity-text-input");
            m_NewSelectorTextInputField.RegisterCallback<KeyDownEvent>(OnEnter, TrickleDown.TrickleDown);
            UpdateNewSelectorFieldEnabledStateFromDocument();

            m_NewSelectorTextInputField.RegisterCallback<FocusEvent>((evt) =>
            {
                var input = evt.target as VisualElement;
                var field = input.parent as TextField;
                m_FieldFocusedFromStandby = true;
                if (field.text == BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage || m_ShouldRefocusSelectorFieldOnBlur)
                {
                    m_ShouldRefocusSelectorFieldOnBlur = false;
                    field.value = BuilderConstants.UssSelectorClassNameSymbol;
                }

                ShowTooltip();
            });

            m_NewSelectorTextField.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                var field = evt.target as TextField;

                if (!string.IsNullOrEmpty(evt.newValue) && evt.newValue != BuilderConstants.UssSelectorClassNameSymbol)
                {
                    m_NewSelectorAddMenu.SetEnabled(true);
                    m_PseudoStatesMenu.SetEnabled(true);
                }
                else
                {
                    m_NewSelectorAddMenu.SetEnabled(false);
                    m_PseudoStatesMenu.SetEnabled(false);
                }

                if (!m_FieldFocusedFromStandby)
                    return;

                m_FieldFocusedFromStandby = false;

                // We don't want the '.' we just inserted in the FocusEvent to be highlighted,
                // which is the default behavior.
                field.SelectRange(1, 1);
            });

            m_NewSelectorTextInputField.RegisterCallback<BlurEvent>((evt) =>
            {
                var input = evt.target as VisualElement;
                var field = input.parent as TextField;
                if (m_ShouldRefocusSelectorFieldOnBlur)
                {
                    field.schedule.Execute(PostEnterRefocus);
                    evt.PreventDefault();
                    evt.StopImmediatePropagation();
                    return;
                }

                if (string.IsNullOrEmpty(field.text) || field.text == BuilderConstants.UssSelectorClassNameSymbol)
                {
                    field.SetValueWithoutNotify(BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage);
                    m_NewSelectorAddMenu.SetEnabled(false);
                    m_PseudoStatesMenu.SetEnabled(false);
                }

                HideTooltip();
            });

            // Setup New USS Menu.
            m_AddUSSMenu = parent.Q<ToolbarMenu>("add-uss-menu");
            SetUpAddUSSMenu();

            // Setup new selector button.
            m_NewSelectorAddMenu = parent.Q<ToolbarMenu>("add-new-selector-menu");
            m_NewSelectorAddMenu.SetEnabled(false);
            SetUpAddMenu();

            // Setup pseudo states menu.
            m_PseudoStatesMenu = parent.Q<ToolbarMenu>("add-pseudo-state-menu");
            m_PseudoStatesMenu.SetEnabled(false);
            SetUpPseudoStatesMenu();
        }

        protected override bool IsSelectedItemValid(VisualElement element)
        {
            var isCS = element.GetStyleComplexSelector() != null;
            var isSS = element.GetStyleSheet() != null;

            return isCS || isSS;
        }

        void PostEnterRefocus()
        {
            m_NewSelectorTextInputField.Focus();
        }

        void OnEnter(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                return;

            CreateNewSelector(document.activeStyleSheet);

            evt.PreventDefault();
            evt.StopImmediatePropagation();
        }

        void OnAddPress(StyleSheet styleSheet)
        {
            CreateNewSelector(styleSheet);

            PostEnterRefocus();
        }

        void CreateNewSelector(StyleSheet styleSheet)
        {
            var newValue = m_NewSelectorTextField.text;
            if (newValue == BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage)
                return;

            if (styleSheet == null)
            {
                if (BuilderStyleSheetsUtilities.CreateNewUSSAsset(m_PaneWindow))
                {
                    styleSheet = m_PaneWindow.document.firstStyleSheet;

                    // The EditorWindow will no longer have Focus after we show the
                    // Save Dialog so even though the New Selector field will appear
                    // focused, typing won't do anything. As such, it's better, in
                    // this one case to remove focus from this field so users know
                    // to re-focus it themselves before they can add more selectors.
                    m_NewSelectorTextField.value = string.Empty;
                    m_NewSelectorTextField.Blur();
                }
                else
                {
                    return;
                }
            }
            else
            {
                m_ShouldRefocusSelectorFieldOnBlur = true;
            }

            var newSelectorStr = newValue;
            if (newSelectorStr.StartsWith(BuilderConstants.UssSelectorClassNameSymbol))
            {
                newSelectorStr = BuilderConstants.UssSelectorClassNameSymbol + newSelectorStr.TrimStart(BuilderConstants.UssSelectorClassNameSymbol[0]);
            }

            if (string.IsNullOrEmpty(newSelectorStr))
                return;

            if (newSelectorStr.Length == 1 && (
                    newSelectorStr.StartsWith(BuilderConstants.UssSelectorClassNameSymbol)
                    || newSelectorStr.StartsWith("-")
                    || newSelectorStr.StartsWith("_")))
                return;

            if (!BuilderNameUtilities.StyleSelectorRegex.IsMatch(newSelectorStr))
            {
                Builder.ShowWarning(BuilderConstants.StyleSelectorValidationSpacialCharacters);
                m_NewSelectorTextField.schedule.Execute(() =>
                {
                    m_NewSelectorTextField.SetValueWithoutNotify(newValue);
                    m_NewSelectorTextField.SelectAll();
                });
                return;
            }

            var selectorContainerElement = m_Viewport.styleSelectorElementContainer;
            BuilderSharedStyles.CreateNewSelector(selectorContainerElement, styleSheet, newSelectorStr);

            m_Selection.NotifyOfHierarchyChange();
            m_Selection.NotifyOfStylingChange();
        }

        void SetUpAddUSSMenu()
        {
            if (m_AddUSSMenu == null)
                return;

            m_AddUSSMenu.menu.MenuItems().Clear();

            if (m_PaneWindow.document.visualTreeAsset.IsEmpty())
            {
                m_AddUSSMenu.menu.AppendAction(
                    BuilderConstants.ExplorerStyleSheetsPanePlusMenuNoElementsMessage,
                    action => {},
                    action => DropdownMenuAction.Status.Disabled);
            }
            else
            {
                m_AddUSSMenu.menu.AppendAction(
                    BuilderConstants.ExplorerStyleSheetsPaneCreateNewUSSMenu,
                    action =>
                    {
                        BuilderStyleSheetsUtilities.CreateNewUSSAsset(m_PaneWindow);
                    });
                m_AddUSSMenu.menu.AppendAction(
                    BuilderConstants.ExplorerStyleSheetsPaneAddExistingUSSMenu,
                    action =>
                    {
                        BuilderStyleSheetsUtilities.AddExistingUSSToAsset(m_PaneWindow);
                    });
            }
        }

        void SetUpAddMenu()
        {
            m_NewSelectorAddMenu.menu.MenuItems().Clear();

            if (m_PaneWindow.document.firstStyleSheet == null)
            {
                m_NewSelectorAddMenu.menu.AppendAction(
                    BuilderConstants.ExplorerStyleSheetsPaneAddToNewUSSMenu,
                    action =>
                    {
                        OnAddPress(null);
                    });
                m_NewSelectorAddMenu.menu.AppendAction(
                    BuilderConstants.ExplorerStyleSheetsPaneAddToExistingUSSMenu,
                    action =>
                    {
                        var successfullyAdded = BuilderStyleSheetsUtilities.AddExistingUSSToAsset(m_PaneWindow);
                        if (successfullyAdded)
                            OnAddPress(document.firstStyleSheet);
                    });
            }
            else
            {
                foreach (var openUSSFile in m_PaneWindow.document.openUSSFiles)
                {
                    var styleSheet = openUSSFile.Sheet;
                    m_NewSelectorAddMenu.menu.AppendAction(
                        styleSheet.name + BuilderConstants.UssExtension,
                        action =>
                        {
                            var newUSS = action.userData as StyleSheet;
                            OnAddPress(newUSS);

                            if (newUSS != document.activeStyleSheet)
                            {
                                document.UpdateActiveStyleSheet(m_Selection, newUSS, this);
                                UpdateHierarchy(m_Selection.hasUnsavedChanges);
                            }
                        },
                        action => (UnityEngine.Object)action.userData == document.activeStyleSheet
                            ? DropdownMenuAction.Status.Checked
                            : DropdownMenuAction.Status.Normal,
                        styleSheet);
                }
            }
        }

        void SetUpPseudoStatesMenu()
        {
            foreach (var state in kNewSelectorPseudoStatesNames)
                m_PseudoStatesMenu.menu.AppendAction(state, a =>
                {
                    m_NewSelectorTextField.value += a.name;
                });
        }

        void ShowTooltip()
        {
            if (m_TooltipPreview == null)
                return;

            if (m_TooltipPreview.isShowing)
                return;

            m_TooltipPreview.Show();

            m_TooltipPreview.style.left = this.pane.resolvedStyle.width + BuilderConstants.TooltipPreviewYOffset;
            m_TooltipPreview.style.top = m_Viewport.viewportWrapper.worldBound.y;
        }

        void HideTooltip()
        {
            if (m_TooltipPreview == null)
                return;

            m_TooltipPreview.Hide();
        }

        void UpdateNewSelectorFieldEnabledStateFromDocument()
        {
            bool enabled = false;
            if (m_PaneWindow != null)
                enabled = !m_PaneWindow.document.visualTreeAsset.IsEmpty();
            m_NewSelectorTextField.SetEnabled(enabled);
            SetUpAddUSSMenu();
        }

        protected override void ElementSelected(VisualElement element)
        {
            base.ElementSelected(element);

            // Initial element selection will be called before the document has been set.
            if (document == null)
                return;

            var activeStyleSheetChanged = document.UpdateActiveStyleSheetFromSelection(m_Selection);
            if (activeStyleSheetChanged)
                UpdateHierarchyAndSelection(m_Selection.hasUnsavedChanges);
        }

        public override void SelectionChanged()
        {
            base.SelectionChanged();

            document.UpdateActiveStyleSheetFromSelection(m_Selection);
        }

        public override void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            base.HierarchyChanged(element, changeType);

            UpdateNewSelectorFieldEnabledStateFromDocument();
        }

        public override void StylingChanged(List<string> styles)
        {
            base.StylingChanged(styles);

            SetUpAddMenu();
        }
    }
}
