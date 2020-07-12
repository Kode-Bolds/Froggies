using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.UI.Builder.EditorTests
{
    class ViewportHeaderTests : BuilderIntegrationTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            CreateTestUXMLFile();
            yield return null;
        }

        protected override IEnumerator TearDown()
        {
            yield return base.TearDown();
            DeleteTestUXMLFile();
        }

        /// <summary>
        /// The currently open UXML asset name, or <unsaved asset>`, is displayed in the Viewport header, grayed out.
        /// </summary>
        [UnityTest]
        public IEnumerator ViewportHeaderTitleText()
        {
            Assert.True(ViewportPane.subTitle.Contains("<unsaved file>"));

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestUXMLFilePath);
            var toolbar = ViewportPane.Q<BuilderToolbar>();
            toolbar.LoadDocument(asset);

            yield return UIETestHelpers.Pause(1);
            Assert.True(ViewportPane.subTitle.Contains(k_TestUXMLFileName));
        }

        /// <summary>
        /// If there are unsaved changes, a `*` is appended to the asset name.
        /// </summary>
        [UnityTest]
        public IEnumerator DocumentUnsavedChangesShouldAddIndicationToTheToolbar()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TestUXMLFilePath);
            var toolbar = ViewportPane.Q<BuilderToolbar>();
            toolbar.LoadDocument(asset);

            yield return UIETestHelpers.Pause();
            Assert.False(ViewportPane.subTitle.Contains("*"));

            AddElementCodeOnly();
            Assert.True(ViewportPane.subTitle.Contains("*"));
        }

        /// <summary>
        /// The current UI Builder package version is displayed in the **Viewport** title bar.
        /// </summary>
        [Test]
        public void CurrentBuilderVersionIsDisplayedInTheTitlebar()
        {
            var packageInfo = PackageInfo.FindForAssetPath("Packages/" + BuilderConstants.BuilderPackageName);
            var builderPackageVersion = packageInfo.version;
            Assert.True(ViewportPane.subTitle.Contains(builderPackageVersion));
        }
    }
}