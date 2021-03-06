using UnityEngine;

namespace Kodebolds.Core
{
	public abstract class KodeboldBehaviour : MonoBehaviour, IDependant, IDependency
	{
		protected GameStateManager _gameStateManager;
		protected abstract GameState ActiveGameState { get; }

		public void GetDependencies(Dependencies dependencies)
		{
			_gameStateManager = dependencies.GetDependency<GameStateManager>();
			GetBehaviourDependencies(dependencies);
		}

		public abstract void GetBehaviourDependencies(Dependencies dependencies);

		public void Init()
		{
			InitBehaviour();
		}

		public abstract void InitBehaviour();

		public void OnUpdate()
		{
			if (Application.isPlaying && (_gameStateManager.GameState & ActiveGameState) != 0)
				UpdateBehaviour();
		}

		public abstract void UpdateBehaviour();

		public void Free()
		{
			FreeBehaviour();
		}

		public abstract void FreeBehaviour();
	}
}