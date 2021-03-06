using System;
using System.Collections;
using System.Collections.Generic;
using Kodebolds.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Froggies
{
    public class FlockingSystem : KodeboldJobSystem
    {
        public override void GetSystemDependencies(Dependencies dependencies)
        {
        }

        public override void InitSystem()
        {
        }

        public override void UpdateSystem()
        {
            ComponentDataFromEntity<Translation> translations = GetComponentDataFromEntity<Translation>();
            ComponentDataFromEntity<PhysicsVelocity> velocities = GetComponentDataFromEntity<PhysicsVelocity>();
            ComponentDataFromEntity<UnitMove> unitMoves = GetComponentDataFromEntity<UnitMove>();

            Entities
                .WithReadOnly(translations)
                .ForEach((Entity entity, in Flocker flocker, in DynamicBuffer<FlockingGroup> flockingGroup) =>
                {
                    //Get flocking target
                    Entity target = flocker.flockingTarget;
                    Translation targetTranslation = translations[target];
                    float3 targetPos = targetTranslation.Value;
                    PhysicsVelocity targetVelocity = velocities[target];
                    float3 targetDir = math.normalize(targetVelocity.Linear);
                    float targetSpeed = unitMoves[target].moveSpeed;

                    // Get flocking group averages
                    float3 sumDir = default;
                    float3 sumPos = default;
                    
                    for (int i = 0; i < flockingGroup.Length; ++i)
                    {
                        Entity groupedFlocker = flockingGroup[i].entity;
                        Translation groupedTranslation = translations[groupedFlocker];
                        sumPos += groupedTranslation.Value;
                        PhysicsVelocity groupedVelocity = velocities[groupedFlocker];
                        sumDir += math.normalize(groupedVelocity.Linear);
                    }

                    float3 averageGroupPos = sumPos / flockingGroup.Length;
                    float3 averageGroupDir = sumDir / flockingGroup.Length;

                    // Combine target values with averages using given ratio 
                    float3 averagePos = (targetPos * flocker.targetRatio) +
                                        (averageGroupPos * (1 - flocker.targetRatio));
                    float3 averageDir = (targetDir * flocker.targetRatio) +
                                        (averageGroupDir * (1 - flocker.targetRatio));

                    // Cohesion


                    //Separation


                    //Alignment
                    
                    
                }).ScheduleParallel();
        }

        public override void FreeSystem()
        {
        }
    }
}