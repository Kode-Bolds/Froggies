using Kodebolds.Core;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggies
{
	public struct InputData
	{
		public InputActions inputActions;
		public MouseInput mouseInput;
		public KeyboardInput keyboardInput;
	}

	public struct InputActions
	{
		public float2 cameraMovement;
		public bool spawn;
	}

	public struct MouseInput
	{
		public float2 mouseScreenPos;
		public float2 mouseDelta;
		public float3 mouseWorldPos;
		public float2 mouseScroll;

		public bool leftClickPressed;
		public bool leftClickDown;
		public bool leftClickReleased;

		public bool rightClickPressed;
		public bool rightClickDown;
		public bool rightClickReleased;

		public bool middleClickPressed;
		public bool middleClickDown;
		public bool middleClickReleased;
	}

	public struct KeyboardInput
	{
		public bool shiftDown;
	}

	public class InputManagementSystem : KodeboldJobSystem
	{
		private InputData m_inputData;
		public InputData InputData => m_inputData;

		private ControlScheme m_controlScheme;

		public override void GetSystemDependencies(Dependencies dependencies)
		{

		}

		public override void InitSystem()
		{
			m_controlScheme = new ControlScheme();
			m_controlScheme.Default.Enable();
		}

		public override void UpdateSystem()
		{
			m_inputData = new InputData();

			m_inputData.inputActions.cameraMovement = m_controlScheme.Default.CameraMovement.ReadValue<Vector2>();
			m_inputData.inputActions.spawn = m_controlScheme.Default.Spawn.triggered;

			m_inputData.mouseInput.mouseDelta = Mouse.current.delta.ReadValue();
			m_inputData.mouseInput.mouseScreenPos = GetMouseScreenPos();
			m_inputData.mouseInput.mouseWorldPos = GetMouseWorldPos();
			m_inputData.mouseInput.mouseScroll = Mouse.current.scroll.ReadValue();

			m_inputData.mouseInput.leftClickPressed = Mouse.current.leftButton.wasPressedThisFrame;
			m_inputData.mouseInput.leftClickDown = Mouse.current.leftButton.isPressed;
			m_inputData.mouseInput.leftClickReleased = Mouse.current.leftButton.wasReleasedThisFrame;

			m_inputData.mouseInput.rightClickPressed = Mouse.current.rightButton.wasPressedThisFrame;
			m_inputData.mouseInput.rightClickDown = Mouse.current.rightButton.isPressed;
			m_inputData.mouseInput.rightClickReleased = Mouse.current.rightButton.wasReleasedThisFrame;

			m_inputData.mouseInput.middleClickPressed = Mouse.current.middleButton.wasPressedThisFrame;
			m_inputData.mouseInput.middleClickDown = Mouse.current.middleButton.isPressed;
			m_inputData.mouseInput.middleClickReleased = Mouse.current.middleButton.wasReleasedThisFrame;

			m_inputData.keyboardInput.shiftDown = Keyboard.current.shiftKey.isPressed;
		}

		private float2 GetMouseScreenPos()
		{
			return Mouse.current.position.ReadValue();
		}

		private float3 GetMouseWorldPos()
		{
			float2 mousePos = GetMouseScreenPos();

			return Camera.main.ScreenToWorldPoint(new float3(mousePos.x, mousePos.y, Camera.main.nearClipPlane));
		}

		[BurstCompile]
		public static bool CastRayFromMouse(float3 cameraPos, float3 mouseWorldPos, float distance, out Unity.Physics.RaycastHit closestHit, CollisionFilter collisionFilter, CollisionWorld collisionWorld)
		{
			float3 origin = cameraPos;
			float3 direction = math.normalize(mouseWorldPos - origin);

			RaycastInput rayInput = new RaycastInput()
			{
				Start = origin,
				End = origin + (direction * distance),
				Filter = collisionFilter
			};

			return collisionWorld.CastRay(rayInput, out closestHit);
		}

		public override void FreeSystem()
		{

		}
	}
}