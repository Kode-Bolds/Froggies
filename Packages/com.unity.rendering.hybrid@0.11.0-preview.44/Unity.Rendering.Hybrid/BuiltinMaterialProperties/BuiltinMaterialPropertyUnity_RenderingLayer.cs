using Unity.Entities;
using Unity.Mathematics;

#if ENABLE_HYBRID_RENDERER_V2
namespace Unity.Rendering
{
    [MaterialProperty("unity_RenderingLayer", MaterialPropertyFormat.Float4)]
    public struct BuiltinMaterialPropertyUnity_RenderingLayer : IComponentData
    {
        public uint4 Value;
    }

    [MaterialProperty("unity_RenderingLayer", MaterialPropertyFormat.Float4)]
    public struct BuiltinMaterialPropertyUnity_RenderingLayer_Shared : IHybridSharedComponentFloat4Override
    {
        public uint4 Value;

        public float4 GetFloat4OverrideData()
        {
            return math.asfloat(Value);
        }
    }
}
#endif
