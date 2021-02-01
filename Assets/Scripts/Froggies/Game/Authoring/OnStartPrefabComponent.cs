using Unity.Entities;

namespace Froggies
{
    [GenerateAuthoringComponent]
    public struct OnStartPrefabData : IComponentData
    {
        public Entity resources;
    }
}