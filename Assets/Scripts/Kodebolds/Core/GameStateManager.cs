namespace Kodebolds.Core
{
	public enum GameState
	{
		Initalising,
		Updating
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
			_gameState = GameState.Updating;
		}

		public void Free()
		{

		}
	}
}