using NUnit.Framework;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class IUxmlFactoryExtensionsTests
    {
        [Test]
        public void GetTraits()
        {
            var buttonUxmlFactory = new Button.UxmlFactory();
            var traits = buttonUxmlFactory.GetTraits();

            Assert.IsNotNull(traits);
            Assert.IsTrue(traits.GetType() == typeof(Button.UxmlTraits));
        }
    }
}