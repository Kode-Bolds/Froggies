using Kodebolds.Core;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;

namespace Froggies
{
	public enum SpawnCommandType
	{
		Harvester = 0,
		Projectile = 1
	}

	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct SpawnCommand
	{
		[FieldOffset(0)] public SpawnCommandType spawnCommandType;
		[FieldOffset(4)] public Entity entity;
		[FieldOffset(12)] private SpawnCommandData spawnCommandData;

		public T* CommandData<T>() where T : unmanaged, ISpawnCommandData
		{
			return (T*)UnsafeUtility.AddressOf(ref spawnCommandData);
		}
	}

	public struct SpawnCommandData : ISpawnCommandData
	{
		private Bytes128 bytes128;
	}

	public interface ISpawnCommandData { }

	public struct HarvesterSpawnData : ISpawnCommandData
	{
		public Translation translation;
		public LocalToWorld localToWorld;
		public PathFinding pathFinding;
	}

	public struct ProjectileSpawnData : ISpawnCommandData
	{
		public Translation translation;
		public Projectile projectile;
	}

	public static unsafe class SpawnCommands
	{
		public static void SpawnHarvester(NativeQueue<SpawnCommand> spawnQueue, Entity entity, Translation translation, LocalToWorld localToWorld, PathFinding pathFinding)
		{
			SpawnCommand spawnCommand = new SpawnCommand();
			spawnCommand.spawnCommandType = SpawnCommandType.Harvester;
			spawnCommand.entity = entity;

			HarvesterSpawnData* spawnData = spawnCommand.CommandData<HarvesterSpawnData>();
			spawnData->translation = translation;
			spawnData->localToWorld = localToWorld;
			spawnData->pathFinding = pathFinding;

			spawnQueue.Enqueue(spawnCommand);
		}

		public static void SpawnProjectile(NativeQueue<SpawnCommand> spawnQueue, Entity entity, Translation translation, Projectile projectile)
		{
			SpawnCommand spawnCommand = new SpawnCommand();
			spawnCommand.spawnCommandType = SpawnCommandType.Projectile;
			spawnCommand.entity = entity;

			ProjectileSpawnData* spawnData = spawnCommand.CommandData<ProjectileSpawnData>();
			spawnData->translation = translation;
			spawnData->projectile = projectile;

			spawnQueue.Enqueue(spawnCommand);
		}
	}
}