using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    public static class Style
    {
        public static DisplayConstraint Display(DisplayStyle expected)
        {
            return new DisplayConstraint(expected);
        }
    }

    public class DisplayConstraint : Constraint
    {
        readonly DisplayStyle m_Expected;

        public DisplayConstraint(DisplayStyle expected)
        {
            m_Expected = expected;
            Description = expected.ToString();
        }

        public override ConstraintResult ApplyTo(object actual)
        {
            if (actual is VisualElement visual)
                return new ConstraintResult(this, visual.style.display.value, visual.style.display.value == m_Expected);

            return new ConstraintResult(this, $"not a {nameof(VisualElement)}", false);
        }
    }

    public class TestDemo
    {
        public void Demo()
        {
            var ve = new VisualElement();
            Assert.That(ve, Style.Display(DisplayStyle.Flex));
        }
    }
}
