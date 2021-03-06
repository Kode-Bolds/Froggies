using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Froggies
{
    public struct PathFinding : IComponentData
    {
        public bool requestedPath;
        public bool completedPath;
        public int2 currentNode;
        public int2 targetNode;
        public int currentIndexOnPath;
    }

    public struct HasPathTag : IComponentData
	{

	}

    [InternalBufferCapacity(256)]
    public struct PathNode : IBufferElementData
    {
        public float3 position;
        public int2 gridPosition;
    }
    
    public class PathFindingComponent: MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            PathFinding pathFinding = new PathFinding();

            dstManager.AddComponentData(entity, pathFinding);
        }
    }
}

