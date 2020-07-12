//using System.Collections;
//using System.Collections.Generic;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Transforms;
//using UnityEngine;

//[DisableAutoCreation]
//public class PatrolControlPointsSystem : KodeboldJobSystem
//{
//    private ControlPointsBlobRef m_controlsPointRef;
//    private EntityQuery m_query;

//    public override void GetSystemDependencies(Dependencies dependencies)
//    {

//    }

//    public override void InitSystem()
//    {

//    }

//    protected override void OnCreate()
//    { 
//        m_query = GetEntityQuery(ComponentType.ReadOnly<ControlPointsBlobRef>());
        
//    }

//    public override void UpdateSystem()
//    {
//        m_controlsPointRef = m_query.GetSingleton<ControlPointsBlobRef>();

//        BlobAssetReference<ControlPointsBlobData> controlPointsLocal = m_controlsPointRef.controlPoints;

//        Entities.ForEach((ref Translation translation, ref AITarget target, in UnitMove movespeed) =>
//        {
//            float3 targetPoint = controlPointsLocal.Value.positions[target.controlPointIndex];
//            float3 directionToTarget = math.normalize(targetPoint - translation.Value);
//            translation.Value += movespeed.moveSpeed * directionToTarget;

//            if (math.abs(math.length(translation.Value - targetPoint)) < 0.1f)
//            {
//                target.controlPointIndex++;
//            }
//            target.controlPointIndex %= controlPointsLocal.Value.positions.Length;
//        }).ScheduleParallel();
//    }
//}
