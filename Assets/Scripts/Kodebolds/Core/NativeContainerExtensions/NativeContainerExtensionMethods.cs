using Unity.Entities;
using Unity.Transforms;

namespace Kodebolds.Core
{
	public static class NativeContainerExtensionMethods
	{
		public static bool TryGetComponentDataFromEntity<T>(this ComponentDataFromEntity<T> componentDataFromEntity, Entity entity, out T component) where T : struct, IComponentData
		{
			if (componentDataFromEntity.HasComponent(entity))
			{
				component = componentDataFromEntity[entity];
				return true;
			}

			component = default;
			return false;
		}

		public static bool TryGetBufferFromEntity<T>(this BufferFromEntity<T> bufferFromEntity, Entity entity, out DynamicBuffer<T> buffer) where T : struct, IBufferElementData
		{
			if (bufferFromEntity.HasComponent(entity))
			{
				buffer = bufferFromEntity[entity];
				return true;
			}

			buffer = default;
			return false;
		}

		public static void DestroyEntityWithChildren(this EntityCommandBuffer ecb, Entity entity, BufferFromEntity<Child> childrenLookup)
		{
			if (TryGetBufferFromEntity(childrenLookup, entity, out DynamicBuffer<Child> children))
			{
				for (int childIndex = 0; childIndex < children.Length; childIndex++)
				{
					ecb.DestroyEntityWithChildren(children[childIndex].Value, childrenLookup);
				}
			}

			ecb.DestroyEntity(entity);
		}

		public static void DestroyEntityWithChildren(this EntityCommandBuffer.ParallelWriter ecb, int sortKey, Entity entity, BufferFromEntity<Child> childrenLookup)
		{
			if(TryGetBufferFromEntity(childrenLookup, entity, out DynamicBuffer<Child> children))
			{
				for(int childIndex = 0; childIndex < children.Length; childIndex++)
				{
					ecb.DestroyEntityWithChildren(sortKey, children[childIndex].Value, childrenLookup);
				}
			}

			ecb.DestroyEntity(sortKey, entity);
		}
	}
}