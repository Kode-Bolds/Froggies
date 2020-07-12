using Unity.Entities;

public static class NativeContainerExtensionMethods
{
	public static bool TryGetComponentDataFromEntity<T>(this ComponentDataFromEntity<T> componentDataFromEntity, Entity entity, out T component) where T : struct, IComponentData
	{
		if (componentDataFromEntity.Exists(entity))
		{
			component = componentDataFromEntity[entity];
			return true;
		}

		component = default;
		return false;
	}
}
