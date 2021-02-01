using Unity.Entities;

namespace Froggies
{
    [GenerateAuthoringComponent]
    public struct RuntimePrefabData : IComponentData
    {
        public Entity aiDrone;
    }
}