using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    class ImageStyleField : MultiTypeField
    {
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<ImageStyleField, UxmlTraits> {}

        public ImageStyleField() : this(null) {}

        public ImageStyleField(string label) : base(label)
        {
            AddType(typeof(Texture2D), "Texture");
        }

        public void TryEnableVectorGraphicTypeSupport()
        {
#if UNITY_2019_3_OR_NEWER
            AddType(typeof(VectorImage), "Vector");
#endif
        }
    }
}
