using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("unity_LightmapIndex"           , MaterialPropertyFormat.Float4  )] public struct BuiltinMaterialPropertyUnity_LightmapIndex : IComponentData { public float4   Value; }
}