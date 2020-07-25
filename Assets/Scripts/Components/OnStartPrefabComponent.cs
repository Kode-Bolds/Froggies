using Unity.Entities;

[GenerateAuthoringComponent]
public struct OnStartPrefabData : IComponentData
{
    public Entity resources;
}
