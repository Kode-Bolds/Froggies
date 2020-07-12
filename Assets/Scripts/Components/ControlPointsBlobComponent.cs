using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct ControlPointsBlobRef : IComponentData
{
    public BlobAssetReference<ControlPointsBlobData> controlPoints;  
}

public struct ControlPointsBlobData
{
    public BlobArray<float3> positions;
}

public class ControlPointsBlobComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField] Transform[] m_serializedControlPoints;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        ControlPointsBlobRef controlPointsRef = new ControlPointsBlobRef();
        controlPointsRef.controlPoints = CreateBlobData();

        dstManager.AddComponentData(entity, controlPointsRef);
    }

    private unsafe BlobAssetReference<ControlPointsBlobData> CreateBlobData()
    {
        using (BlobBuilder builder = new BlobBuilder(Allocator.Temp))
        {
            //Allocate root
            ref ControlPointsBlobData blobRoot = ref builder.ConstructRoot<ControlPointsBlobData>();

            //Allocate array
            BlobBuilderArray<float3> controlPointArray = builder.Allocate(ref blobRoot.positions, m_serializedControlPoints.Length);

            //Fill in array values
            for (int i = 0; i < m_serializedControlPoints.Length; ++i)
            {
                controlPointArray[i] = m_serializedControlPoints[i].position;
            }

            //float3[] arr = new float3[4];

            //fixed (void* ptr = &arr[0])
            //{
            //    UnsafeUtility.MemCpy(controlPointArray.GetUnsafePtr(), ptr, arr.Length * sizeof(float3));
            //}

            return builder.CreateBlobAssetReference<ControlPointsBlobData>(Allocator.Persistent);
        }
    }
}


