using System;
using Unity.Entities;
using UnityEngine;

namespace Froggies
{
    [Serializable]
    public struct UnitMove : IComponentData
    {
        public float moveSpeed;
        public bool rotating;
        public float angle;
        public float turnRate;
    }

    public class UnitMoveComponent : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] private float m_MoveSpeed;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            UnitMove unitMove = new UnitMove()
            {
                moveSpeed = m_MoveSpeed
            };

            dstManager.AddComponentData(entity, unitMove);
        }
    }
}