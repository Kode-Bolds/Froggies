using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Kodebolds.Core
{
	[StructLayout(LayoutKind.Sequential)]
	[NativeContainer]
	[NativeContainerSupportsDeallocateOnJobCompletion]
	[DebuggerDisplay("Length = {Length}, Length0 = {Length0}, Length1 = {Length1}")]
	[DebuggerTypeProxy(typeof(NativeArray2DDebugView<>))]
	public unsafe struct NativeArray2D<T> : IDisposable, IEnumerable<T>, IEquatable<NativeArray2D<T>> where T : unmanaged
	{
		[NativeDisableUnsafePtrRestriction]
		private void* m_Buffer;

		private int m_Length;
		public int Length => m_Length;

		private int m_Length0;
		public int Length0 => m_Length0;

		private int m_Length1;
		public int Length1 => m_Length1;


#if ENABLE_UNITY_COLLECTIONS_CHECKS
		private AtomicSafetyHandle m_Safety;

		[NativeSetClassTypeToNullOnSchedule]
		private DisposeSentinel m_DisposeSentinel;
#endif

		private Allocator m_AllocatorLabel;


		public NativeArray2D(int length0, int length1, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
		{
			Allocate(length0, length1, allocator, out this);

			if (options == NativeArrayOptions.ClearMemory)
				UnsafeUtility.MemClear(m_Buffer, m_Length * UnsafeUtility.SizeOf<T>());
		}

		public NativeArray2D(T[,] array, Allocator allocator)
		{
			int length0 = array.GetLength(0);
			int length1 = array.GetLength(1);
			Allocate(length0, length1, allocator, out this);
			Copy(array, this);
		}

		public NativeArray2D(NativeArray2D<T> array, Allocator allocator)
		{
			Allocate(array.m_Length0, array.m_Length1, allocator, out this);
			Copy(array, this);
		}

		private static void Allocate(int length0, int length1, Allocator allocator, out NativeArray2D<T> array)
		{
			RequireValidAllocator(allocator);

			if (!UnsafeUtility.IsUnmanaged<T>())
			{
				throw new InvalidOperationException("Only unmanaged types are supported.");
			}

			int length = length0 * length1;
			if (length <= 0)
			{
				throw new InvalidOperationException("Total number of elements must be greater than zero.");
			}

			array = new NativeArray2D<T>
			{
				m_Buffer = UnsafeUtility.Malloc(length * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator),
				m_Length0 = length0,
				m_Length1 = length1,
				m_Length = length1 * length0,
				m_AllocatorLabel = allocator
			};

#if ENABLE_UNITY_COLLECTIONS_CHECKS
			DisposeSentinel.Create(out array.m_Safety, out array.m_DisposeSentinel, 1, allocator);
#endif
		}

		public T this[int index0, int index1]
		{
			get
			{
				RequireReadAccess();
				RequireIndexInBounds(index0, index1);

				return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index1 * m_Length0 + index0);
			}

			[WriteAccessRequired]
			set
			{
				RequireWriteAccess();
				RequireIndexInBounds(index0, index1);

				UnsafeUtility.WriteArrayElement(m_Buffer, index1 * m_Length0 + index0, value);
			}
		}

		public T* GetPointerToElement(int index0, int index1)
		{
			RequireReadAccess();
			RequireIndexInBounds(index0, index1);

			return (T*)m_Buffer + index1 * m_Length0 + index0;
		}

		public T* GetPointerToElement(int2 indices)
		{
			return GetPointerToElement(indices.x, indices.y);
		}

		[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
		private static void RequireValidAllocator(Allocator allocator)
		{
			if (!UnsafeUtility.IsValidAllocator(allocator))
			{
				throw new InvalidOperationException("The NativeArray2D cannot be Disposed because it was not allocated with a valid allocator.");
			}
		}

		[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
		private void RequireReadAccess()
		{
			AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
		}

		[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
		private void RequireWriteAccess()
		{
			AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
		}

		[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
		[BurstDiscard]
		private void RequireIndexInBounds(int index0, int index1)
		{
			if (!CheckXBounds(index0))
				throw new IndexOutOfRangeException("Index0 " + index0 + " out of bounds of range " + m_Length0);

			if (!CheckYBounds(index1))
				throw new IndexOutOfRangeException("Index1 " + index1 + " out of bounds of range " + m_Length1);
		}

		public bool CheckXBounds(int x)
		{
			return (x < m_Length0 && x >= 0);
		}

		public bool CheckYBounds(int y)
		{
			return (y < m_Length1 && y >= 0);
		}

		public AtomicSafetyHandle GetAtomicSafetyHandle()
		{
			return m_Safety;
		}

		public void* GetUnsafePtr()
		{
			AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
			return m_Buffer;
		}

		public void* GetUnsafePtrReadOnly()
		{
			AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
			return m_Buffer;
		}

		public bool IsCreated => (IntPtr)m_Buffer != IntPtr.Zero;

		[WriteAccessRequired]
		public void Dispose()
		{
			if (m_Buffer == null)
			{
				throw new ObjectDisposedException("The NativeArray2D is already disposed.");
			}

			RequireWriteAccess();
			RequireValidAllocator(m_AllocatorLabel);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
			DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

			UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
			m_Buffer = null;
			m_Length0 = 0;
			m_Length1 = 0;
		}

		public JobHandle Dispose(JobHandle inputDeps)
		{
			if (m_Buffer == null)
			{
				throw new ObjectDisposedException("The NativeArray2D is already disposed.");
			}

			RequireValidAllocator(m_AllocatorLabel);

			DisposeSentinel.Clear(ref m_DisposeSentinel);

			JobHandle jobHandle = new NativeArray2DDisposeJob
			{
				m_Buffer = m_Buffer,
				m_AllocatorLabel = m_AllocatorLabel,
				m_Safety = m_Safety
			}.Schedule(inputDeps);

			AtomicSafetyHandle.Release(m_Safety);

			m_Buffer = null;
			m_Length0 = 0;
			m_Length1 = 0;

			return jobHandle;
		}

		[BurstCompile]
		private struct NativeArray2DDisposeJob : IJob
		{
			[NativeDisableUnsafePtrRestriction]
			public void* m_Buffer;
			public Allocator m_AllocatorLabel;
			public AtomicSafetyHandle m_Safety;

			public void Execute()
			{
				UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
			}
		}

		private static void Copy(NativeArray2D<T> src, NativeArray2D<T> dest)
		{
			src.RequireReadAccess();
			dest.RequireWriteAccess();

			if (src.Length0 != dest.Length0 || src.Length1 != dest.Length1)
				throw new ArgumentException("NativeArray2Ds must have the same size.");

			UnsafeUtility.MemCpy(dest.GetUnsafePtr(), src.GetUnsafePtr(), src.Length0 * src.Length1 * UnsafeUtility.SizeOf<T>());
		}

		private static void Copy(T[,] src, NativeArray2D<T> dest)
		{
			dest.RequireWriteAccess();

			if (src.GetLength(0) != dest.Length0 || src.GetLength(1) != dest.Length1)
				throw new ArgumentException("Arrays must have the same size.");

			fixed (void* srcPtr = &src[0, 0])
			{
				UnsafeUtility.MemCpy(dest.GetUnsafePtr(), srcPtr, dest.Length * sizeof(T));
			}
		}

		private static void Copy(NativeArray2D<T> src, T[,] dest)
		{
			src.RequireReadAccess();

			if (src.Length0 != dest.GetLength(0) || src.Length1 != dest.GetLength(1))
				throw new ArgumentException("Arrays must have the same size.");

			fixed (void* destPtr = &dest[0, 0])
			{
				UnsafeUtility.MemCpy(destPtr, src.GetUnsafePtr(), dest.Length * sizeof(T));
			}
		}

		[WriteAccessRequired]
		public void CopyFrom(T[,] array)
		{
			Copy(array, this);
		}

		[WriteAccessRequired]
		public void CopyFrom(NativeArray2D<T> array)
		{
			Copy(array, this);
		}

		public void CopyTo(T[,] array)
		{
			Copy(this, array);
		}

		public void CopyTo(NativeArray2D<T> array)
		{
			Copy(this, array);
		}

		public T[,] ToArray()
		{
			T[,] array = new T[m_Length0, m_Length1];
			fixed (void* arrayPtr = &array[0, 0])
			{
				UnsafeUtility.MemCpy(arrayPtr, m_Buffer, m_Length * sizeof(T));
			}
			return array;
		}

		public bool Equals(NativeArray2D<T> other)
		{
			return m_Buffer == other.m_Buffer
				&& m_Length0 == other.m_Length0
				&& m_Length1 == other.m_Length1;
		}

		public override bool Equals(object other)
		{
			if (ReferenceEquals(null, other))
				return false;

			return other is NativeArray2D<T> && Equals((NativeArray2D<T>)other);
		}

		public override int GetHashCode()
		{
			int result = (int)m_Buffer;
			result = (result * 397) ^ m_Length0;
			result = (result * 397) ^ m_Length1;
			return result;
		}

		public static bool operator ==(NativeArray2D<T> a, NativeArray2D<T> b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(NativeArray2D<T> a, NativeArray2D<T> b)
		{
			return !a.Equals(b);
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(ref this);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return new Enumerator(ref this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public struct Enumerator : IEnumerator<T>
		{
			private NativeArray2D<T> m_Array;
			private int m_Index0;
			private int m_Index1;

			public Enumerator(ref NativeArray2D<T> array)
			{
				m_Array = array;
				m_Index0 = -1;
				m_Index1 = 0;
			}

			public T Current => m_Array[m_Index0, m_Index1];

			object IEnumerator.Current => Current;

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				m_Index0++;
				if (m_Index0 >= m_Array.Length0)
				{
					m_Index0 = 0;
					m_Index1++;
					return m_Index1 < m_Array.m_Length1;
				}

				return true;
			}

			public void Reset()
			{
				m_Index0 = -1;
				m_Index1 = 0;
			}
		}
	}

	internal class NativeArray2DDebugView<T> where T : unmanaged
	{
		private readonly NativeArray2D<T> m_array;

		public NativeArray2DDebugView(NativeArray2D<T> array)
		{
			m_array = array;
		}

		public T[,] Items => m_array.ToArray();
	}
}
