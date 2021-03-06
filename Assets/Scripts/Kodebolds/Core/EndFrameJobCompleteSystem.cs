using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Kodebolds.Core
{
	[AlwaysUpdateSystem]
	public class EndFrameJobCompleteSystem : KodeboldJobSystem
	{
		private NativeList<JobHandle> m_jobHandlesToComplete;

		protected override GameState ActiveGameState => GameState.Updating;

		public override void GetSystemDependencies(Dependencies dependencies)
		{

		}

		public override void InitSystem()
		{
			m_jobHandlesToComplete = new NativeList<JobHandle>(Allocator.Persistent);
		}

		public override void UpdateSystem()
		{
			for (int jobHandleIndex = 0; jobHandleIndex < m_jobHandlesToComplete.Length; jobHandleIndex++)
			{
				m_jobHandlesToComplete[jobHandleIndex].Complete();
			}

			m_jobHandlesToComplete.Clear();
		}

		public override void FreeSystem()
		{
			m_jobHandlesToComplete.Dispose();
		}

		public void AddJobHandleToComplete(JobHandle jobHandle)
		{
			m_jobHandlesToComplete.Add(jobHandle);
		}
	}
}