using System;
using Unity.Entities;
using UnityEngine;

namespace Kodebolds.Core
{
	public abstract class KodeboldJobSystem : SystemBase, IDependency, IDependant
	{
		protected GameStateManager _gameStateManager;
		protected abstract GameState ActiveGameState { get; }

		public void GetDependencies(Dependencies dependencies)
		{
			_gameStateManager = dependencies.GetDependency<GameStateManager>();
			GetSystemDependencies(dependencies);
		}

		public abstract void GetSystemDependencies(Dependencies dependencies);

		public void Init()
		{
			InitSystem();
		}

		public abstract void InitSystem();

		protected override void OnUpdate()
		{
			//Don't run update logic until we have entered the updating game state. (eg. no longer initialising the game data)
			if (Application.isPlaying && (_gameStateManager.GameState & ActiveGameState) != 0)
				UpdateSystem();
		}

		public abstract void UpdateSystem();

		public void Free()
		{
			FreeSystem();
		}

		public abstract void FreeSystem();
	}
}