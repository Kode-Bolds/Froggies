using System.Collections.Generic;
using Unity.Entities;

namespace Kodebolds.Core
{
	[DisableAutoCreation]
	public class InitialisationBehaviourUpdaterSystem : KodeboldJobSystem
	{
		private List<KodeboldBehaviour> m_kodeboldBehaviours;

		protected override GameState ActiveGameState => GameState.Updating;

		public void SetBehavioursList(List<KodeboldBehaviour> behaviours)
		{
			m_kodeboldBehaviours = behaviours;
		}

		public override void GetSystemDependencies(Dependencies dependencies)
		{

		}

		public override void InitSystem()
		{

		}

		public override void UpdateSystem()
		{
			int count = m_kodeboldBehaviours.Count;
			for (int behaviourIndex = 0; behaviourIndex < count; behaviourIndex++)
			{
				m_kodeboldBehaviours[behaviourIndex].OnUpdate();
			}
		}

		public override void FreeSystem()
		{

		}
	}
}