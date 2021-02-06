using Kodebolds.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Froggies
{
    [AlwaysUpdateSystem]
    public class SelectionSystem : KodeboldJobSystem
    {
        private EndSimulationEntityCommandBufferSystem m_entityCommandBuffer;
        private InputManagementSystem m_inputManagementSystem;
        private RaycastSystem m_raycastSystem;
        private NativeArray<float3> m_boxBounds;

        public bool redrawSelectedUnits = false;

        public override void GetSystemDependencies(Dependencies dependencies)
        {
            m_inputManagementSystem = dependencies.GetDependency<InputManagementSystem>();
            m_raycastSystem = dependencies.GetDependency<RaycastSystem>();
        }

        public override void InitSystem()
        {
            m_entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            m_boxBounds = new NativeArray<float3>(2, Allocator.Persistent);
        }

        public override void UpdateSystem()
        {

            bool leftClickPressed = m_inputManagementSystem.InputData.mouseInput.leftClickPressed;
            bool leftClickReleased = m_inputManagementSystem.InputData.mouseInput.leftClickReleased;
            bool leftClickDown = m_inputManagementSystem.InputData.mouseInput.leftClickDown;

            if (leftClickDown || leftClickReleased)
            {
                Dependency = JobHandle.CombineDependencies(Dependency, m_raycastSystem.RaycastSystemDependency);
                NativeArray<float3> boxBounds = m_boxBounds;
                NativeArray<float3> boxBoundsSorted = new NativeArray<float3>(2, Allocator.TempJob);
                NativeArray<RaycastResult> selectionRaycastResult = m_raycastSystem.SelectionRaycastResult;
                NativeArray<RaycastResult> raycastResult = m_raycastSystem.RaycastResult;
                //Calculate box bounds
                Dependency = Job.WithReadOnly(selectionRaycastResult).WithCode(() =>
                {
                    if (leftClickPressed)
                    {
                        boxBounds[0] = selectionRaycastResult[0].hitPosition;
                    }
                    if (leftClickDown)
                    {
                        boxBounds[1] = selectionRaycastResult[0].hitPosition;
                    }

                    boxBoundsSorted[0] = math.min(boxBounds[0], boxBounds[1]);
                    boxBoundsSorted[1] = math.max(boxBounds[0], boxBounds[1]);
                }).Schedule(Dependency);

                EntityCommandBuffer.ParallelWriter ecbConcurrent = m_entityCommandBuffer.CreateCommandBuffer().AsParallelWriter();

                //Remove previous selections
                if (leftClickReleased)
                {
                    //Deselect things outside of the box
                    Dependency = Entities
                    .WithReadOnly(boxBoundsSorted)
                    .WithAll<SelectedTag>()
                    .ForEach((Entity entity, int entityInQueryIndex, in Translation translation) =>
                    {
                        if (((translation.Value.x < boxBoundsSorted[0].x) || (translation.Value.x > boxBoundsSorted[1].x) &&
                            (translation.Value.z < boxBoundsSorted[0].z) || (translation.Value.z > boxBoundsSorted[1].z)) &&
                            (raycastResult[0].raycastTargetEntity != entity))
                        {
                            ecbConcurrent.RemoveComponent<SelectedTag>(entityInQueryIndex, entity);
                        }
                    }).ScheduleParallel(Dependency);

                    //Select units inside the box if they aren't already selected
                    Dependency = Entities
                    .WithReadOnly(boxBoundsSorted)
                    .WithAll<UnitTag>()
                    .WithNone<SelectedTag>()
                    .WithReadOnly(raycastResult)
                    .ForEach((Entity entity, int entityInQueryIndex, in Translation translation) =>
                    {
                        if (((translation.Value.x > boxBoundsSorted[0].x) && (translation.Value.x < boxBoundsSorted[1].x) &&
                            (translation.Value.z > boxBoundsSorted[0].z) && (translation.Value.z < boxBoundsSorted[1].z)) ||
                            (raycastResult[0].raycastTargetEntity == entity))
                        {
                            ecbConcurrent.AddComponent(entityInQueryIndex, entity, new SelectedTag { });
                        }
                    }).ScheduleParallel(Dependency);

                    m_entityCommandBuffer.AddJobHandleForProducer(Dependency);

                    redrawSelectedUnits = true;
                }
                boxBoundsSorted.Dispose(Dependency);
            }
        }

        public override void FreeSystem()
        {
            m_boxBounds.Dispose();
        }
    }
}
