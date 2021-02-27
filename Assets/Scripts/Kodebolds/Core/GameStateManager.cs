using System;

namespace Kodebolds.Core
{
	[Flags]
	public enum GameState
	{
		None = 0,
		Initalising = 1 << 0,
		Updating = 1 << 1,
		Paused = 1 << 2,
		Menu = 1 << 3,
		Always = Initalising | Updating | Paused | Menu
	}

	public class GameStateManager : IDependency
	{
		private GameState _gameState;
		public GameState GameState => _gameState;

		public GameStateManager()
		{
			_gameState = GameState.Initalising;
		}

		public void FinishInitialisation()
		{
			//TODO: Go into the menu state after initialisation, rather than straight into update.
			_gameState = GameState.Updating;
		}

		public void StartUpdate()
		{
			_gameState = GameState.Updating;
		}

		public void PauseUpdate()
		{
			_gameState = GameState.Paused;
		}
	}
}