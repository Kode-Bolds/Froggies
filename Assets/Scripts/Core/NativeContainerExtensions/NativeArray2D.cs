using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

[DebuggerDisplay("Length = {Length}, Columns = {Columns}, Rows = {Rows}")]
[DebuggerTypeProxy(typeof(NativeArray2DDebugView<>))]
[NativeContainer]
[NativeContainerSupportsDeallocateOnJobCompletion]
public struct NativeArray2D<T> : IEnumerable<T>, IEnumerable where T : unmanaged
{
	private NativeArray<T> m_data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
	private AtomicSafetyHandle m_safety;
#endif

	public int Length => m_data.Length;

	private int m_columns;
	public int Columns => m_columns;

	private int m_rows;
	public int Rows => m_rows;

	/// <summary>
	/// Construct a new 2D Native Array with the given row and column count, and given allocator.
	/// </summary>
	/// <param name="columnCount"></param>
	/// <param name="rowCount"></param>
	/// <param name="allocator"></param>
	public NativeArray2D(int columnCount, int rowCount, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
	{
		m_data = new NativeArray<T>(columnCount * rowCount, allocator, options);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
		m_safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(m_data);
#endif

		m_columns = columnCount;
		m_rows = rowCount;
	}

	/// <summary>
	/// Construct a 2D Native Array from a given C# managed 2D array with the given allocator.
	/// </summary>
	/// <param name="array"></param>
	/// <param name="allocator"></param>
	public unsafe NativeArray2D(T[,] array, Allocator allocator)
	{
		m_data = new NativeArray<T>(array.GetLength(0) * array.GetLength(1), allocator);

		fixed(void* arrayPtr = &array[0, 0])
		{
			UnsafeUtility.MemCpy(m_data.GetUnsafePtr(), arrayPtr, m_data.Length * sizeof(T));
		}

#if ENABLE_UNITY_COLLECTIONS_CHECKS
		m_safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(m_data);
#endif

		m_columns = array.GetLength(0);
		m_rows = array.GetLength(1);
	}

	public T this[int columnIndex, int rowIndex]
	{
		get
		{
			if(columnIndex < m_columns)
				throw new Exception("Column index " + columnIndex + " out of bounds of range " + m_columns);

			if(rowIndex < m_rows)
				throw new Exception("Row index " + rowIndex + " out of bounds of range " + m_rows);

			return m_data[columnIndex + rowIndex * m_columns];
		}
		set
		{
			if (columnIndex < m_columns)
				throw new Exception("Column index " + columnIndex + " out of bounds of range " + m_columns);

			if (rowIndex < m_rows)
				throw new Exception("Row index " + rowIndex + " out of bounds of range " + m_rows);

			m_data[columnIndex + rowIndex * m_columns] = value;
		}
	}

	public bool Equals(NativeArray2D<T> other)
	{
		return m_data == other.m_data;
	}

	public static bool operator ==(NativeArray2D<T> a, NativeArray2D<T> b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(NativeArray2D<T> a, NativeArray2D<T> b)
	{
		return !a.Equals(b);
	}

	public override bool Equals(object other)
	{
		if(ReferenceEquals(null, other))
		{
			return false;
		}

		return other is NativeArray2D<T> && Equals((NativeArray2D<T>)other);
	}

	public override int GetHashCode()
	{
		return m_columns ^ m_rows;
	}

	/// <summary>
	/// Convert this 2D Native Array to a C# managed 2D array.
	/// </summary>
	/// <returns></returns>
	public unsafe T[,] ToArray()
	{
		T[,] array = new T[m_columns, m_rows];
		fixed (void* arrayPtr = &array[0, 0])
		{
			UnsafeUtility.MemCpy(arrayPtr, m_data.GetUnsafePtr(), m_data.Length * sizeof(T));
		}
		return array;
	}

	public unsafe void CopyTo(NativeArray2D<T> dest)
	{
		if(Length != dest.Length)
			throw new Exception("Containers have differing sizes!");

		UnsafeUtility.MemCpy(dest.m_data.GetUnsafePtr(), m_data.GetUnsafePtr(), m_data.Length * sizeof(T));
	}

	public void Dispose()
	{
		m_data.Dispose();
	}

	public JobHandle Dispose(JobHandle dependencies)
	{
		return m_data.Dispose(dependencies);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return new NativeArray<T>.Enumerator(ref m_data);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}

internal class NativeArray2DDebugView<T> where T : unmanaged
{
	private readonly NativeArray2D<T> m_array;

	public NativeArray2DDebugView(NativeArray2D<T> array)
	{
		m_array = array;
	}

	public T[,] Items
	{
		get
		{
			return m_array.ToArray();
		}
	}
}