# Full Feature List

## Global

1. Can delete element via Delete key.
1. Can cut/copy/duplicate/paste Template instances, adding the correct Template registrations to the file.
1. Can copy/paste elements to and from a text file as valid UXML.
1. Can undo/redo changes to styles, hierarchy, and selection.
1. Open documents, the current selection, and unsaved changes survive domain reload.
1. Double-clicking a `.uxml` asset in the Project opens it with UI Builder. Double-clicking a `.uxml` that is already opened in UI Builder window will open the file in the default IDE.
1. Previously open document is re-opened after a Unity Editor restart.
1. Renaming, moving, or deleting a `.uxml` or `.uss` that is currently open in the UI Builder will give you the option to abort the operation or reset the Builder and lose any unsaved changes.
1. A dialog to Save/DontSave/Cancel will be shown if there are unsaved changes in the UI Builder, even if the UI Builder window is not open.
1. The Builder will properly update itself if external changes are made to the currently open UXML and USS assets.
1. The Builder will display a message saying unsaved changes are lost if there were unsaved changes and an external change was made to the currently open UXML and USS assets.
1. If the Builder loads or reloads with a UXML or USS that has invalid or unsupported syntax, a dialog, console errors, and a warning overlay will display telling the user what happened. Saving will not be allowed in this state.

## StyleSheets

1. Can select a StyleSheet via a root item that should have the name of the .uss file. This displays its dedicated Inspector. `Tested`
1. Can push the "+" dropdown menu in the toolbar to:
    1. **Create New USS** - this will open a Save File Dialog allowing you to create a new USS Asset in your project.
    1. **Add Existing USS** - this will open the Open File Dialog allowing you to add an existing USS Asset to the UXML document.
    1. Note: If there are no elements in current document, no options will display in this menu except a disabled options saying why no USS assets can be added.
1. Right-clicking anywhere in the TreeView should display the standard copy/paste/duplicate/delete menu with the additional options to: `Tested`
    1. **Create New USS** - this will open a Save File Dialog allowing you to create a new USS Asset in your project. `Tested`
    1. **Add Existing USS** - this will open the Open File Dialog allowing you to add an existing USS Asset to the UXML document. `Tested`
    1. **Remove USS** (only enabled if right-clicking on a StyleSheet) - this will remove the StyleSheet from the UXML document. This should prompt to save unsaved changes. `Tested`
1. Selectors get draggable style class pills for each selector part that is a style class name. `Tested`
1. In the StyleSheets pane, you can select selectors by clicking on the row or a style class pill. `Tested`
1. Can drag a style class pill from the StyleSheets pane onto an element in the Viewport to add the class. `Tested`
1. Can drag a style class pill from the StyleSheets pane onto an element in the Hierarchy to add the class. `Tested`
1. Dragging a style class onto an element inside a template instance or C# type in the Viewport adds it to the parent instance or C# element. `Tested`
1. Dragging a style class onto an element inside a template instance or C# type in the Hierarchy does nothing. `Tested`
1. In the toolbar of the StyleSheets pane there's a field that lets you create new selectors. This field is disabled if there are no elements in the UXML document. `Tested`
    1. After the field is focused, the explanation text is replaced with a default `.` and the cursor is set right after the `.` to let you quickly add a class-based selector. `Tested`
    1. You can commit and add your selector using the **Add** button/menu:
        1. If there are no USS files in the UXML document, the options to **Add to New USS** and **Add to Existing USS** will be available.
        1. If there is at least one USS file in the UXML document, the **Add** menu will contain the list of USS files in the UXML document, with the *active* StyleSheet checked.
            1. Selecting the *active* StyleSheet (the checked one) will add the selector to this USS. `Tested`
            1. Selecting a different unchecked StyleSheet will add the selector to this USS and change the *active* StyleSheet.
    1. You can commit and add your selector to the *active* StyleSheet by pressing **Enter**. `Tested`
        1. If there is no USS in document, the Save Dialog Option to create a new USS file will be prompted and selector will be added to the newly created and added USS file. `Tested`
    1. If the selector string contains invalid characters, an error message will display and the new selector will not be created - keeping the focus on the rename field. `Tested`
    1. You can discover and append `:pseudoStates` from the **States** menu.
    1. While the text field is selected, you should see a large tooltip displaying the selector cheatsheet. `Tested`
1. When selecting or hovering over a style selector in the StyleSheets pane, all elements in the Canvas that match the selector are highlighted.
1. With a selector selected, you can use standard short-cuts or the Edit menu to copy/paste/duplicate/delete it. You can also copy/paste the USS for the selector to/from a text file. `Tested`
1. Selecting an element or a the main document (VisualTreeAsset) should deselect any selected tree items in the StyleSheets pane. `Tested`
1. Selecting a StyleSheet or a selector within it will set the current *active* StyleSheet to this StyleSheet, updating the highlight (bold) of the *active* StyleSheet. `Tested`
1. When pasting a selector in the StyleSheets pane, it will be added to the *active* StyleSheet. `Tested`

## Hierarchy

1. The root items should display the currently loaded .uxml filename.
1. Can click to select an element. `Tested`
1. Can drag element onto other elements in the Hierarchy to re-parent. `Tested`
1. Can drag an element between other elements to reorder, with live preview in the Canvas. `Tested`
1. Can drag an element onto other elements in the Viewport to re-parent. `Tested`
1. Elements are displayed using their #name in blue. If they have no name, they are displayed using their C# type in white. `Tested`
1. You can always show the C# type of an element, even if it has a #name, by enabling the **Type** option from the `...` options menu in the top right of the Hierarchy pane.
1. You can show currently added style classes of an element by enabling the **Class List** option from the `...` options menu in the top right of the Hierarchy pane.
1. Elements are displayed grayed out if they are children of a template instance or C# type. `Tested`
1. Selecting an element inside a template instance or C# type displays the Inspector in read-only (disabled) mode.
1. Dragging an element onto a template instance or C# type element in the Viewport re-parents it to the parent instance or C# element. `Tested`
1. Dragging an element onto a template instance or C# type element in the Hierarchy re-parents it to the parent instance or C# element. `Tested`
1. Dragging child elements of a template instance or C# type element within the element or outside does not work. `Tested`
1. With an element selected, you can use the standard short-cuts and Edit menu to copy/paste/duplicate/delete it. The copied element is pasted at the same level of the hierarchy as the source element. If the source element's parent is deleted, the copied element is pasted at the root. `Tested`
1. Can copy/paste the UXML for the element to/from a text file. `Tested`
1. Right-clicking anywhere in the Hierarchy opens the Copy/Paste/Duplicate/Delete/Rename context menu.
1. Can double-click on an item to rename it. `Tested`
1. Can use the Rename command (optional hotkey like F2) to rename an item. `Tested`
1. During element rename, if clicking somewhere else (blurring the rename field), the rename will be applied and the rename field will go away. `Tested`
1. During element rename, if new name is not valid, an error message will display and rename will not be applied - keeping the focus on the rename field. `Tested`
1. When editing name of element in Hierarchy, clicking somewhere else will commit the change (if the new name is valid). `Tested`
1. Selecting an style selector or a the main StyleSheet in the StyleSheets pane should deselect any selected tree items in the Hierarchy. `Tested`
1. Elements have the correct icon to their left - same icon as in the Library pane.

## Library

1. Can switch between **Standard** and **Project** tab using tabs in the Library header. `Tested`
1. **Standard** tab shows built-in elements. `Tested`
1. **Standard** tab mode can be switched to the tree view representation using **Tree View** option from the `...` options menu in the top right of the Library pane.  `Tested`
1. **Project** tab contains UXML assets (`.uxml`) in the project `Asset/` folder under the **Assets** header. If there are no assets to display, the **Assets** header will be presented with no items.
1. In the **Project** tab, the UXML item context menu contains an action to **Add** template to the current document as an instance, **Open in UI Builder**, and **Open with IDE**.
1. You can view UXML assets (`.uxml`) within the `Packages/` folder under the **Assets** heading using the **Show Package Files** option from the `...` options menu in the top right of the Library pane.
1. **Project** tab contains available project-defined custom controls with `UxmlFactory` defined under the **Custom Controls** heading. If there are no custom controls available, this heading will not be displayed. `Tested`
1. **Standard** tab items that are only supported for Editor Extensions have an "Editor Only" tag beside them (in **Tree View** mode only).
1. Hovering over items in the Library **Project** view tab shows a preview of that element in a floating preview box. The preview uses the current Theme selected for the Canvas.
1. Can double click to create a new element instance at the root. `Tested`
1. Items that have corresponding `.uxml` assets have an "Open" button (icon) visible (on hover) that opens the asset for editing in UI Builder. The currently open `.uxml` asset in the Library is grayed out and is not instantiable to prevent infinite recursion. `Tested`
1. Can click and drag onto a Viewport element to create new instance as a child. This will also focus the Viewport pane. `Tested`
1. Can click and drag onto a Hierarchy element to create new instance as a child, or between elements to create as a sibling.  `Tested`
1. Can create (double-click or drag) template instances from other `.uxml` files. `Tested`
1. When creating a new empty VisualElement, it has an artificial minimum size and border which is reset as soon as you parent a child element under it or change its styling. `Tested`
1. Library pane updates if new `.uxml` files are added/deleted/moved/renamed to/from the project. `Tested`

## Viewport

### Header

1. The currently open UXML asset name, or `<unsaved asset>`, is displayed in the Viewport header, grayed out. `Tested`
1. If there are unsaved changes, a `*` is appended to the asset name. `Tested`
1. The current UI Builder package version is displayed in the **Viewport** title bar. `Tested`

### Toolbar

1. Selecting **File > New** clears the selection, the Viewport canvas, the StyleSheets pane, the Hierarchy, and all undo/redo stack operations for the previous document. A prompt is displayed if there are unsaved changes. `Tested`
1. Selecting **File > Open...** displays an Open File Dialog and lets you select a `.uxml` asset inside your Project.
1. Selecting **File > Save** asks for new file name the UXML if it is the first save, otherwise, it overwrites the previously saved/loaded file.
1. Saving should work even if the opened assets have been moved or renamed (in which case, the UI Builder should update the USS Style paths inside the UXML document).
1. Selecting **File > Save As...** always asks for a new file name and saves as a copy of the current document.
1. Can select a zoom level from the **100%** dropdown. Can also zoom via the mouse scroll wheel and Alt + RightClick + Mouse Move. `Tested`
1. Can reset the view and make sure the canvas fits the viewport with the **Fit Canvas** button. `Tested`
1. Can preview Light/Dark/Runtime themes inside the Canvas via the **Theme** popup field, independent from the current Editor Theme. **Default Theme** uses the current Editor Theme, while the other options force a theme to be used in the Canvas. If the runtime package is not installed, the Runtime theme will be substituted by the Light Editor theme.
1. Pressing **Preview** toggles _Preview_ mode, where you can no longer select elements by clicking them in the Viewport. Instead, Viewport elements receive regular mouse and focus events.
1. The `...` can be used to show/hide the UXML and USS Preview panes. This state is remembered across domain reloads.

### Canvas

1. Selected element has a blue border around it.
1. Selected element has a title header displaying the type of the element if it has no name, otherwise the name.
1. Can be resized via handles on all 4 sides.
1. Canvas has a minimum size.
1. Right-clicking an element in the Canvas opens the Copy/Paste/Duplicate/Delete/Rename context menu.
1. With an element selected, you can use the standard short-cuts and Edit menu to copy/paste/duplicate/delete it. The copied element and its children are pasted as children of the parent of the currently selected element. If nothing is selected, they are pasted at the root.
1. Can click to select element.
1. Selecting an element inside a template instance or C# type selects the parent instance or C# element.
1. Relative position elements have bottom, right, and bottom-right handles that change inline `height` and `width` styles.
1. Absolute position elements have all four side handles and all four corner handles visible.
1. Absolute position elements have four anchor handles visible to set or unset the `left`/`right`/`top`/`bottom` inline styles.
1. Absolute position elements can be moved by clicking and dragging, changing `top`/`right`/`left`/`bottom` inline styles depending on anchor state.
1. Resize and position handles change different styles depending on anchor state (ie. if `left` and `right` styles are set, changing the width changes the `right` style - otherwise, changing the width changes the `width` style).
1. Canvas size is restored after Domain Reload or Window reload. It is reset when opening/creating a new document.
1. Canvas size is remembered per-document.
1. When changing Width or Height in the Inspector, the corresponding resize handles in the canvas are highlighted.
1. When hovering over elements in the Canvas, the corresponding entry in the Hierarchy is highlighted.
1. When hovering over elements in the Canvas, all StyleSheets pane entries of style selectors that match this element are highlighted.
1. Canvas size is remembered for each asset and restored when loading the asset. It also means it survives Editor restarts.
1. Clicking the root item (with the .uxml filename) in the Hierarchy displays the Canvas options in the Inspector:
    1. Can see and change the Canvas height and width.
    1. Can set the custom Canvas background color/image Opacity.
    1. Can set the Canvas background to be a solid color via the Color Background mode.
    1. Can set the Canvas background to be an image, can set the ScaleMode of the image, and can have the Canvas resize to match the image via the **Fit Canvas to Image** button.
    1. Can set the Canvas background to be a render texture for a chosen Camera.
    1. All of these settings are remembered next time you open the same UXML document.
    1. If no background is selected, or if fully transparent, and the Runtime Theme is selected, the Canvas will use the checkerboard-style transparent background.
1. Can double click on an element that has a text attribute, or has sponsored a child element to serve as the text element, to edit its text in-place, directly in the Canvas.
    1. Once the in-place text field appears, it should have the same alignment, font style, and font size in order to perfectly overlap the existing text.
    1. Typing in the text field should increase its size and if appropriate, increase the size of its target element just as if the text attribute was being edited.
    1. Pressing ESC should undo any changes and restore the text value to what it was before editing.
    1. If the existing text value includes new-lines, the in-place text field will be set to multi-line mode and will need Shift+Enter to commit the new value.

### Viewport Surface

1. Can pan by holding down middle mouse button in the Viewport and moving the mouse.
1. Can pan by holding down Ctrl + Alt + LeftClick and moving the mouse.
1. Can zoom in and out with the mouse wheel.
1. Can zoom in and out by holding down Alt + RightClick and moving the mouse right and left.
1. Zoom and pan are remembered per-document.
1. Zoom and pan are restored after Domain Reload or Window reload. They are reset when opening/creating a new document.

## Previews

### UXML

1. Updates text on any changes to hierarchy, attributes, or inline styles.
1. Shows unsaved StyleSheet as path="&#42;unsaved in-memory StyleSheet with...".
1. Upon saving, all unsaved StyleSheet paths are fixed.
1. Shows `<Style>` tags for all root elements.
1. The `UnityEngine.UIElements` namespace is aliased to `ui:` and `UnityEditor.UIElements` namespace is aliased to `uie:`.
1. Custom C# elements not in the `UnityEngine.UIElements` and `UnityEditor.UIElements` namespaces have no namespace alias and appear as their full type.
1. (2019.3+) A relative path to a `.uss` asset is used in the `src` attribute of the `<Style>` tag if the asset is in the same folder as the main asset, or a subfolder of that folder. Otherwise, an absolute path is used.
1. (2019.3+) A relative path to a `.uxml` asset is used in the `src` attribute of the `<Template>` tag if the asset is in the same folder as the main asset, or a subfolder of that folder. Otherwise, an absolute path is used.
1. Pane header displays the name of the `.uxml` asset being previewed.
1. If asset is saved on disk, a button to open the `.uxml` asset in the default IDE will appear in the top-right corner of the pane header.
1. Special symbols in attribute values, like `\t \n & < > ' "`, are escaped properly when generating the UXML.

### USS

1. Displays the contents of the currently *active* StyleSheet. Contents are updated if the *active* StyleSheet changes.
1. Updates on all StyleSheet/Selector changes.
1. Dimension (Length) styles have the unit added to the USS (`px` or `%`).
1. Pane header displays the name of the `.uss` asset being previewed.
1. If asset is saved on disk, a button to open the `.uss` asset in the default IDE will appear in the top-right corner of the pane header.

## Inspector

### StyleSheet Inspector

1. Only visible if the selection is a StyleSheet (by selecting the root item in the StyleSheets pane).
1. Can create new Selectors by entering text in the **Selector** field and pressing Enter (or the **Create** button).
1. Shows quick help on selectors.

### Style Selector Inspector

1. Only visible if the selection is a selector in the current StyleSheet.
1. Can change the selector text by changing the **Selector** field and pressing Enter.

### Attributes Section

1. Only visible if the selection is an element in the current document.
1. Shows all valid attributes for the selected element, given its C# type.
1. Attributes already set to a non-default value are highlighted with the same styling as Prefab overrides.
1. Changing attributes updates the Hierarchy (or the StyleSheets pane), the Viewport, and the UXML Preview and changes are immediate.
1. Right-clicking **Unset** on an attribute removes it from the UXML tag, resets the value to the element-defined default, and resets the override styling.
1. Right-clicking **Unset All** on an attribute is the same as **Unset** except it unsets all set attributes.
1. For the `text` attribute, the TextField is set to multi-line and accepts multi-line values.
1. For the `name`, `binding-path`, and `view-data-path` attributes, if invalid characters will not be accepted - showing an error message when the user types and invalid character.
1. For the `type` attribute, like on the `ObjectField`, validation and attempted auto-completion will be done - showing an error message if the type is invalid.
1. If a validated field already has invalid characters, no changes should be allowed except deleting the value completely or pasting a valid value.

### StyleSheet Section

1. Only visible if the selection is an element in the current document.
1. Can add existing style class to element by typing the name of the class in the field inside the **StyleSheet** section and pressing the **Add Style Class to List** button (or pressing Enter).
1. Can extract all overwritten **Inlined Styles** to a new style class selector, added to the *active* StyleSheet, by typing the name of the class in the field inside **StyleSheet** and pressing the **Extract Inlined Styles to New Class** button. If there are no USS files attached to the UXML document, a dialog will open allowing the option to add to new USS or to an existing USS.
1. If the style class being added to an element is not valid, an error message appears.
1. All style classes on the current element are displayed as pills.
1. Style class pills have an **X** button that lets you remove them from the element, but only if the class was added in the current UXML document (and not via C# in a custom control's constructor).
1. Under **Matching Selectors**, all matching selectors on the current element are displayed with read-only fields for their properties.
1. Style class pills in the **StyleSheet** section show faded if there is no single-class selector in the main StyleSheet.
1. Double-clicking on a style class pill in the **StyleSheet** section selects the corresponding single-class selector in the main StyleSheet, if one exists, otherwise it creates it.

### (Inlined) Styles Section

1. Only visible if the selection is an element in the current document, or a selector in the current StyleSheet (in this case, the tile will change to just **Styles**).
1. Changing any value sets it in the StyleSheet or inline UXML style attribute and highlights it with a solid bar on the left side and bold font.
1. Style category headers have an override bar and bold font if any child styles are overridden.
1. All style value types are supported.
1. Sub-section foldout expanded states are preserved between selection changes and domain reload.
1. Right-clicking **Set** on a style field adds it to the UXML inline style or StyleSheet at whatever default or inherited value it was at when clicked.
1. Right-clicking **Unset** on an style field removes it from the UXML inline style or StyleSheet, resets the value to default, and resets the override styling.
1. The **Set** option in the right click menu on a style field should be grayed out if style is already set.
1. The **Unset** option in the right click menu on a style field should be grayed out if style is not set.
1. Right-clicking **Unset** on an styles category foldout is the same as **Unset** except it unsets all overridden style fields within that category foldout.
1. Right-clicking **Unset All** on a style field or styles category foldout is the same as **Unset** except it unsets all overridden style fields.
1. Align section toggle button strips change icons depending on the value of the flex-direction style.
1. Length style fields have a dropdown to select **Keyword** or **Unit**.
1. (2019.3+) Some Length style fields support the `%` **Unit**.
1. Foldout style fields (like Margin and Padding) properly add the unit or keyword for each child style property.
1. If the optional Vector Graphics package is installed, the background image style will allow assigning a vector image (scalable image) asset type.
1. When focusing a size, margin, padding, or border style field, the selected element will have a color overlay showing its size/margin/padding/border.
