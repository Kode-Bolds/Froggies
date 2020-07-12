# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.11.2] - 2020-05-14

- Fixed stack overflow error when deleting USS selector via right-click > Delete in the StyleSheets pane.
- Fixed name and icon of UI Builder window being reset to just "Builder" (and no icon) after a domain reload.
- (2020.1+) Fixed UXML Instance elements (TemplateContainers) not showing any attribute fields in the Inspector (like Name).

## [0.11.1] - 2020-05-12

- Added a "+" menu to the StyleSheets pane toolbar to make adding USS assets to the current document more discoverable.
- Fixed UI Builder not being able to display if loaded UXML or USS have invalid or unsupported syntax, even after a Unity restart.
- Fixed regression where changes made to USS or UXML in external editor would be undone when the UI Builder refreshed.
- Fixed hover preview overlay in the Library not disappearing when opening a UXML document via the Library open icon.
- Fixed reloading the scene causing Canvas camera background to go blank.
- Fixed extracting of inline styles from the Inspector when there are no USS files attached to the UXML document. A dialog with options will now be shown.

## [0.11.0] - 2020-05-05

- Multi-USS Support:
    - Added support for attaching zero and more than one USS file to your UXML document.
    - By default, new UXML documents will now start with zero USS files attached.
    - From the StyleSheets pane, you can right-click to:
        - add an existing USS file to your UXML document,
        - create a new empty USS file,
        - or remove a USS from the UXML document.
    - In order to add a USS file to the document, there has to be at least one element in the UXML document to contain the `<Style>` tag.
    - There is now the concept of an "active" USS file (marked with **bold** text). This will be the file new selectors are added via:
        - the toolbar field,
        - copy/paste,
        - or the Inspector's Extract Inline Styles feature.
- Re-designed Library Pane:
    - Library items now have icons! These icons also appear in the Hierarchy.
    - New Library pane view with large icons in a grid.
    - Elements that are only supported for writing Editor Extensions are now marked "Editor Only" in the Library.
    - UXML files in the Library "Open" button replaced with icon that only appears on hover.
    - Library is split into two tabs:
        - "Standard" tab is where all standard Unity controls are located.
        - "Project" tab is where all project UXML and custom C# controls can be found.
- Re-designed Selection Overlays:
    - Improved the look of the selection overlay to not obscure the element. It's just a blue border now.
    - The size/border/padding/margin overlays will appear only when editing size/border/padding/margin style properties in the Inspector.
    - Added header on selected element with type or name being displayed.
- Other:
    - Added support for editing of text in-place directly in the Canvas by double clicking on a text element.
    - Added ability to hide the UXML and USS Preview panes.
    - Added checkerboard-style background for the Runtime Theme to better visualize transparent elements.
    - Added support for the system Rename command in the Hierarchy (which can optionally be given a hotkey, like F2, via the Shortcut Manager).
    - Added right-click menu on Inspector Styles section category foldouts with option to Unset all style properties in a category at once.
    - Added an icon to the UI Builder window tab.
    - Added support for the vector image type (enabled if the optional Vector Graphics package is installed).
    - (2020.2+) Added horizontal scrollbar to Hierarchy and StyleSheets panes.
    - (2020.2+) Added support for the MissingReference style property type to allow users to fix broken paths in USS inside the UI Builder.
- Fixed
    - Fixed invalid type conversion error when placing down a LayerMaskField.
    - Fixed Wrap style "nowrap" generating incorrect USS with the wrong keyword, "no-wrap".
    - Fixed ESC key not cancelling the Rename of an element in the Hierarchy.
    - Fixed clicking somewhere else while renaming an element in the Hierarchy (de-focusing the Rename field) not committing the rename.
    - Fixed background image and cursor style properties generating invalid USS if set to "none".
    - Fixed extra fields being visible behind numeric StyleFields when the Inspector pane was really wide.
    - Fixed extra escape characters being added to the style attribute in the generated UXML.
    - Fixed having the ability to remove an un-removable style class that was not added in UXML in the Inspector.
    - Fixed line endings used in saved UXML and USS to use Unity's project setting and/or OS-defaults.
    - Fixed undo/redo history being cleared when entering playmode.
    - (2019.3+) Fixed exceptions when opening a USS file with use of color keywords.

## [0.10.2] - 2020-03-11

- Moved Hierarchy "hamburger/3-dots" options menu from a dedicated toolbar to the Hierarchy pane header.
- Newly created empty VisualElements will now auto-size themselves when given a background image, provided no other styles have already been overridden.
- The Inspector will now be blank when nothing is selected to match the Unity Inspector Window.
- Fixed handling of built-in resources assets when set via the Inspector. USS does not support such paths and this is now properly indicated with an message.
- Fixed background color style alpha value being reset to 255 if all other components were set to 0.
- Fixed ghosting of some inspector controls when the inspector was disabled.
- Fixed text overlapping controls in Library hover and drag tooltips.
- Fixed null-ref exception when deselecting an element after having made a change to style but not committed the change.
- Fixed Overflow style not being visually applied in the canvas when changed as an inline style.
- Fixed Canvas not being centered the first time the UI Builder window is opened with no UXML document loaded.
- Fixed escaping of `&` in image file paths inside inlined styles in UXML.
- Fixed canvas background settings like Camera view being reset when saving file for the first time.
- Fixed the unsaved changes marker `*` not appearing beside file names in the StyleSheet, Hierarchy, UXML Preview, and USS Preview panes.
- (mac) Fixed macOS playing the "bad key-press" sound when the Delete key is used to delete an item from the inspector.
- (2020.1+) Fixed showing a blank 3rd button in the Overflow style options in the Inspector.

## [0.10.1] - 2020-02-06

- Improved handling of external changes made to the currently open UXML or USS asset. Unsaved changes will still be lost for now but the UI Builder will at least refresh itself properly after an external change.
- Added character validation for new style selector in the StyleSheets pane.
- Improved character validation when adding a style class to an element in the Inspector pane.
- Added character validation to the Name, Binding Path, and View Data Key attributes of an element in the Inspector pane as you type.
- Added type validation to the Type attribute, like on the ObjectField, with a bit of auto-completion.
- Added multi-line support to the Text attribute in the Inspector.
- Added the style fields' right-click menu option to **Set** a style (inline in UXML or in StyleSheet) at whatever default or inherited value it is at without having to modify it explicitly.
- Style fields' right-click menu options **Set** and **Unset** will be grayed out if the respective action is not applicable (can't Set something that is already Set).
- Added IMGUIContainer and all Toolbar controls to the Library.
- Added better (looking) default values for Gradient Field and Curve Field when hovering them in the Library and when adding them to the document.
- Copy/Paste and Duplicate will now focus the newly created element.
- Dragging an element from the Library into the Canvas will now focus the Viewport, letting you immediately delete it.
- When the Save Dialog appears, the UXML path field will automatically get focus.
- Pressing Enter in the UXML or USS fields of the Save Dialog will be equivalent to pressing the Save button.
- When editing name of element in Hierarchy, clicking somewhere else will now commit the change (if the new name is valid).
- Removed **Reset** button from Toolbar.
- Removed UI Builder's own assets and types from showing up in the Library.
- Element highlight while hovering the StyleSheets and Hierarchy pane will now be clipped by the Viewport and will no longer show on top of other panes.
- Fixed a bug where the UXML or USS Preview pane header would appear duplicated.
- Fixed special symbols in attribute values, like `\t \n & < > ' "`, not being escaped properly when generating the UXML.
- Fixed extracting local style overrides to new USS selector not properly extracting `resource()`-type paths.
- Fixed console errors when turning on Camera render texture background mode for the Canvas on new documents.
- Fixed SliderInt console errors when added to document and having the Step Size attribute not working.
- Fixed Opacity style percent field incorrectly casting the integer input values to float, causing a 5 to turn into a 4.
- Fixed ProgressBar Low Value and High Value attributes not being read properly from the element when displaying the Attributes inspector.
- Fixed `display: none` in a StyleSheet rule not being properly read by the Builder, causing console errors when showing the Inspector of the rule.
- Fixed the Type attribute on the ObjectField immediately invalidating the UXML on first input character, causing import errors, and making it unusable.
- Fixed Slider default label saying SliderInt.
- Fixed tooltip and drag preview for Bounds and Bounds (Int) to not appear squished.
- Fixed zoom and pan not being remembered per document.
- Fixed being able to delete and move elements inside a C# element or UXML template instance.
- Fixed canvas theme not being re-applied when loading a document.
- Fixed Toolbar document name not getting the * for unsaved changes right after a save and a modification to inline styles.
- Fixed no preview showing up while dragging a ListView, ScrollView, or IMGUIContainer.
- Fixed double-clicking the .uxml document entry in the Hierarchy causing an error in the Console.
- Fixed asset rename/move warning dialogs to show the type of action being done (was always showing as a move).
- Fixed dragging from Hierarchy using right-click causing the preview tooltip to stay on screen when mouse button is released.

## [0.10.0] - 2019-12-13

- Added zoom and pan support in the Viewport. There are view reset buttons and a zoom levels menu in the Viewport toolbar.
- Added support for changing the Length style unit.
- Added support for `%` unit (in 2019.3+).
- Added support for keywords on numeric style fields in the Inspector.
- Added element context menu on the Canvas elements - same one as the Hierarchy context menu.
- Added **Unset All** option to the Attributes and Local Styles fields of the Inspector.
- Added name of uxml/uss file to the header of the UXML/USS Preview panes.
- Added button to the top-right of the UXML and USS Preview panes to open the source file (if saved) in the default IDE.
- A dialog to Save/DontSave/Cancel will now be shown if there are unsaved changes in the UI Builder, even if the UI Builder window is not open.
- (2019.3+) Fixed not being able to clear selection by clicking in empty space in the Hierarchy or StyleSheets panes.

## [0.9.0] - 2019-11-25

- Explorer:
    - Split up the Hierarchy and StyleSheets sections into their own separate panes.
    - Moved the new selector controls that were previously at the bottom of all the selectors in the StyleSheet section to a new toolbar in the newly separate StyleSheets pane.
    - Removed the "Class/Complex" modes in the new selector controls. The field now always allows for any type of selector, it just starts off with a "." already typed for you - defaulting to the style class selector.
    - The new selector States dropdown is no longer a mode. It is now menu of pseudo states which are appended to the current selector string.
    - The new selector field will now properly reset and refocus after adding a selector to allow for easy chaining of new selector creation.
    - The new selector toolbar now has an explicit Add button to not just rely on Enter being pressed to create the new selector.
    - When the new selector field has focus, the selector writing cheatsheet, previously available in the Inspector when selecting the StyleSheet, will now appear as a tooltip.
    - The StyleSheets explorer now fully colorizes all parts of each selector using the same color scheme as the Hierarchy.
    - What previously where the main tree view items for "the StyleSheet" and "the Hierarchy" still exist in the corresponding new pane as the sole root items, but now they display the filename they correspond to.
- Other:
    - All style class pills in the StyleSheets pane and in the Inspector will now have yellow text to match the class names in the Hierarchy.
    - Renaming, moving, or deleting a `.uxml` or `.uss` that is currently open in the UI Builder will now give you the option to abort the operation or reset the Builder and lose any unsaved changes.
- Fixes:
    - The UI Builder no longer explodes if a UXML or USS file currently open is deleted or renamed.
    - Changing the value of compound fields like margin or padding will properly reset the "fake" new VisualElement size and border.
    - Fixed not being able to drag elements into the empty part of the Hierarchy pane.

## [0.8.4] - 2019-11-05

- (2020.1) Fixed UXML Template Instances created in the UI Builder not properly being saved to the UXML asset.
- (2020.1) Fixed VisualElements created from the Library not getting added to the generated UXML.

## [0.8.3] - 2019-10-10

- Fixed inability to type a Canvas size in the Canvas Inspector because the min-value validation would kick in on every key press.
- (2019.3+) Fixed Viewport toolbar visual glitches with recent **2019.3.0b6+** and **2020.1.0a7+** versions.
- (2019.3+) Fixed Explorer toolbar settings menu (3 dots) visual glitch with recent **2019.3.0b6+** and **2020.1.0a7+** versions.
- (2020.1) Fixed compilation errors and various bugs when using recent trunk: **2020.1.0a7+**

## [0.8.2] - 2019-09-20

- Updated documentation after pass by docs team.
- Added support for the Runtime theme.
- Added back OptionsPanel sample and fixed it to work in the Assets folder after being imported.
- Added four new Samples derived from the Unite CPH 2019 Tanks demo.
- Added support for `resource()` function in USS which will be used if the asset being referenced is inside a **Resources** folder.
- Renamed Inspector "Inherited Styles" section to just "StyleSheet".
- Renamed Inspector "Local Styles" section to "Styles" if inspecting a selector, or "Inlined Styles" if inspecting an element.
- Removed exposed UI Builder test windows from the Tests menu.
- Fixed the way the Builder's own package version was being retrieved for display in the Viewport header.
- Fixed Project section of the Library to properly handle elements at the root Project path (`Assets/`) or root namespace.
- Fixed the Library not updating when deleting or renaming an entire folder from the Unity Project window containing `.uxml` assets.
- (2020.1) Fixed compilation errors with latest trunk.

## [0.8.1] - 2019-09-16

- Removed broken sample.
- Fixed fatal null reference exception at startup if package info is not available.

## [0.8.0] - 2019-09-15

- Added full documentation.
- Toolbar:
    - Moved **New**, **Save**, and **Save As** buttons in the Toolbar to a new **File** menu in the Toolbar.
    - Added **Open...** option to the new **File** menu that opens a file selection dialog.
    - Removed the UXML Asset Object Field from the Toolbar.
    - Added name of currently open UXML Asset in the **Viewport** title bar, faded, and with a `*` when there are unsaved changes.
    - Added UI Builder package version to the **Viewport** title bar.
- Inspector:
    - Updated Inspector icons.
    - Inspector style class pills in the **Inherited Styles** section will now show faded if there is no single-class selector in the main StyleSheet.
    - Double-clicking on a style class pill in the **Inherited Styles** section it will now select the corresponding single-class selector in the main StyleSheet, if one exists, otherwise it will create it.
    - The label of the header field of a group of number fields (ie. border width) now has a dragger manipulator so you can drag-change all grouped fields (border sides) at once.
    - (2019.3+) Border color style fields are now grouped under a Foldout with a header field that changes all 4 sides at once.
- Canvas:
    - Added Canvas inspector. Access by selecting the Hierarchy item in the Explorer.
    - Can add a custom Canvas background color, image, or camera view (with an Opacity setting).
    - Removed **Game** button from Toolbar.
    - Added and now enforcing a minimum Canvas size.
    - Canvas size will now be remembered for each asset and restored when loading the asset. It also means it will survive Editor restarts.
- Other:
    - UXML and USS Preview section now starts off minimized.
    - Previously open document will be re-opened after a Unity Editor restart. Unsaved changes will still be discarded on Editor quit.
    - Added PropertyField, ProgressBar, ScrollView, and ListView to the Library.
    - Removed the default namespace from UXML generation, aliased the `UnityEngine.UIElements` namespace to `ui:`, and added `ui:` to the main `<ui:UXML>` tag.
- Fixes:
    - Fixed `<Style>` tag staying on root elements that are reparented to no longer be at the root, leading to multiple assignments of the same StyleSheet.
    - Fixed custom C# elements inside different namespaces not working because the default namespace in UXML was `UnityEngine.UIElements`.
    - Fixed UXML generation when a custom element had "UnityEngine.UIElements" in its full type but did not start with it.
    - Fixed copy/paste/duplicate of elements with inlined style background images or relative stylesheet paths.
    - Fixed pasting not always putting at the end of the selected element's parent's children list.
    - Fixed first creation VisualElement helper visualizer and sizer not keeping the shape of its VisualElement if the parent element was set to flex-direction: row.
    - Fixed Canvas manipulators not properly staying on top of the target element sometimes when the Canvas itself was being resized.
    - Fixed Canvas manipulators not accounting for the target element's parent's border widths, causing jitters.
    - Fixed uxml assets with the same names all being disabled in the Library when one of them was open.
    - Fixed the asset name in the Toolbar ObjectField not updating after a new file Save or a Save As.
    - Fixed Save As no properly rename the StyleSheet references in the new asset.
    - Fixed Save As default folder path not being properly set to current document's folder in some cases.
    - Fixed adding a new selector via the Explorer not updating the currently selected element's Inspector.
    - (2019.3+) Fixed duplicate `<Style>` tags appearing in the UXML after a Save As, one referencing the old StyleSheet.

## [0.7.0] - 2019-09-06

- Major rework of the document managment to allow live updates where the assets are being used (Editor and Runtime). The UI Builder now operates directly on the imported assets, instead of copies.
- Added copy/paste/cut/duplicate support for USS Selectors, including pasting to/from a text file.
- (2019.3+) Added support for the `src` attribute in UXML for the `<Style>` and `<Template>` tags. A relative path to a .uss or .uxml asset will be used if the asset is in the same folder (directly or in a subfolder) as the main asset. Otherwise, an absolute path will be used.
- Style category headers will now have an override bar and bold font if any child styles are overridden.
- Added the ability to select elements inside a Template instance or a C# element. When selecting such an element, all fields in the Inspector will be disabled. Also, such selections are only allowed in the Explorer, not the Canvas.
- Added "Fit Canvas" button in the Viewport that appears when the Canvas is bigger than the Viewport (an previously unrecoverable state), and that will resize the Canvas to fit the Viewport.
- Changed the options for the unsaved changes dialog on exit/new from "Yes" and "Cancel" to "Discard" and "Go Back", respectively.
- Fixed infinite recursion when instancing a UXML template within itself by disabling the currently open UXML file entry in the Library.
- Fixed being able to select the In-Explorer New Selector field/buttons row.
- Fixed Inspector scroll position resetting after undo/redo, selection change, and window reloads.
- Fixed Inspector foldouts sometimes not preserving expanded states properly.
- Fixed being able to resize panes until they are no longer visible. All panes now have enforced minimum dimensions.
- Fixed the Inspector binding to the wrong USS Selector when inspecting duplicate selectors (same selector string).
- Fixed error when saving new files to a folder that does not exit. Folders will now be created if missing.
- Fixed length styles with 0 as their value no longer getting "px" added.
- Fixed getting stuck navigating with the arrow keys in the Hieararchy when encountering an expanded Template instance of C# element.
- Fixed immediate de-selection of selected item in Explorer if clicking to the left of the tree item arrow.
- Fixed some items in the Explorer incorrectly appearing grayed out sometimes.
- Fixed folders directly inside the Assets folder appearing at the root of the Library Project tree.
- Fixed aggregate dimension fields (FoldoutWithField) not setting values properly if values were not previously set.
- (2019.3+) Fixed handling of user-made USS loaded with length properties that are missing units (and being loaded as floats).
- (2019.3+) Fixed aggregate dimension fields (FoldoutWithField) throwing null exceptions if values were not previously set.

## [0.6.2] - 2019-08-29

- Fixed long style class name appearance in the Builder Pills. Long names will now be capped and an ellipsis (...) added.
- Fixed warnings and malformed USS/UXML generation when selecting None for the Font style. Will now disallow setting None and give a Log message saying None is disallowed.
- Fixed pressing backspace or delete in the new-selector field in the Explorer deleting the selected element.
- Fixed Align Items style being wrongly bound to Align Content.
- Fixed styles on new selector created via Extract Local Styles to New Class not affecting the original element.
- Fixed UIElements EditorWindow Creation Dialog not working and throwing errors with the UI Builder package installed.
- (macOS) Fixed copy/paste/duplicate creating double the amount of elements.
- (2019.3+) Fixed Opacity style in the Inspector not live updating the opacity in the canvas.
- (2019.3+) Fixed domain reloads breaking unsaved UXML documents with StyleSheet not found errors.
- (2019.3+) Fixed style groups (using FoldoutWithField, ie. margin-left, -right, ...) not working for Dimension styles in the Inspector.
- (2020.1+) Fixed Button Strip styling to account for new Button borders.

## [0.6.1] - 2019-08-21

- Fixed USS Preview not updating when creating new selector directly inside the Explorer.
- (2019.3+) Fixed main StyleSheet path not properly being added to root elements.

## [0.6.0] - 2019-08-21

- Redesigned UI/UX:
    - Redesigned UX in Margin & Padding section in the Inspector.
    - Redesigned UX in Background section in the Inspector.
    - Redesigned UX in Text section in the Inspector.
    - Redesigned UX in Display section in the Inspector.
    - Redesigned UX in Flex section in the Inspector.
    - Redesigned UX in Align section in the Inspector. Align section toggle button strips will change icons depending on the value of the flex-direction style.
- Shared Styles Improvements:
    - Renamed "Shared Styles" header item in the Explorer to "StyleSheet".
    - Renamed "Shared Styles" section in Inspector to "Inherited Styles".
    - Renamed "Local Style Overrides" section in Inspector to "Local Styles".
    - Added new **Extract Local Styles to New Class** button for extract all set Local (inline) Styles of an element to a new style class in the main StyleSheet.
    - When hovering or selecting a style selector in the Explorer, all elements in the Canvas that match this selector will highlight.
    - Added ability to add new StyleSheet selectors from inside the Explorer pane without going to the Inspector.
    - When hovering over elements in the Canvas, all Explorer entries of style selectors that match this element will highlight.
- Other Improvements:
    - Added ability to double-click on elements in the Hierarchy (or right-click > Rename) to rename elements in-place.
    - When hovering over elements in the Canvas, the corresponding entry in the Hierarchy will highlight.
    - When changing Width or Height in the Inspector, the corresponding resize handles in the Canvas will highlight.
    - Will now expose all Unity, UnityEngine, and UnityEditor, namespaced custom VisualElements in the Library if Developer Mode is enabled.
    - Save Dialog initial path will no longer be based on currently selected path in the Project Browser. It will now default to "Assets/" or the location of the previously saved document.
- Fixes:
    - Fixed Save Dialog path fields pushing the "..." buttons out of the dialog when the path string was long.
    - Fixed a null reference exception being thrown when double clicking on a section of controls or project folder in the Library.
    - Fixed regression where Canvas resize handles would not stay highlighted (white) while being dragged.
    - Fixed Font Style style not working at all because it was setting the -unity-font-style-and-weight property instead of -unity-font-style.
    - (2019.3) Updated code and styling for **2019.3.0a12**.

## [0.5.8] - 2019-08-06

- Restructured the Border section of the Local Style Overrides inspector to more easily set all values at once.
- Added "Open" button to Library items that have equivalent .uxml assets for opening them in the UI Builder.
- Made it easier to navigate the Local Overrides Section of the Inspector by visually breaking up the categories.
- Added toolbar to Explorer section with menu for always hiding/showing the element type, name, and/or style classes.
- Added "..." buttons to path fields in the Save Dialog for prompting the system Save File Dialog for easier path selection.
- Added right-click context menu for Copy/Paste/Duplicate/Delete to the Hierarchy.
- Allowed split sections to get smaller so the UXML/USS preview section can be almost entirely hidden now.
- Fixed UXML assets inside Packages not showing up in the Library.
- Fixed StyleSheets in the Resources folder referenced in UXML not working in the Builder.
- Fixed the Save Dialog error box icon sometimes showing in the top left corder of the Builder.
- Fixed url() in USS when the object reference is null to write "none".

## [0.5.7] - 2019-07-24

- Added Foldout to the Library.
- Major refactor of domain reload, window reload (F5), and window maximizing survival logic. There should be no more flickering when any serialization events happen and the builder state will now always survive, even when the Layout is changed or the Builder window is closed and reopened.
- Added check for unsaved changes when quitting the Unity Editor.
- Added first sample UI in Samples.
- Added back Game preview button that replaces canvas background with main camera view.
- Added back preview on hover in the Library for built-in controls.
- Library preview on hover will now display elements using the currently selected Theme for the canvas.
- Fixed inability to save UXML/USS docs to the "Packages/" folder by remove auto prepending of "Assets/" to the path and adding path validation checks in the Save Dialog.
- Fixed dashes wrongly being disallowed in style class names in the Inspector.
- Fixed Editor theme overrides not properly overriding the text color (ie. Light theme used in Dark themed Editor would still use white text color).
- Fixed the Font style field not properly filtering for the Font asset type or allowing you to set it.
- Fixed children not being hidden/unhidden when change the parent's Overflow style property.
- Fixed Size section of Inspector to properly show the bold override field style in all cases.
- Fixed Viewport toolbar theme selection dropdown to be properly styled for the toolbar.
- Fixed url() in USS not getting the starting "/" for absolute paths when using paths inside the Packages folder.
- Fixed USS Preview not updating when Builder window was Maximized.
- (2019.3) Added support for border-(left/right/top/bottom)-color styles.
- (2019.3) Fixed for and bumped trunk version to **2019.3.0a10**.

## [0.5.6] - 2019-07-04

- (2019.3) Fixed for and bumped trunk version to **2019.3.0a9**.

## [0.5.5] - 2019-06-25

- Fixed url() paths being added to UXML style attributes with unescaped double quotes.
- Fixed url() in USS not getting the starting "/" for absolute paths.

## [0.5.4] - 2019-06-20

- (2019.3) Fixed copy/paste of elements with Length-type inline styles improperly importing and causing errors.

## [0.5.3] - 2019-06-20

- Added support for saving with Ctrl+S (or Command+S).
- Changed left-hand blue bar on fields for value overrides to white/black.
- Changed "Empty Element" to "VisualElement" in Library.
- Fixed "Assets/" being added to new file save path when the Assets root folder was currently selected in the Project Browser.
- Fixed saving of an empty document creating an invalid UXML asset.
- Fixed Save As dialog matching uss file name to uxml file name even if the loaded uss file name was different from the uxml name.
- Fixed dummy child element of an empty VisualElement appearing in the Hierarchy after initial creation.
- Fixed elements having opacity set to 0.5 right after initial creation via drag from the Library.
- Fixed validation errors in saved USS asset due to border-width styles not having units added.
- (2019.3) Fixed duplicate style and class attributes in generated UXML.
- (2019.3) Fixed glitchy overlapping text on top of Explorer header.

## [0.5.2] - 2019-06-19

- Removed use of the element preview window on Library mouse hover for the built-in Unity controls.
- The .uxml and .uss extensions will now be automatically applied if missing from the user supplied paths when saving.
- In the save dialog, changing the UXML path will now also update the USS path to match - until you manually change the USS path.
- The Builder will now properly save to the correct assets, even if they have been moved or renamed (in which case it will also update the USS Style paths inside the UXML document).
- Canvas size will now be restored after Domain Reload or Window reload. It will be reset when opening a new document.
- Fixed extra '>' appearing in the Explorer between selector parts.
- Fixed null ref exception when deleting an element while the mouse was over the element in the viewport.
- Fixed being able to save uxml/uss files outside the Assets folder.
- Fixed code previews overlapping the Inspector when the Viewport was too small.
- (2019.3) Fixed toolbar buttons shrinking when toolbar width got too small.

## [0.5.1] - 2019-06-18

- Improved UX for size style fields in the Inspector.
- Fixed Save dialog displaying .uxml file extensions as initial value for .uss path.
- (2019.3) Fixed saving and loading of embedded USS paths in the UXML document.
- (2019.3) Fixed Save As dialog missing Cancel button printing rendering errors in console.
- (2019.3) Fixed glitchy overlapping text artifacts in the Inspector pane.

## [0.5.0] - 2019-06-14

- Added support for reordering of elements in the Hierarchy, with live preview in the canvas.
- Added support for dragging from the Library to the Hierarchy in between elements to place elements at a specific index, with preview.
- New empty VisualElements will now have an artificial minimum size that will be reset if a child is added or its styling is changed in any way.
- Added Light/Dark Theme preview mode to the Toolbar, independent from the current Editor Theme.
- Hovering over items in the Library will show a preview of that element in a floating preview box.
- Added style class name validation when adding it to an element in the Inspector.
- Can now press enter to add a style class to an element in the Inspector.
- Library pane will now update if new uxml files are added/deleted/move/renamed to/from the project.
- Removed PopupField from the Library as it has no UXML support right now.
- Fixed Save/SaveAs dialog to look correctly when using the Editor Light skin.
- Fixed error and stuck preview when dragging over an EnumField from the Library to the Viewport.
- Added support for **2019.3.0a6**.
- Added installation instructions to README.

## [0.4.2] - 2019-06-08

- Cut/copy/duplicate/paste of elements now only work while the Viewport or the Explorer are focused.
- Save As dialog now uses the name of the current document as the initial new file name.
- Fixed Backspace attempting to delete the selected element while a field in the Inspector was focused.
- Fixed regression with the anchors resizing the element slightly when activated.
- Fixed delete, backspace, and pasting working in the code previews.

## [0.4.1] - 2019-06-06

- Added blue highlight at the top of the currently focused pane.
- Selection overlay will hide when going into Preview mode and re-appear if selecting something else or going out of Preview mode.
- Added support for copy/pasting elements to and from a text file as valid UXML.
- Expanded cut/copy/duplicate/paste to work recursively on all children of the selected element.
- Added support for cut/copy/duplicate/paste of Template instances.
- Moved Local Style Overrides and Attributes fields tooltips to be centered on just their Labels.
- Added border highlight hover effect when moving mouse over canvas elements.
- Fixed Cut deleting the element before the Paste.
- Fixed pasted elements not being added at the end of the list of siblings.
- Fixed duplicate elements being sometimes created after a paste.
- Fixed resize handles being offset if element selected during Preview mode.
- Fixed expanded states in the Explorer not properly persisting or breaking when reparenting.
- Fixed Delete (Backspace) key not working on Mac unless the Fn or Shift key is also pressed.
- Fixed Preview orange border disappearing if the canvas is bigger than the viewport.

## [0.4.0] - 2019-06-04

- Added cut/copy/duplicate/paste support for VisualElements.
- Inspector local style overrides sections has been rewritten, with each section getting a dedicated uxml layout instead of being generated from reflection. A new custom binding system is used to bind fields to style values, meaning multiple fields can bind to the same style value.
- Inspector local style override fields now have nice looking names, instead of USS property names.
- Tooltips added to each style field displaying the USS style property name.
- Added **Save As** button/option to the toolbar.
- Added blue bars next to overridden styles and removed blue text to match prefab overrides UI.
- Added support for editing element type-specific attributes, like name, text, and tooltip. This change also fixes the previously limited support for changing the text attribute. Includes support for undo/redo and reverting to default.
- Can now drag style class pill onto elements in hierarchy.
- Can now drag items from the Library to the hierarchy.
- Added initial documentation draft, listing all features.
- Renamed Selector text field to "Selector" from "Name" in the Selector and StyleSheet Inspectors for clarity.
- Fixed inspector flickering when using undo/redo or sometimes when using the canvas handles.
- Fixed the root stylesheet path being added to all elements in UXML, instead of just the root elements.
- Fixed being able to reparent elements inside template instances or C# elements.
- Fixed look when using Unity Light theme.
- Fixed occasional drag-and-drop instabilities.
- Fixed being able to parent an element under one of its children.
- (2019.3) Fixed mis-handling of Dimension styles value types as Floats.
- (2019.3) Fixed compilation issues and bugs specific to **2019.3.0a4**. Any newer trunk versions will not work. Attributes, apart for **Name** will do not work at all in this version.

## [0.3.1] - 2019-05-28

- You can now create new USS Selectors by pressing Enter (instead of using the button).
- Clicking (and not dragging) on a style class pill in the Shared Styles explorer will now select the selector.
- Added VERY brief help section in the Shared Styles Inspector to help with selector creation.
- Added .uxml to Library entries representing UXML assets in the project.
- Fixed template instance uxml file name appearing before element name in Hierarchy.
- Fixed inspector style fields not updating after adding/removing a style class on an element.
- Fixed enum styles not displaying accurate state after reselecting the shared style/element.
- Fixed shared styles not re-applying after changing the selector name.
- (2019.3) Fixed unable to parse file error when builder is opened with a new file.
- (2019.3) Fixed elements getting a content-container attribute set in UXML, leading to corrupt UXML generation.
- (2019.3) Fixed unhandled type Dimension error when resizing the canvas.
- (2019.3) Fixed various styling issues in the N* theme.

## [0.3.0] - 2019-05-27

- Switched from using the live canvas VisualElement hiearchy as the source of truth to using the actual VisualTreeAsset (UXML asset). Nothing major should be noticed as a result of this refactor. It just makes certain future improvements possible.
- No longer generating element names and adding name-based selectors in the global StyleSheet for Local Style Overrides. Instead, Local Style Overrides will be written as inline-styles on the element in UXML. The global StyleSheet will only be used for shared styles.
- The main StyleSheet path will now be added to all root elements in the UXML document. This allows the Builder to now work with any externally created UXML file by removing the reliance on a special root element.
- Finished and fixed support for creating Template Instances (using UXML documents inside other UXML documents).
- Elements inside a Template Instance will appear grayed out in the Hierarchy.
- Selecting elements inside a Template Instance will now select the TemplateContainer.
- TemplateContainers now have their .uxml file names listed in the Hierarchy.
- Added Undo/Redo support.
- Added right-click menu on style override fields to Unset an overridden style.
- Added support for opening a UXML document via double-click in the Project Browser.
- Added "px" units on Length styles in generated USS.
- Added support for Background, Font, and Cursor style property types in the Inspector.
- Removed top-level menus for SplitView and Builder. The UI Builder window is now under: **Window > UI > UI Builder**.
- Removed the "New (Test)" toolbar button in non-development mode.
- Hierarchy no longer shows elements with their first style class. Name if it is set; type otherwise.
- Fixed name/tab of Builder Window becoming corrupt.
- Fixed flickering while dragging elements from the Library.
- Fixed being able to drag-and-drop a Style class onto the main document element.
- Fixed -unity* custom style properties being written to USS without the starting "-".

## [0.2.1] - 2019-05-10

- Added confirmation dialog when discarding unsaved changes.
- Added tracking of unsaved changes via * in EditorWindow title.
- Added ability to change an existing style selector in its Inspector.
- Fixed shared styles selectors only showing first word in Explorer.
- Fixed style selector selections in the Explorer not surviving domain reload.
- Fixed Shared Styles sections sometimes showing a duplicated list of selectors.

## [0.2.0] - 2019-05-10

- Early iteration of shared styles.
- Draggable style classes onto elements.
- Style class list managemenent in the Inspector.
- Re-parenting of elements via drag-and-drop in the Hierarchy.
- Library now containes user defined templates from UXML and source factories.

## [0.1.0] - 2019-04-15

### This is the first release of *Unity Package UI Builder*.

- Basic element creation, manipulation, and editing.
- Loading and saving of UXML documents.
- Preview mode.
- Drag and drop of elements from Library.
- Resize, move, and anchoring manipulators.
- Basic hierarchy view with selection preview.
- Basic styles Inspector.
