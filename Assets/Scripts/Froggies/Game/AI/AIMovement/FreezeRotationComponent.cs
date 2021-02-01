using System;
using Unity.Entities;

namespace Froggies
{
	[Serializable]
	public struct FreezeRotation : IComponentData
	{
		public bool x;
		public bool y;
		public bool z;
	}
}