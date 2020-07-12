# UI Builder

The **UI Builder** lets you visually create and edit UI assets (UXML and USS) for use with the **UI Toolkit** (formally UIElements). Once the package is installed, the UI Builder window can be opened via the **Window > UI Toolkit > UI Builder** menu, or double-clicking on a `.uxml` asset in the Project Browser.

> Note, for 2019.3 and 2019.2, the UI Builder window can be found in **Window > UI > UI Builder**.

**Internal Developers:** Please **join our [#devs-uibuilder](https://unity.slack.com/archives/CJ3TX00QJ) Slack channel** for feedback and questions.

## Installation

Unity versions supported:
- **2019.2**: 2019.2.21f1 or newer
- **2019.3**: 2019.3.12f1 or newer
- **2020.1**: 2020.1.0b7 or newer
- **2020.2**: 2020.2.0a9 or newer

To install:
1. Open the **Window > Package Manager**.
    * For 2019.2, 2019.3, and older 2020.1, enable **Advanced > Show preview Packages**: ![Enable Preview Packages](Documentation~/images/InstallationPackageManagerAdvancedOptions.png)
    * For newer 2020.1 and 2020.2+, go to **Edit > Project Settings... > Package Manager**, and enable check **Enable Preview Packages**:
    ![Enable Preview Packages (new)](Documentation~/images/InstallationPackageManagerEnablePreview.png)
1. Go back to the **Window > Package Manager**.
1. Search for `UI Builder`:![Search Package Manager](Documentation~/images/InstallationPackageManagerSearch.png)
1. Press **Install**.

## Documentation

![UI Builder Main Window](Documentation~/images/UIBuilderAnnotatedMainWindow.png)

### ![1](Documentation~/images/Numeral_1_half.png) Explorer
* **StyleSheet:** Create USS selectors for sharing common styling between multiple elements.
* **Hierarchy:** Current document element tree.
### ![2](Documentation~/images/Numeral_2_half.png) Library
* **Unity Elements:** Built-in Unity elements.
* **Project Elements:** Custom user elements like other `.uxml` templates in the current project.
### ![3](Documentation~/images/Numeral_3_half.png) Viewport
* **Toolbar:** Can Save/Load, change the Theme and activate Preview mode.
* Currently selected element with manipulation handles.
* Edit-time Canvas for editing and previewing current document with optional edit-time-only background image.
### ![4](Documentation~/images/Numeral_4_half.png) Code Previews
* **UXML Preview:** Preview of the generated UXML hierarchy asset.
* **USS Preview:** Preview of the generated USS styles asset.
### ![5](Documentation~/images/Numeral_5_half.png) Inspector
* **Attributes:** Change attributes, like element name, that are set in the UXML document.
* **Inherited Styles:** Add/remove style classes and see which selectors match the current element.
* **Local Styles:** Override styles on the current element, inlined in the UXML document.

For more info, see our [documentation page](Documentation~/index.md).
