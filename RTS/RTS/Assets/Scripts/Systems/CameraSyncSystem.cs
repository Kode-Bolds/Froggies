using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class CameraSyncSystem : KodeboldJobSystem
{
    public override void GetSystemDependencies(Dependencies dependencies)
    {
    }

    public override void InitSystem()
    {
        
    }

    public override void UpdateSystem()
    {
        Entities.WithoutBurst().WithAll<Camera>().ForEach((Transform transform, ref Translation translation, in CameraMovement cameraMovement) => 
        {
            transform.position = translation.Value;
        }).Run();
    }

    public override void FreeSystem()
    {
        
    }
}
