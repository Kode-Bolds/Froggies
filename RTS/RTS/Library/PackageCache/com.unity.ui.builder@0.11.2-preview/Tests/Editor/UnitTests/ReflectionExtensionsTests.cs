using System;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    class ReflectionExtensionsTests
    {
        static readonly string s_ButtonTextValue = "MyButton";

        [Test]
        public void HasValueByReflection()
        {
            var button = new Button(null);

            Assert.IsTrue(button.HasValueByReflection("text"));
            Assert.IsFalse(button.HasValueByReflection("not_existing_property"));
        }

        [Test]
        public void GetValueByReflection()
        {
            var button = new Button(null) {text = s_ButtonTextValue};

            Assert.IsTrue(button.GetValueByReflection("text").Equals(button.text));
            Assert.Throws<ArgumentException>(() => button.GetValueByReflection("not_existing_property"), ReflectionExtensions.s_PropertyNotFoundMessage);
        }

        [Test]
        public void SetValueByReflection()
        {
            var button = new Button(null);

            button.SetValueByReflection("text", s_ButtonTextValue);
            Assert.IsTrue(button.text.Equals(s_ButtonTextValue));
            Assert.Throws<ArgumentException>(() => button.SetValueByReflection("not_existing_property", s_ButtonTextValue), ReflectionExtensions.s_PropertyNotFoundMessage);
        }
    }
}