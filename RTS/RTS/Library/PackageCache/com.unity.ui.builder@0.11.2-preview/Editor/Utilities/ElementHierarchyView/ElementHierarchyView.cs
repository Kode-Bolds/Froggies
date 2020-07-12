using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class ElementHierarchyView : VisualElement
    {
        public bool hierarchyHasChanged { get; set; }
        public bool hasUnsavedChanges { get; set; }
        public BuilderExplorer.BuilderElementInfoVisibilityState elementInfoVisibilityState { get; set; }

        VisualTreeAsset m_ClassPillTemplate;

        public IList<ITreeViewItem> treeRootItems
        {
            get
            {
                return m_TreeRootItems;
            }
            set {}
        }

        public IEnumerable<ITreeViewItem> treeItems
        {
            get
            {
                return m_TreeView.items;
            }
        }

        IList<ITreeViewItem> m_TreeRootItems;

        TreeView m_TreeView;
        HighlightOverlayPainter m_TreeViewHoverOverlay;

        VisualElement m_Container;
        ElementHierarchySearchBar m_SearchBar;

        Action<VisualElement> m_SelectElementCallback;

        List<VisualElement> m_SearchResultsHightlights;
        IPanel m_CurrentPanelDebug;

        BuilderPaneWindow m_PaneWindow;
        VisualElement m_DocumentRootElement;
        BuilderSelection m_Selection;
        BuilderClassDragger m_ClassDragger;
        BuilderHierarchyDragger m_HierarchyDragger;
        BuilderElementContextMenu m_ContextMenuManipulator;

        public VisualElement container
        {
            get { return m_Container; }
        }

        public ElementHierarchyView(
            BuilderPaneWindow paneWindow,
            VisualElement documentRootElement,
            BuilderSelection selection,
            BuilderClassDragger classDragger,
            BuilderHierarchyDragger hierarchyDragger,
            BuilderElementContextMenu contextMenuManipulator,
            Action<VisualElement> selectElementCallback,
            HighlightOverlayPainter highlightOverlayPainter)
        {
            m_PaneWindow = paneWindow;
            m_DocumentRootElement = documentRootElement;
            m_Selection = selection;
            m_ClassDragger = classDragger;
            m_HierarchyDragger = hierarchyDragger;
            m_ContextMenuManipulator = contextMenuManipulator;

            this.focusable = true;

            m_SelectElementCallback = selectElementCallback;
            hierarchyHasChanged = true;
            hasUnsavedChanges = false;

            m_SearchResultsHightlights = new List<VisualElement>();

            this.RegisterCallback<FocusEvent>(e => m_TreeView?.Focus());

            // HACK: ListView/TreeView need to clear their selections when clicking on nothing.
            this.RegisterCallback<MouseDownEvent>(e =>
            {
                var leafTarget = e.leafTarget as VisualElement;
                if (leafTarget.parent is ScrollView)
                    ClearSelection();
            });

            m_TreeViewHoverOverlay = highlightOverlayPainter;

            m_Container = new VisualElement();
            m_Container.name = "explorer-container";
            m_Container.style.flexGrow = 1;
            m_ClassDragger.builderHierarchyRoot = m_Container;
            m_HierarchyDragger.builderHierarchyRoot = m_Container;
            Add(m_Container);

            m_SearchBar = new ElementHierarchySearchBar(this);
            Add(m_SearchBar);

            // TODO: Hiding for now since search does not work, especially with style class pills.
            m_SearchBar.style.display = DisplayStyle.None;
            m_ClassPillTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/BuilderClassPill.uxml");

            // Create TreeView.
            m_TreeRootItems = new List<ITreeViewItem>();
            m_TreeView = new TreeView(m_TreeRootItems, 20, MakeItem, FillItem);
            m_TreeView.viewDataKey = "unity-builder-explorer-tree";
            m_TreeView.style.flexGrow = 1;
#if UNITY_2020_1_OR_NEWER
            m_TreeView.onSelectionChange += OnSelectionChange;
#else
            m_TreeView.onSelectionChanged += OnSelectionChange;
#endif

#if UNITY_2019_3_OR_NEWER
            m_TreeView.RegisterCallback<MouseDownEvent>(OnLeakedMouseClick);
#endif

            m_Container.Add(m_TreeView);

            m_ContextMenuManipulator.RegisterCallbacksOnTarget(m_Container);
        }

        void ActivateSearchBar(ExecuteCommandEvent evt)
        {
            Debug.Log(evt.commandName);
            if (evt.commandName == "Find")
                m_SearchBar.Focus();
        }

        void FillItem(VisualElement element, ITreeViewItem item)
        {
            var explorerItem = element as BuilderExplorerItem;
            explorerItem.Clear();

            // Pre-emptive cleanup.
            var row = explorerItem.parent.parent;
            row.RemoveFromClassList(BuilderConstants.ExplorerHeaderRowClassName);
            row.RemoveFromClassList(BuilderConstants.ExplorerItemHiddenClassName);
            row.RemoveFromClassList(BuilderConstants.ExplorerActiveStyleSheetClassName);

            // Get target element (in the document).
            var documentElement = (item as TreeViewItem<VisualElement>).data;
            documentElement.SetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName, explorerItem);
            explorerItem.SetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName, documentElement);
            row.userData = documentElement;

            // If we have a FillItem callback (override), we call it and stop creating the rest of the item.
            var fillItemCallback =
                documentElement.GetProperty(BuilderConstants.ExplorerItemFillItemCallbackVEPropertyName) as Action<VisualElement, ITreeViewItem, BuilderSelection>;
            if (fillItemCallback != null)
            {
                fillItemCallback(explorerItem, item, m_Selection);
                return;
            }

            // Create main label container.
            var labelCont = new VisualElement();
            labelCont.AddToClassList(BuilderConstants.ExplorerItemLabelContClassName);
            explorerItem.Add(labelCont);

            if (BuilderSharedStyles.IsStyleSheetElement(documentElement))
            {
                var styleSheetAsset = documentElement.GetStyleSheet();
                var styleSheetFileName = AssetDatabase.GetAssetPath(styleSheetAsset);
                var styleSheetAssetName = BuilderAssetUtilities.GetStyleSheetAssetName(styleSheetAsset, hasUnsavedChanges);
                var ssLabel = new Label(styleSheetAssetName);
                ssLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                ssLabel.AddToClassList("unity-debugger-tree-item-type");
                row.AddToClassList(BuilderConstants.ExplorerHeaderRowClassName);
                labelCont.Add(ssLabel);

                // Register right-click events for context menu actions.
                m_ContextMenuManipulator.RegisterCallbacksOnTarget(explorerItem);

                if (styleSheetAsset == m_PaneWindow.document.activeStyleSheet)
                    row.AddToClassList(BuilderConstants.ExplorerActiveStyleSheetClassName);

                return;
            }
            else if (BuilderSharedStyles.IsSelectorElement(documentElement))
            {
                var selectorParts = BuilderSharedStyles.GetSelectorParts(documentElement);

                foreach (var partStr in selectorParts)
                {
                    if (partStr.StartsWith(BuilderConstants.UssSelectorClassNameSymbol))
                    {
                        m_ClassPillTemplate.CloneTree(labelCont);
                        var pill = labelCont.contentContainer.ElementAt(labelCont.childCount - 1);
                        var pillLabel = pill.Q<Label>("class-name-label");
                        pill.AddToClassList("unity-debugger-tree-item-pill");
                        pill.SetProperty(BuilderConstants.ExplorerStyleClassPillClassNameVEPropertyName, partStr);
                        pill.userData = documentElement;

                        // Add ellipsis if the class name is too long.
                        var partStrShortened = BuilderNameUtilities.CapStringLengthAndAddEllipsis(partStr, BuilderConstants.ClassNameInPillMaxLength);
                        pillLabel.text = partStrShortened;

                        m_ClassDragger.RegisterCallbacksOnTarget(pill);
                    }
                    else if (partStr.StartsWith(BuilderConstants.UssSelectorNameSymbol))
                    {
                        var selectorPartLabel = new Label(partStr);
                        selectorPartLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                        selectorPartLabel.AddToClassList(BuilderConstants.ElementNameClassName);
                        labelCont.Add(selectorPartLabel);
                    }
                    else if (partStr.StartsWith(BuilderConstants.UssSelectorPseudoStateSymbol))
                    {
                        var selectorPartLabel = new Label(partStr);
                        selectorPartLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                        selectorPartLabel.AddToClassList(BuilderConstants.ElementPseudoStateClassName);
                        labelCont.Add(selectorPartLabel);
                    }
                    else if (partStr == BuilderConstants.SingleSpace)
                    {
                        var selectorPartLabel = new Label(BuilderConstants.TripleSpace);
                        selectorPartLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                        selectorPartLabel.AddToClassList(BuilderConstants.ElementTypeClassName);
                        labelCont.Add(selectorPartLabel);
                    }
                    else
                    {
                        var selectorPartLabel = new Label(partStr);
                        selectorPartLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                        selectorPartLabel.AddToClassList(BuilderConstants.ElementTypeClassName);
                        labelCont.Add(selectorPartLabel);
                    }
                }

                // Register right-click events for context menu actions.
                m_ContextMenuManipulator.RegisterCallbacksOnTarget(explorerItem);

                return;
            }

            if (BuilderSharedStyles.IsDocumentElement(documentElement))
            {
                var uxmlAsset = documentElement.GetVisualTreeAsset();
                var ssLabel = new Label(BuilderAssetUtilities.GetVisualTreeAssetAssetName(uxmlAsset, hasUnsavedChanges));
                ssLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                ssLabel.AddToClassList("unity-debugger-tree-item-type");
                row.AddToClassList(BuilderConstants.ExplorerHeaderRowClassName);
                labelCont.Add(ssLabel);
                return;
            }

            // Check if element is inside current document.
            if (!documentElement.IsPartOfCurrentDocument())
                row.AddToClassList(BuilderConstants.ExplorerItemHiddenClassName);

            // Register drag-and-drop events for reparenting.
            m_HierarchyDragger.RegisterCallbacksOnTarget(explorerItem);

            // Allow reparenting.
            explorerItem.SetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName, documentElement);

            // Element type label.
            if (string.IsNullOrEmpty(documentElement.name) ||
                elementInfoVisibilityState.HasFlag(BuilderExplorer.BuilderElementInfoVisibilityState.TypeName))
            {
                var typeLabel = new Label(documentElement.typeName);
                typeLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                typeLabel.AddToClassList(BuilderConstants.ElementTypeClassName);
                labelCont.Add(typeLabel);
            }

            // Element name label.
            var nameLabel = new Label();
            nameLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
            nameLabel.AddToClassList("unity-debugger-tree-item-name-label");
            nameLabel.AddToClassList(BuilderConstants.ExplorerItemNameLabelClassName);
            nameLabel.AddToClassList(BuilderConstants.ElementNameClassName);
            if (!string.IsNullOrEmpty(documentElement.name))
                nameLabel.text = BuilderConstants.UssSelectorNameSymbol + documentElement.name;
            labelCont.Add(nameLabel);

            // Textfield to rename element in hierarchy.
            var renameTextfield = explorerItem.CreateRenamingTextField(documentElement, nameLabel, m_Selection);
            labelCont.Add(renameTextfield);

            // Add class list.
            if (documentElement.classList.Count > 0 && elementInfoVisibilityState.HasFlag(BuilderExplorer.BuilderElementInfoVisibilityState.ClassList))
            {
                foreach (var ussClass in documentElement.GetClasses())
                {
                    var classLabelCont = new VisualElement();
                    classLabelCont.AddToClassList(BuilderConstants.ExplorerItemLabelContClassName);
                    explorerItem.Add(classLabelCont);

                    var classLabel = new Label(BuilderConstants.UssSelectorClassNameSymbol + ussClass);
                    classLabel.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                    classLabel.AddToClassList(BuilderConstants.ElementClassNameClassName);
                    classLabel.AddToClassList("unity-debugger-tree-item-classlist-label");
                    classLabelCont.Add(classLabel);
                }
            }

            // Show name of uxml file if this element is a TemplateContainer.
            var path = documentElement.GetProperty(BuilderConstants.LibraryItemLinkedTemplateContainerPathVEPropertyName) as string;
            Texture2D itemIcon;
            if (documentElement is TemplateContainer && !string.IsNullOrEmpty(path))
            {
                var pathStr = Path.GetFileName(path);
                var label = new Label(pathStr);
                label.AddToClassList(BuilderConstants.ExplorerItemLabelClassName);
                label.AddToClassList(BuilderConstants.ElementTypeClassName);
                label.AddToClassList("unity-builder-explorer-tree-item-template-path"); // Just make it look a bit shaded.
                labelCont.Add(label);
                itemIcon = BuilderLibraryContent.GetUXMLAssetIcon(path);
            }
            else
            {
                itemIcon = BuilderLibraryContent.GetTypeLibraryIcon(documentElement.GetType());
            }

            // Element icon.
            var icon = new VisualElement();
            icon.AddToClassList(BuilderConstants.ExplorerItemIconClassName);
            var styleBackgroundImage = icon.style.backgroundImage;
            styleBackgroundImage.value = new Background { texture = itemIcon };
            icon.style.backgroundImage = styleBackgroundImage;
            labelCont.Insert(0, icon);

            // Register right-click events for context menu actions.
            m_ContextMenuManipulator.RegisterCallbacksOnTarget(explorerItem);
        }

        void HighlightItemInTargetWindow(VisualElement documentElement)
        {
            if (m_TreeViewHoverOverlay == null)
                return;

            m_TreeViewHoverOverlay.AddOverlay(documentElement);
            var panel = documentElement.panel;
            panel?.visualTree.MarkDirtyRepaint();
        }

        public void ClearHighlightOverlay()
        {
            if (m_TreeViewHoverOverlay == null)
                return;

            m_TreeViewHoverOverlay.ClearOverlay();
        }

        public void ResetHighlightOverlays()
        {
            if (m_TreeViewHoverOverlay == null)
                return;

            m_TreeViewHoverOverlay.ClearOverlay();

            if (m_TreeView != null)
            {
#if UNITY_2020_1_OR_NEWER
                foreach (TreeViewItem<VisualElement> selectedItem in m_TreeView.selectedItems)
#else
                foreach (TreeViewItem<VisualElement> selectedItem in m_TreeView.currentSelection)
#endif
                {
                    var documentElement = selectedItem.data;
                    HighlightAllRelatedDocumentElements(documentElement);
                }
            }

            var panel = this.panel;
            panel?.visualTree.MarkDirtyRepaint();
        }

        public void RebuildTree(VisualElement rootVisualElement, bool includeParent = true)
        {
            if (!hierarchyHasChanged)
                return;

            // Save focus state.
            bool wasTreeFocused = false;
            if (m_TreeView != null)
                wasTreeFocused = m_TreeView.Q<ListView>().IsFocused();


            m_CurrentPanelDebug = rootVisualElement.panel;

            int nextId = 1;
            if (includeParent)
                m_TreeRootItems = GetTreeItemsFromVisualTreeIncludingParent(rootVisualElement, ref nextId);
            else
                m_TreeRootItems = GetTreeItemsFromVisualTree(rootVisualElement, ref nextId);

            // Clear selection which would otherwise persist via view data persistence.
            m_TreeView?.ClearSelection();
            m_TreeView.rootItems = m_TreeRootItems;

            // Restore focus state.
            if (wasTreeFocused)
                m_TreeView.Q<ListView>()?.Focus();

            // Auto-expand all items on load.
            if (m_TreeRootItems != null)
                foreach (var item in m_TreeView.rootItems)
                    m_TreeView.ExpandItem(item.id);

            hierarchyHasChanged = false;
        }

#if UNITY_2019_3_OR_NEWER
        void OnLeakedMouseClick(MouseDownEvent evt)
        {
            if (!(evt.target is ScrollView))
                return;

            m_TreeView.ClearSelection();
            evt.StopPropagation();
        }
#endif

#if UNITY_2020_1_OR_NEWER
        void OnSelectionChange(IEnumerable<ITreeViewItem> items)
#else
        void OnSelectionChange(List<ITreeViewItem> items)
#endif
        {
            if (m_SelectElementCallback == null)
                return;

#if UNITY_2020_1_OR_NEWER
            if (items.Count() == 0)
#else
            if (items.Count == 0)
#endif
            {
                m_SelectElementCallback(null);
                return;
            }

            var item = items.First() as TreeViewItem<VisualElement>;
            var element = item != null ? item.data : null;
            m_SelectElementCallback(element);
        }

        void HighlightAllElementsMatchingSelectorElement(VisualElement selectorElement)
        {
            var selector = selectorElement.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
            if (selector == null)
                return;

            var selectorStr = StyleSheetToUss.ToUssSelector(selector);
            var matchingElements = BuilderSharedStyles.GetMatchingElementsForSelector(m_DocumentRootElement, selectorStr);
            if (matchingElements == null)
                return;

            foreach (var element in matchingElements)
                HighlightItemInTargetWindow(element);
        }

        void HighlightAllRelatedDocumentElements(VisualElement documentElement)
        {
            if (BuilderSharedStyles.IsSelectorElement(documentElement))
            {
                HighlightAllElementsMatchingSelectorElement(documentElement);
            }
            else
            {
                HighlightItemInTargetWindow(documentElement);
            }
        }

        VisualElement MakeItem()
        {
            var element = new BuilderExplorerItem();
            element.name = "unity-treeview-item-content";
            element.RegisterCallback<MouseEnterEvent>((e) =>
            {
                ClearHighlightOverlay();

                var explorerItem = e.target as VisualElement;
                var documentElement = explorerItem?.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;
                HighlightAllRelatedDocumentElements(documentElement);
            });
            element.RegisterCallback<MouseLeaveEvent>((e) =>
            {
                ClearHighlightOverlay();
            });

            element.RegisterCustomBuilderStyleChangeEvent(elementStyle =>
            {
                var documentElement = element.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;
                var isValidTarget = documentElement != null;
                if (!isValidTarget)
                    return;

                var icon = element.Q(null, BuilderConstants.ExplorerItemIconClassName);
                if (icon == null)
                    return;

                var path = documentElement.GetProperty(BuilderConstants.LibraryItemLinkedTemplateContainerPathVEPropertyName) as string;
                var libraryIcon = BuilderLibraryContent.GetTypeLibraryIcon(documentElement.GetType());
                if (documentElement is TemplateContainer && !string.IsNullOrEmpty(path))
                {
                    libraryIcon = BuilderLibraryContent.GetUXMLAssetIcon(path);
                }
                else if (elementStyle == BuilderElementStyle.Highlighted && !EditorGUIUtility.isProSkin)
                {
                        libraryIcon = BuilderLibraryContent.GetTypeDarkSkinLibraryIcon(documentElement.GetType());
                }

                var styleBackgroundImage = icon.style.backgroundImage;
                styleBackgroundImage.value = new Background { texture = libraryIcon };
                icon.style.backgroundImage = styleBackgroundImage;
            });

            return element;
        }

        TreeViewItem<VisualElement> FindElement(IEnumerable<ITreeViewItem> list, VisualElement element)
        {
            if (list == null)
                return null;

            foreach (var item in list)
            {
                var treeItem = item as TreeViewItem<VisualElement>;
                if (treeItem.data == element)
                    return treeItem;

                TreeViewItem<VisualElement> itemFoundInChildren = null;
                if (treeItem.hasChildren)
                    itemFoundInChildren = FindElement(treeItem.children, element);

                if (itemFoundInChildren != null)
                    return itemFoundInChildren;
            }

            return null;
        }

        public void ClearSelection()
        {
            if (m_TreeView == null)
                return;

            m_TreeView.ClearSelection();
        }

        public void ClearSearchResults()
        {
            foreach (var hl in m_SearchResultsHightlights)
                hl.RemoveFromHierarchy();

            m_SearchResultsHightlights.Clear();
        }

        public void SelectElement(VisualElement element)
        {
            SelectElement(element, string.Empty);
        }

        public void SelectElement(VisualElement element, string query)
        {
            SelectElement(element, query, SearchHighlight.None);
        }

        public void SelectElement(VisualElement element, string query, SearchHighlight searchHighlight)
        {
            ClearSearchResults();

            var item = FindElement(m_TreeRootItems, element);
            if (item == null)
                return;

#if UNITY_2020_1_OR_NEWER
            m_TreeView.SetSelection(item.id);
            m_TreeView.ScrollToItem(item.id);
#else
            m_TreeView.SelectItem(item.id);
#endif

            if (string.IsNullOrEmpty(query))
                return;

            var selected = m_TreeView.Query(classes: "unity-list-view__item--selected").First();
            if (selected == null || searchHighlight == SearchHighlight.None)
                return;

            var content = selected.Q("unity-treeview-item-content");
            var labelContainers = content.Query(classes: "unity-builder-explorer-tree-item-label-cont").ToList();
            foreach (var labelContainer in labelContainers)
            {
                var label = labelContainer.Q<Label>();

                if (label.ClassListContains("unity-debugger-tree-item-type") && searchHighlight != SearchHighlight.Type)
                    continue;

                if (label.ClassListContains("unity-debugger-tree-item-name") && searchHighlight != SearchHighlight.Name)
                    continue;

                if (label.ClassListContains("unity-debugger-tree-item-classlist") && searchHighlight != SearchHighlight.Class)
                    continue;

                var text = label.text;
                var indexOf = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
                if (indexOf < 0)
                    continue;

                var highlight = new VisualElement();
                m_SearchResultsHightlights.Add(highlight);
                highlight.AddToClassList("unity-debugger-highlight");
                int letterSize = 8;
                highlight.style.width = query.Length * letterSize;
                highlight.style.left = indexOf * letterSize;
                labelContainer.Insert(0, highlight);

                break;
            }
        }

        IList<ITreeViewItem> GetTreeItemsFromVisualTreeIncludingParent(VisualElement parent, ref int nextId)
        {
            if (parent == null)
                return null;

            var items = new List<ITreeViewItem>();
            var id = nextId;
            nextId++;

            var item = new TreeViewItem<VisualElement>(id, parent);
            items.Add(item);

            var childItems = GetTreeItemsFromVisualTree(parent, ref nextId);
            if (childItems == null)
                return items;

            item.AddChildren(childItems);

            return items;
        }

        IList<ITreeViewItem> GetTreeItemsFromVisualTree(VisualElement parent, ref int nextId)
        {
            List<ITreeViewItem> items = null;

            if (parent == null)
                return null;

            int count = parent.hierarchy.childCount;
            if (count == 0)
                return null;

            for (int i = 0; i < count; i++)
            {
                var element = parent.hierarchy[i];

                if (element.name == BuilderConstants.SpecialVisualElementInitialMinSizeName)
                    continue;

                if (items == null)
                    items = new List<ITreeViewItem>();

                var id = 0;
                var linkedAsset = element.GetVisualElementAsset();
                if (linkedAsset != null)
                {
                    id = linkedAsset.id;
                }
                else
                {
                    id = nextId;
                    nextId++;
                }

                var item = new TreeViewItem<VisualElement>(id, element);
                items.Add(item);

                var childItems = GetTreeItemsFromVisualTree(element, ref nextId);
                if (childItems == null)
                    continue;

                item.AddChildren(childItems);
            }

            return items;
        }
    }
}
