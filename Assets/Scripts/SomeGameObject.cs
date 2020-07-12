using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Transforms;
using Unity.Jobs;

public struct ComponentData : IComponentData
{

}

[InternalBufferCapacity(10)]
public struct BufferData : IBufferElementData
{

}

public struct BlobDataRef : IComponentData
{
	public BlobAssetReference<BlobData> blobData;
}

public struct BlobData
{
	public int data;
	public int moreData;
	public BlobArray<int> blobArray;
}

public class SomeGameObject : MonoBehaviour, IConvertGameObjectToEntity
{
	public int data;
	public int moreData;
	public int[] someMoreData;

	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		BlobDataRef blobDataComponent = new BlobDataRef();
		blobDataComponent.blobData = CreateBlobData();

		dstManager.AddComponentData(entity, blobDataComponent);
	}

	public BlobAssetReference<BlobData> CreateBlobData()
	{
		using (BlobBuilder builder = new BlobBuilder(Allocator.Temp))
		{
			ref BlobData blobDataRoot = ref builder.ConstructRoot<BlobData>();

			blobDataRoot.data = data;
			blobDataRoot.moreData = moreData;

			BlobBuilderArray<int> blobArray = builder.Allocate(ref blobDataRoot.blobArray, someMoreData.Length);

			for(int index = 0; index < someMoreData.Length; index++)
			{
				blobArray[index] = someMoreData[index];
			}

			return builder.CreateBlobAssetReference<BlobData>(Allocator.Persistent);
		}
	}

	[DisableAutoCreation]
	public class NormieSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((ref Translation translation) =>
			{

			});
		}
	}

	[DisableAutoCreation]
	public class ThreadedSystem : JobComponentSystem
	{
		private EntityQuery _query;

		protected override void OnCreate()
		{
			_query = GetEntityQuery(ComponentType.ReadOnly<Translation>());
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			NativeArray<Entity> entites = _query.ToEntityArrayAsync(Allocator.TempJob, out JobHandle entitiesJobHandle);
			NativeArray<Translation> translations = _query.ToComponentDataArrayAsync<Translation>(Allocator.TempJob, out JobHandle translationsJobHandle);

			ComponentDataFromEntity<Translation> translations2 = GetComponentDataFromEntity<Translation>(true);

			JobHandle hello = Entities.WithReadOnly(translations).WithDeallocateOnJobCompletion(translations).ForEach((Entity entity, ref Translation translation) =>
		   {
			   Translation translation2 = translations[0];
		   }).Schedule(JobHandle.CombineDependencies(inputDeps, translationsJobHandle));

			JobHandle hello2 = Entities.ForEach((ref Rotation rotation) =>
			{

			}).Schedule(hello);

			return hello2;
		}
	}
}