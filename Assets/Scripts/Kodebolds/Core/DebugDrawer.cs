using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Kodebolds.Core
{
	public enum DebugDrawCommandType
	{
		Line = 0,
		Sphere = 1,
	}

	public interface IDebugDrawCommandData { }

	[StructLayout(LayoutKind.Explicit)]
	public struct DebugDrawCommand
	{
		[FieldOffset(0)] public DebugDrawCommandType debugDrawCommandType;
		[FieldOffset(4)] public DebugDrawLineData debugDrawLineData;
		[FieldOffset(4)] public DebugDrawSphereData debugDrawSphereData;

		public T DebugCommandData<T>() where T : IDebugDrawCommandData
		{
			switch (debugDrawCommandType)
			{
				case DebugDrawCommandType.Line:
					return (T)(IDebugDrawCommandData)debugDrawLineData;
				case DebugDrawCommandType.Sphere:
					return (T)(IDebugDrawCommandData)debugDrawSphereData;
				default:
					throw new Exception("Invalid debug draw command type");
			}
		}
	}

	public struct DebugDrawLineData : IDebugDrawCommandData
	{
		public Color colour;
		public float3 start;
		public float3 end;
	}

	public struct DebugDrawSphereData : IDebugDrawCommandData
	{

	}

	public class DebugDrawer : KodeboldBehaviour
	{
		[SerializeField] private bool m_enabled;
		public NativeQueue<DebugDrawCommand> DebugDrawCommandQueue => m_debugDrawCommandQueue;
		public NativeQueue<DebugDrawCommand>.ParallelWriter DebugDrawCommandQueueParallel => m_debugDrawCommandQueueParallel;

		private NativeQueue<DebugDrawCommand>.ParallelWriter m_debugDrawCommandQueueParallel;
		private NativeQueue<DebugDrawCommand> m_debugDrawCommandQueue;

		public JobHandle debugDrawDependencies;

		public override void GetBehaviourDependencies(Dependencies dependencies)
		{

		}

		public override void InitBehaviour()
		{
			m_debugDrawCommandQueue = new NativeQueue<DebugDrawCommand>(Allocator.Persistent);
			m_debugDrawCommandQueueParallel = m_debugDrawCommandQueue.AsParallelWriter();
		}

		public override void UpdateBehaviour()
		{
#if UNITY_EDITOR
			if (!m_enabled)
				return;

			debugDrawDependencies.Complete();

			NativeArray<DebugDrawCommand> debugDrawCommands = m_debugDrawCommandQueue.ToArray(Allocator.Temp);
			m_debugDrawCommandQueue.Clear();

			for (int debugDrawCommandIndex = 0; debugDrawCommandIndex < debugDrawCommands.Length; debugDrawCommandIndex++)
			{
				DebugDrawCommand debugDrawCommand = debugDrawCommands[debugDrawCommandIndex];
				switch (debugDrawCommand.debugDrawCommandType)
				{
					case DebugDrawCommandType.Line:
						DebugDrawLineData debugDrawLineData = debugDrawCommand.DebugCommandData<DebugDrawLineData>();
						Debug.DrawLine(debugDrawLineData.start, debugDrawLineData.end, debugDrawLineData.colour, 0, false);
						break;
					case DebugDrawCommandType.Sphere:
						break;
					default:
						throw new Exception("Invalid debug draw command type");
				}
			}

			debugDrawCommands.Dispose();
#endif
		}

		public override void FreeBehaviour()
		{
			m_debugDrawCommandQueue.Dispose();
		}

	}
}