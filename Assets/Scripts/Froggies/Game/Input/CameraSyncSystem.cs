using Kodebolds.Core;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Froggies
{
	public class CameraSyncSystem : KodeboldJobSystem
	{
		protected override GameState ActiveGameState => GameState.Updating | GameState.Paused;

		public override void GetSystemDependencies(Dependencies dependencies)
		{
			GameDataContainer gameData = dependencies.GetDependency<GameDataContainer>();
		}

		public override void InitSystem()
		{

		}

		public override void UpdateSystem()
		{
			Entities.WithoutBurst().WithAll<Camera>().ForEach((Transform transform, ref Translation translation, in CameraMovement cameraMovement) =>
			{
				transform.position = translation.Value;
			}).Run();
		}

		public override void FreeSystem()
		{

		}
	}
}