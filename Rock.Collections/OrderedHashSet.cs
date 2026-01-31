using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Rock.Collections.Internals;

namespace Rock.Collections;

[Serializable]
[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
[DebuggerDisplay("Count = {Count}")]
[JsonConverter(typeof(OrderedHashSetJsonConverter<>))]
public class OrderedHashSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable, ISerializable, IDeserializationCallback
{
	internal struct Slot
	{
		internal int hashCode;

		internal T value;

		internal int next;

		internal int nextOrder;

		internal int previousOrder;
	}

	public struct Reader : IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
	{
		private OrderedHashSet<T> m_set;

		public int Count => m_set.Count;

		public Reader(OrderedHashSet<T> set)
		{
			m_set = set;
		}

		public bool Contains(T item)
		{
			return m_set.Contains(item);
		}

		public Range StartWith(T item)
		{
			return new Range(m_set, item);
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(m_set);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public struct ReverseReader : IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
	{
		private OrderedHashSet<T> m_set;

		public int Count => m_set.Count;

		public ReverseReader(OrderedHashSet<T> set)
		{
			m_set = set;
		}

		public bool Contains(T item)
		{
			return m_set.Contains(item);
		}

		public ReverseRange StartWith(T item)
		{
			return new ReverseRange(m_set, item);
		}

		public ReverseEnumerator GetEnumerator()
		{
			return new ReverseEnumerator(m_set);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public struct Range : IEnumerable<T>, IEnumerable
	{
		private OrderedHashSet<T> m_set;

		private T m_startingItem;

		public Range(OrderedHashSet<T> set, T startingItem)
		{
			m_set = set;
			m_startingItem = startingItem;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(m_set, m_set.InternalIndexOf(m_startingItem));
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public struct ReverseRange : IEnumerable<T>, IEnumerable
	{
		private OrderedHashSet<T> m_set;

		private T m_startingItem;

		public ReverseRange(OrderedHashSet<T> set, T startingItem)
		{
			m_set = set;
			m_startingItem = startingItem;
		}

		public ReverseEnumerator GetEnumerator()
		{
			return new ReverseEnumerator(m_set, m_set.InternalIndexOf(m_startingItem));
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	[Serializable]
	public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
	{
		private OrderedHashSet<T> m_set;

		private int m_index;

		private int m_version;

		private T m_current;

		public T Current => m_current;

		object IEnumerator.Current
		{
			get
			{
				if (m_index == m_set.m_firstOrderIndex || m_index == -1)
				{
					throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
				}
				return Current;
			}
		}

		internal Enumerator(OrderedHashSet<T> set)
			: this(set, set.m_firstOrderIndex)
		{
		}

		internal Enumerator(OrderedHashSet<T> set, int startIndex)
		{
			m_set = set;
			m_index = startIndex;
			m_version = set.m_version;
			m_current = default(T);
		}

		public bool MoveNext()
		{
			if (m_version != m_set.m_version)
			{
				throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
			}
			if (m_index != -1)
			{
				m_current = m_set.m_slots[m_index].value;
				m_index = m_set.m_slots[m_index].nextOrder;
				return true;
			}
			m_current = default(T);
			return false;
		}

		void IDisposable.Dispose()
		{
		}

		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	[Serializable]
	public struct ReverseEnumerator : IEnumerator<T>, IDisposable, IEnumerator
	{
		private OrderedHashSet<T> m_set;

		private int m_index;

		private int m_version;

		private T m_current;

		public T Current => m_current;

		object IEnumerator.Current
		{
			get
			{
				if (m_index == m_set.m_lastOrderIndex || m_index == -1)
				{
					throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
				}
				return Current;
			}
		}

		internal ReverseEnumerator(OrderedHashSet<T> set)
			: this(set, set.m_lastOrderIndex)
		{
		}

		internal ReverseEnumerator(OrderedHashSet<T> set, int startIndex)
		{
			m_set = set;
			m_index = startIndex;
			m_version = set.m_version;
			m_current = default(T);
		}

		public bool MoveNext()
		{
			if (m_version != m_set.m_version)
			{
				throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
			}
			if (m_index != -1)
			{
				m_current = m_set.m_slots[m_index].value;
				m_index = m_set.m_slots[m_index].previousOrder;
				return true;
			}
			m_current = default(T);
			return false;
		}

		void IDisposable.Dispose()
		{
		}

		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	private static bool IsValueType = typeof(T).IsValueType;

	private static bool IsNullable = typeof(T).IsValueType && typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>);

	private const int Lower31BitMask = int.MaxValue;

	private const int GrowthFactor = 2;

	private const int ShrinkThreshold = 3;

	private const string CapacityName = "Capacity";

	private const string ElementsName = "Elements";

	private const string ComparerName = "Comparer";

	private const string VersionName = "Version";

	private int[] m_buckets;

	private Slot[] m_slots;

	private int m_count;

	private int m_lastIndex;

	private int m_freeList;

	private IEqualityComparer<T> m_comparer;

	private int m_version;

	private int m_firstOrderIndex;

	private int m_lastOrderIndex;

	private SerializationInfo m_siInfo;

	public int Count => m_count;

	public Reader Items => new Reader(this);

	public ReverseReader Reversed => new ReverseReader(this);

	bool ICollection<T>.IsReadOnly => false;

	public IEqualityComparer<T> Comparer => m_comparer;

	public OrderedHashSet()
		: this((IEqualityComparer<T>)EqualityComparer<T>.Default)
	{
	}

	public OrderedHashSet(int capacity)
		: this(capacity, (IEqualityComparer<T>)EqualityComparer<T>.Default)
	{
	}

	public OrderedHashSet(IEqualityComparer<T> comparer)
		: this(0, comparer)
	{
	}

	public OrderedHashSet(int capacity, IEqualityComparer<T> comparer)
	{
		if (comparer == null)
		{
			comparer = EqualityComparer<T>.Default;
		}
		m_comparer = comparer;
		m_lastIndex = 0;
		m_count = 0;
		m_freeList = -1;
		m_version = 0;
		m_firstOrderIndex = -1;
		m_lastOrderIndex = -1;
		if (capacity > 0)
		{
			Initialize(capacity);
		}
	}

	public OrderedHashSet(IEnumerable<T> collection)
		: this(collection, (IEqualityComparer<T>)EqualityComparer<T>.Default)
	{
	}

	public OrderedHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
		: this(comparer)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		int capacity = 0;
		if (collection is ICollection<T> collection2)
		{
			capacity = collection2.Count;
		}
		Initialize(capacity);
		UnionWith(collection);
		if ((m_count == 0 && m_slots.Length > HashHelpers.GetPrime(0)) || (m_count > 0 && m_slots.Length / m_count > 3))
		{
			TrimExcess();
		}
	}

	protected OrderedHashSet(SerializationInfo info, StreamingContext context)
	{
		m_siInfo = info;
	}

	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		info.AddValue("Version", m_version);
		info.AddValue("Comparer", m_comparer, typeof(IEqualityComparer<T>));
		info.AddValue("Capacity", (m_buckets != null) ? m_buckets.Length : 0);
		if (m_buckets != null)
		{
			T[] array = new T[m_count];
			CopyTo(array);
			info.AddValue("Elements", array, typeof(T[]));
		}
	}

	public virtual void OnDeserialization(object sender)
	{
		if (m_siInfo == null)
		{
			return;
		}
		int @int = m_siInfo.GetInt32("Capacity");
		m_comparer = (IEqualityComparer<T>)m_siInfo.GetValue("Comparer", typeof(IEqualityComparer<T>));
		m_freeList = -1;
		m_firstOrderIndex = -1;
		m_lastOrderIndex = -1;
		if (@int != 0)
		{
			m_buckets = new int[@int];
			m_slots = new Slot[@int];
			T[] array = (T[])m_siInfo.GetValue("Elements", typeof(T[]));
			if (array == null)
			{
				throw new SerializationException("Serialization_MissingKeys");
			}
			for (int i = 0; i < array.Length; i++)
			{
				Add(array[i]);
			}
		}
		else
		{
			m_buckets = null;
		}
		m_version = m_siInfo.GetInt32("Version");
		m_siInfo = null;
	}

	void ICollection<T>.Add(T item)
	{
		Add(item);
	}

	public void Clear()
	{
		if (m_lastIndex > 0)
		{
			Array.Clear(m_slots, 0, m_lastIndex);
			Array.Clear(m_buckets, 0, m_buckets.Length);
			m_lastIndex = 0;
			m_count = 0;
			m_freeList = -1;
			m_firstOrderIndex = -1;
			m_lastOrderIndex = -1;
		}
		m_version++;
	}

	public bool Contains(T item)
	{
		if (m_buckets != null)
		{
			int num = InternalGetHashCode(item);
			for (int num2 = m_buckets[num % m_buckets.Length] - 1; num2 >= 0; num2 = m_slots[num2].next)
			{
				if (m_slots[num2].hashCode == num && m_comparer.Equals(m_slots[num2].value, item))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		CopyTo(array, arrayIndex, m_count);
	}

	public void UnionWith(IEnumerable<T> other)
	{
		if (other == null)
		{
			throw new ArgumentNullException("other");
		}
		foreach (T item in other)
		{
			Add(item);
		}
	}

	private int InternalIndexOf(T item)
	{
		int num = InternalGetHashCode(item);
		for (int num2 = m_buckets[num % m_buckets.Length] - 1; num2 >= 0; num2 = m_slots[num2].next)
		{
			if (m_slots[num2].hashCode == num && m_comparer.Equals(m_slots[num2].value, item))
			{
				return num2;
			}
		}
		return -1;
	}

	public bool Remove(T item)
	{
		if (m_buckets != null)
		{
			int num = InternalGetHashCode(item);
			int num2 = num % m_buckets.Length;
			int num3 = -1;
			for (int num4 = m_buckets[num2] - 1; num4 >= 0; num4 = m_slots[num4].next)
			{
				if (m_slots[num4].hashCode == num && m_comparer.Equals(m_slots[num4].value, item))
				{
					if (num3 < 0)
					{
						m_buckets[num2] = m_slots[num4].next + 1;
					}
					else
					{
						m_slots[num3].next = m_slots[num4].next;
					}
					if (m_firstOrderIndex == num4)
					{
						m_firstOrderIndex = m_slots[num4].nextOrder;
					}
					if (m_lastOrderIndex == num4)
					{
						m_lastOrderIndex = m_slots[num4].previousOrder;
					}
					int nextOrder = m_slots[num4].nextOrder;
					int previousOrder = m_slots[num4].previousOrder;
					if (nextOrder != -1)
					{
						m_slots[nextOrder].previousOrder = previousOrder;
					}
					if (previousOrder != -1)
					{
						m_slots[previousOrder].nextOrder = nextOrder;
					}
					m_slots[num4].hashCode = -1;
					m_slots[num4].value = default(T);
					m_slots[num4].next = m_freeList;
					m_slots[num4].previousOrder = -1;
					m_slots[num4].nextOrder = -1;
					m_count--;
					m_version++;
					if (m_count == 0)
					{
						m_lastIndex = 0;
						m_freeList = -1;
					}
					else
					{
						m_freeList = num4;
					}
					return true;
				}
				num3 = num4;
			}
		}
		return false;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this);
	}

	public void CopyTo(T[] array)
	{
		CopyTo(array, 0, m_count);
	}

	public void CopyTo(T[] array, int arrayIndex, int count)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (arrayIndex < 0)
		{
			throw new ArgumentOutOfRangeException("arrayIndex", "ArgumentOutOfRange_NeedNonNegNum");
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NeedNonNegNum");
		}
		if (arrayIndex > array.Length || count > array.Length - arrayIndex)
		{
			throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
		}
		int num = 0;
		int num2 = m_firstOrderIndex;
		while (num2 != -1 && num < count)
		{
			array[arrayIndex + num] = m_slots[num2].value;
			num++;
			num2 = m_slots[num2].nextOrder;
		}
	}

	public bool MoveFirst(T item)
	{
		int num = InternalIndexOf(item);
		if (num != -1)
		{
			int previousOrder = m_slots[num].previousOrder;
			if (previousOrder != -1)
			{
				int nextOrder = m_slots[num].nextOrder;
				if (nextOrder == -1)
				{
					m_lastOrderIndex = previousOrder;
				}
				else
				{
					m_slots[nextOrder].previousOrder = previousOrder;
				}
				m_slots[previousOrder].nextOrder = nextOrder;
				m_slots[num].previousOrder = -1;
				m_slots[num].nextOrder = m_firstOrderIndex;
				m_slots[m_firstOrderIndex].previousOrder = num;
				m_firstOrderIndex = num;
			}
			return true;
		}
		return false;
	}

	public bool MoveLast(T item)
	{
		int num = InternalIndexOf(item);
		if (num != -1)
		{
			int nextOrder = m_slots[num].nextOrder;
			if (nextOrder != -1)
			{
				int previousOrder = m_slots[num].previousOrder;
				if (previousOrder == -1)
				{
					m_firstOrderIndex = nextOrder;
				}
				else
				{
					m_slots[previousOrder].nextOrder = nextOrder;
				}
				m_slots[nextOrder].previousOrder = previousOrder;
				m_slots[num].nextOrder = -1;
				m_slots[num].previousOrder = m_lastOrderIndex;
				m_slots[m_lastOrderIndex].nextOrder = num;
				m_lastOrderIndex = num;
			}
			return true;
		}
		return false;
	}

	public bool MoveBefore(T itemToMove, T mark)
	{
		int num = InternalIndexOf(itemToMove);
		int num2 = InternalIndexOf(mark);
		if (num != -1 && num2 != -1 && num != num2)
		{
			int nextOrder = m_slots[num].nextOrder;
			int previousOrder = m_slots[num].previousOrder;
			if (previousOrder == -1)
			{
				m_firstOrderIndex = nextOrder;
			}
			else
			{
				m_slots[previousOrder].nextOrder = nextOrder;
			}
			if (nextOrder == -1)
			{
				m_lastOrderIndex = previousOrder;
			}
			else
			{
				m_slots[nextOrder].previousOrder = previousOrder;
			}
			int previousOrder2 = m_slots[num2].previousOrder;
			m_slots[num].nextOrder = num2;
			m_slots[num].previousOrder = previousOrder2;
			m_slots[num2].previousOrder = num;
			if (previousOrder2 == -1)
			{
				m_firstOrderIndex = num;
			}
			else
			{
				m_slots[previousOrder2].nextOrder = num;
			}
			return true;
		}
		return false;
	}

	public bool MoveAfter(T itemToMove, T mark)
	{
		int num = InternalIndexOf(itemToMove);
		int num2 = InternalIndexOf(mark);
		if (num != -1 && num2 != -1 && num != num2)
		{
			int nextOrder = m_slots[num].nextOrder;
			int previousOrder = m_slots[num].previousOrder;
			if (previousOrder == -1)
			{
				m_firstOrderIndex = nextOrder;
			}
			else
			{
				m_slots[previousOrder].nextOrder = nextOrder;
			}
			if (nextOrder == -1)
			{
				m_lastOrderIndex = previousOrder;
			}
			else
			{
				m_slots[nextOrder].previousOrder = previousOrder;
			}
			int nextOrder2 = m_slots[num2].nextOrder;
			m_slots[num].previousOrder = num2;
			m_slots[num].nextOrder = nextOrder2;
			m_slots[num2].nextOrder = num;
			if (nextOrder2 == -1)
			{
				m_lastOrderIndex = num;
			}
			else
			{
				m_slots[nextOrder2].previousOrder = num;
			}
			return true;
		}
		return false;
	}

	public Range StartWith(T item)
	{
		return new Range(this, item);
	}

	public ReverseRange StartWithReversed(T item)
	{
		return new ReverseRange(this, item);
	}

	public int RemoveWhere(Predicate<T> match)
	{
		if (match == null)
		{
			throw new ArgumentNullException("match");
		}
		int num = 0;
		for (int i = 0; i < m_lastIndex; i++)
		{
			if (m_slots[i].hashCode >= 0)
			{
				T value = m_slots[i].value;
				if (match(value) && Remove(value))
				{
					num++;
				}
			}
		}
		return num;
	}

	public void TrimExcess()
	{
		if (m_count == 0)
		{
			m_buckets = null;
			m_slots = null;
			m_version++;
			return;
		}
		int prime = HashHelpers.GetPrime(m_count);
		Slot[] array = new Slot[prime];
		int[] array2 = new int[prime];
		int num = 0;
		for (int i = 0; i < m_lastIndex; i++)
		{
			if (m_slots[i].hashCode >= 0)
			{
				array[num] = m_slots[i];
				int num2 = array[num].hashCode % prime;
				array[num].next = array2[num2] - 1;
				array2[num2] = num + 1;
				m_slots[i].next = num;
				num++;
			}
		}
		num = 0;
		for (int j = 0; j < m_lastIndex; j++)
		{
			if (m_slots[j].hashCode >= 0)
			{
				int nextOrder = m_slots[j].nextOrder;
				int previousOrder = m_slots[j].previousOrder;
				if (nextOrder != -1)
				{
					array[num].nextOrder = m_slots[nextOrder].next;
				}
				else
				{
					m_lastOrderIndex = num;
				}
				if (previousOrder != -1)
				{
					array[num].previousOrder = m_slots[previousOrder].next;
				}
				else
				{
					m_firstOrderIndex = num;
				}
				num++;
			}
		}
		m_lastIndex = num;
		m_slots = array;
		m_buckets = array2;
		m_freeList = -1;
	}

	private void Initialize(int capacity)
	{
		int prime = HashHelpers.GetPrime(capacity);
		m_buckets = new int[prime];
		m_slots = new Slot[prime];
	}

	private void IncreaseCapacity()
	{
		int num = m_count * 2;
		if (num < 0)
		{
			num = m_count;
		}
		int prime = HashHelpers.GetPrime(num);
		if (prime <= m_count)
		{
			throw new ArgumentException("Arg_HSCapacityOverflow");
		}
		Slot[] array = new Slot[prime];
		if (m_slots != null)
		{
			Array.Copy(m_slots, 0, array, 0, m_lastIndex);
		}
		int[] array2 = new int[prime];
		for (int i = 0; i < m_lastIndex; i++)
		{
			int num2 = array[i].hashCode % prime;
			array[i].next = array2[num2] - 1;
			array2[num2] = i + 1;
		}
		m_slots = array;
		m_buckets = array2;
	}

	public bool Add(T value)
	{
		if (m_buckets == null)
		{
			Initialize(0);
		}
		int num = InternalGetHashCode(value);
		int num2 = num % m_buckets.Length;
		for (int num3 = m_buckets[num % m_buckets.Length] - 1; num3 >= 0; num3 = m_slots[num3].next)
		{
			if (m_slots[num3].hashCode == num && m_comparer.Equals(m_slots[num3].value, value))
			{
				return false;
			}
		}
		int num4;
		if (m_freeList >= 0)
		{
			num4 = m_freeList;
			m_freeList = m_slots[num4].next;
		}
		else
		{
			if (m_lastIndex == m_slots.Length)
			{
				IncreaseCapacity();
				num2 = num % m_buckets.Length;
			}
			num4 = m_lastIndex;
			m_lastIndex++;
		}
		m_slots[num4].hashCode = num;
		m_slots[num4].value = value;
		m_slots[num4].next = m_buckets[num2] - 1;
		if (m_lastOrderIndex != -1)
		{
			m_slots[m_lastOrderIndex].nextOrder = num4;
		}
		if (m_firstOrderIndex == -1)
		{
			m_firstOrderIndex = num4;
		}
		m_slots[num4].nextOrder = -1;
		m_slots[num4].previousOrder = m_lastOrderIndex;
		m_lastOrderIndex = num4;
		m_buckets[num2] = num4 + 1;
		m_count++;
		m_version++;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int InternalGetHashCode(T item)
	{
		bool num;
		if (!IsValueType)
		{
			num = item == null;
		}
		else
		{
			if (!IsNullable)
			{
				goto IL_002e;
			}
			num = item.Equals(null);
		}
		if (num)
		{
			return 0;
		}
		goto IL_002e;
		IL_002e:
		return m_comparer.GetHashCode(item) & 0x7FFFFFFF;
	}
}
