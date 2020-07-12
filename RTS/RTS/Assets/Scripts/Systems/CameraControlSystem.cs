using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class CameraControlSystem : KodeboldJobSystem
{
    InputManagementSystem m_inputManagementSystem;

    public override void GetSystemDependencies(Dependencies dependencies)
    {
        m_inputManagementSystem = dependencies.GetDependency<InputManagementSystem>();
    }

    public override void InitSystem()
    {
        
    }

    public override void UpdateSystem()
    {
        float2 cameraControls = m_inputManagementSystem.InputData.inputActions.cameraMovement;
        float2 mouseScroll = m_inputManagementSystem.InputData.mouseInput.mouseScroll;
        float2 mousePos = m_inputManagementSystem.InputData.mouseInput.mouseScreenPos;
        float2 screenBounds = new float2(Screen.width, Screen.height);
        float2 mouseDelta = m_inputManagementSystem.InputData.mouseInput.mouseDelta;

        float dt = Time.DeltaTime;  
        bool focused = Application.isFocused;
        bool middleMouse = m_inputManagementSystem.InputData.mouseInput.middleClickDown;

        Entities
        .WithAll<Camera>()
        .ForEach((ref Translation translation, in CameraMovement cameraMovement, in LocalToWorld localToWorld) => 
        {

            if (mouseScroll.y != 0)
            {
                float scrollDirection = -math.clamp(mouseScroll.y, -1, 1);
                translation.Value += new float3(0, scrollDirection * cameraMovement.zoomSpeed, 0) * dt;                
            }

            //If using keys ignore mouse + edge pan
            if(cameraControls.x != 0 || cameraControls.y != 0)
			{
                translation.Value += new float3(cameraControls.x * cameraMovement.keyMoveSpeed, 0, cameraControls.y * cameraMovement.keyMoveSpeed) * dt;

            }
            // if using middle mouse ignore edge pan
            //TODO: Find how to calculate proper z movement from y mouse delta
            else if (middleMouse)
            {
                translation.Value -= new float3(mouseDelta.x, 0, mouseDelta.y) * cameraMovement.middleMouseMoveSpeed * dt;

            }
            //Only edge pan if mouse is still on this window (or its boundaries)
            //TODO: Find way to lock cursor to improve edge panning
            else if (focused)
            {
                float3 edgePan = new float3(0,0,0);
                //left
                if (mousePos.x <= cameraMovement.edgePanMargin)
                {
                    edgePan += cameraMovement.edgePanMoveSpeed * -localToWorld.Right; 
                }
                //down
                if (mousePos.y <= cameraMovement.edgePanMargin)
                {
                    edgePan += cameraMovement.edgePanMoveSpeed * new float3(0,0,-1); 
                }
                //right
                if (mousePos.x >= screenBounds.x - cameraMovement.edgePanMargin)
                {
                    edgePan += cameraMovement.edgePanMoveSpeed * localToWorld.Right; 
                }
                //up
                if (mousePos.y >= screenBounds.y - cameraMovement.edgePanMargin)
                {
                    edgePan += cameraMovement.edgePanMoveSpeed * new float3(0,0,1); 
                }
                edgePan *= dt;

                translation.Value += edgePan;
            }

            float xBounds = cameraMovement.cameraLimits.x / 2;
            if (translation.Value.x > xBounds)
            {
                translation.Value.x = xBounds;
            }
            if (translation.Value.x < -xBounds)
            {
                translation.Value.x = -xBounds;
            }
            float zBound = cameraMovement.cameraLimits.y / 2;
            if (translation.Value.z > zBound)
            {
                translation.Value.z = zBound;
            }
            if (translation.Value.z < -zBound)
            {
                translation.Value.z = -zBound;
            }

        }).ScheduleParallel();
    }

    public override void FreeSystem()
    {
        
    }
}
