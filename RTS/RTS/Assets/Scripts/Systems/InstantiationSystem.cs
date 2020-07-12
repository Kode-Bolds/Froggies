using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

public class InstantiationSystem : KodeboldJobSystem
{
    private EntityQuery m_query;
    public override void GetSystemDependencies(Dependencies dependencies)
    {
    }

    public override void InitSystem()
    {

    }

    protected override void OnCreate()
    {
        m_query = GetEntityQuery(ComponentType.ReadOnly<OnStartPrefabData>());
        RequireForUpdate(m_query);
    }

    public override void UpdateSystem()
    {
        //OnStartPrefabData prefabData = m_query.GetSingleton<OnStartPrefabData>();

        //EntityManager.Instantiate(prefabData.controlPoints);

        ////Delete on start prefab data to stop this system from running after prefabs have been instantiated
        //Entity entity = m_query.GetSingletonEntity();
        //EntityManager.DestroyEntity(entity);
    }

    public override void FreeSystem()
    {

    }
}
