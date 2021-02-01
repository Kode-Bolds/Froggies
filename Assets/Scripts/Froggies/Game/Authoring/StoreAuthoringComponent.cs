using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Froggies
{
    public class StoreAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
    {
        [HideInInspector] public Store storeComponent;
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            Transform transform = GetComponent<Transform>();
            Debug.Assert(transform.localScale.x == transform.localScale.z, "Must have a uniform scale on x and z axis!");

            float radius = GetComponent<PhysicsShapeAuthoring>().GetSphereProperties(out quaternion _).Radius;
            float scale = transform.localScale.x;
            storeComponent.depositRadius = radius * scale;
            dstManager.AddComponentData(entity, new TargetableByAI { targetType = AITargetType.Store });
            dstManager.AddComponentData(entity, storeComponent);
        }
    }
}