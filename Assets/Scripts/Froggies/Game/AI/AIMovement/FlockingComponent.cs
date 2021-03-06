using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Conversion;
using Unity.Physics;
using UnityEngine;

namespace Froggies
{
    public struct Flocker : IComponentData
    {
        public Entity flockingTarget;
        public float targetRatio;
        public float cohesionWeight;
        public float separationWeight;
        public float alignmentWeight;
    }


    // We can use this to keep track of which other flockers are moving in our group
    public struct FlockingGroup : IBufferElementData
    {
        public Entity entity;
    }

    public class FlockingComponent : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        public GameObject pathfinder;
        public float targetRatio;
        public float cohesionWeight;
        public float separationWeight;
        public float alignmentWeight;

        private List<GameObject> siblings;
        
        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
           referencedPrefabs.Add(pathfinder);
           for (int i = 0; i < transform.parent.childCount; ++i)
           {
               siblings.Add(transform.parent.GetChild(i).gameObject);
               referencedPrefabs.Add(transform.parent.GetChild(i).gameObject);
           }
        }
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            Flocker flocker = new Flocker
            {
                flockingTarget = conversionSystem.GetPrimaryEntity(pathfinder),
                targetRatio = targetRatio,
                cohesionWeight = cohesionWeight,
                separationWeight = separationWeight,
                alignmentWeight = alignmentWeight
            };
            dstManager.AddComponentData(entity, flocker);

            DynamicBuffer<FlockingGroup> flockingGroup = dstManager.AddBuffer<FlockingGroup>(entity);
            for (int i = 0; i < siblings.Count; ++i)
            {
                flockingGroup.Add(new FlockingGroup{entity = conversionSystem.GetPrimaryEntity(siblings[i])});
            }

            
        }
    }
}