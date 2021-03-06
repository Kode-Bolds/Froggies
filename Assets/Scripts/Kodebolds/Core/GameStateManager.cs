using System;

namespace Kodebolds.Core
{
	[Flags]
	public enum GameState
	{
		Initialising = 1 << 0,
		Menu = 1 << 1,
		Loading = 1 << 2,
		Updating = 1 << 3,
		Paused = 1 << 4,
		Always = Initialising | Menu | Loading | Updating | Paused 
	}

	public class GameStateManager : IDependency
	{
		private GameState _gameState;
		public GameState GameState => _gameState;

		public GameStateManager()
		{
			_gameState = GameState.Initialising;
		}

		public void FinishInitialisation()
		{
			//TODO: Go into the menu state after initialisation, rather than straight into update.
			_gameState = GameState.Updating;
		}

		public void StartLoading()
		{
			_gameState = GameState.Loading;
		}

		public void FinishLoading()
		{
			_gameState = GameState.Updating;
		}

		public void PauseUpdate()
		{
			_gameState = GameState.Paused;
		}
	}
}