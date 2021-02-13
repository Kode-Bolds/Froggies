using System;

namespace Kodebolds.Core
{
	[Serializable]
	public struct Bytes16
	{
		private long bytes8;
		private long bytes16;
	}

	[Serializable]
	public struct Bytes32
	{
		private Bytes16 bytes16;
		private Bytes16 bytes32;
	}

	[Serializable]
	public struct Bytes64
	{
		private Bytes32 bytes32;
		private Bytes32 bytes64;
	}

	[Serializable]
	public struct Bytes128
	{
		private Bytes64 bytes64;
		private Bytes64 bytes128;
	}
}