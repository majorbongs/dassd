using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using Gtacnr.Model.Exceptions;
using Newtonsoft.Json;
using Rock.Collections.Internals;

namespace Rock.Collections;

[Serializable]
[DebuggerTypeProxy(typeof(DictionaryDebugView<, >))]
[DebuggerDisplay("Count = {Count}")]
[ComVisible(false)]
[JsonConverter(typeof(OrderedDictionaryJsonConverter<, >))]
public class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary, ICollection, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, ISerializable, IDeserializationCallback
{
	private struct Entry
	{
		public int hashCode;

		public int next;

		public TKey key;

		public TValue value;

		public int nextOrder;

		public int previousOrder;
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal struct EmptyHelper
	{
		public static OrderedDictionary<TKey, TValue> m_empty = new OrderedDictionary<TKey, TValue>();
	}

	public struct Reader : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>
	{
		private OrderedDictionary<TKey, TValue> dictionary;

		public static Reader Empty => new Reader(EmptyHelper.m_empty);

		public TValue this[TKey key] => dictionary[key];

		public int Count => dictionary.Count;

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => dictionary.keys;

		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => dictionary.values;

		public Reader(OrderedDictionary<TKey, TValue> dictionary)
		{
			this.dictionary = dictionary;
		}

		public bool ContainsKey(TKey key)
		{
			return dictionary.ContainsKey(key);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			return dictionary.TryGetValue(key, out value);
		}

		public Enumerator GetEnumerator()
		{
			return dictionary.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public struct ReverseReader : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>
	{
		private OrderedDictionary<TKey, TValue> dictionary;

		public static ReverseReader Empty => new ReverseReader(EmptyHelper.m_empty);

		public TValue this[TKey key] => dictionary[key];

		public int Count => dictionary.Count;

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.Select((KeyValuePair<TKey, TValue> s) => s.Key);

		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => this.Select((KeyValuePair<TKey, TValue> s) => s.Value);

		public ReverseReader(OrderedDictionary<TKey, TValue> dictionary)
		{
			this.dictionary = dictionary;
		}

		public bool ContainsKey(TKey key)
		{
			return dictionary.ContainsKey(key);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			return dictionary.TryGetValue(key, out value);
		}

		public ReverseEnumerator GetEnumerator()
		{
			return new ReverseEnumerator(dictionary, 2);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	[Serializable]
	public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable, IEnumerator, IDictionaryEnumerator
	{
		private OrderedDictionary<TKey, TValue> dictionary;

		private int version;

		private int index;

		private KeyValuePair<TKey, TValue> current;

		private int getEnumeratorRetType;

		internal const int DictEntry = 1;

		internal const int KeyValuePair = 2;

		public KeyValuePair<TKey, TValue> Current => current;

		object IEnumerator.Current
		{
			get
			{
				if (index == dictionary.m_firstOrderIndex || index == -1)
				{
					throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
				}
				if (getEnumeratorRetType == 1)
				{
					return new DictionaryEntry(current.Key, current.Value);
				}
				return current;
			}
		}

		DictionaryEntry IDictionaryEnumerator.Entry
		{
			get
			{
				if (index == dictionary.m_firstOrderIndex || index == -1)
				{
					throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
				}
				return new DictionaryEntry(current.Key, current.Value);
			}
		}

		object IDictionaryEnumerator.Key
		{
			get
			{
				if (index == dictionary.m_firstOrderIndex || index == -1)
				{
					throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
				}
				return current.Key;
			}
		}

		object IDictionaryEnumerator.Value
		{
			get
			{
				if (index == dictionary.m_firstOrderIndex || index == -1)
				{
					throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
				}
				return current.Value;
			}
		}

		internal Enumerator(OrderedDictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
		{
			this.dictionary = dictionary;
			version = dictionary.version;
			index = dictionary.m_firstOrderIndex;
			this.getEnumeratorRetType = getEnumeratorRetType;
			current = default(KeyValuePair<TKey, TValue>);
		}

		internal Enumerator(OrderedDictionary<TKey, TValue> dictionary, int getEnumeratorRetType, int startingIndex)
		{
			this.dictionary = dictionary;
			version = dictionary.version;
			index = startingIndex;
			this.getEnumeratorRetType = getEnumeratorRetType;
			current = default(KeyValuePair<TKey, TValue>);
		}

		public bool MoveNext()
		{
			if (version != dictionary.version)
			{
				throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
			}
			if (index != -1)
			{
				current = new KeyValuePair<TKey, TValue>(dictionary.entries[index].key, dictionary.entries[index].value);
				index = dictionary.entries[index].nextOrder;
				return true;
			}
			index = -1;
			current = default(KeyValuePair<TKey, TValue>);
			return false;
		}

		public void Dispose()
		{
		}

		void IEnumerator.Reset()
		{
			if (version != dictionary.version)
			{
				throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
			}
			index = dictionary.m_firstOrderIndex;
			current = default(KeyValuePair<TKey, TValue>);
		}
	}

	[Serializable]
	public struct ReverseEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable, IEnumerator, IDictionaryEnumerator
	{
		private OrderedDictionary<TKey, TValue> dictionary;

		private int version;

		private int index;

		private KeyValuePair<TKey, TValue> current;

		private int getEnumeratorRetType;

		internal const int DictEntry = 1;

		internal const int KeyValuePair = 2;

		public KeyValuePair<TKey, TValue> Current => current;

		object IEnumerator.Current
		{
			get
			{
				if (index == dictionary.m_lastOrderIndex || index == -1)
				{
					throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
				}
				if (getEnumeratorRetType == 1)
				{
					return new DictionaryEntry(current.Key, current.Value);
				}
				return current;
			}
		}

		DictionaryEntry IDictionaryEnumerator.Entry
		{
			get
			{
				if (index == dictionary.m_lastOrderIndex || index == -1)
				{
					throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
				}
				return new DictionaryEntry(current.Key, current.Value);
			}
		}

		object IDictionaryEnumerator.Key
		{
			get
			{
				if (index == dictionary.m_lastOrderIndex || index == -1)
				{
					throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
				}
				return current.Key;
			}
		}

		object IDictionaryEnumerator.Value
		{
			get
			{
				if (index == dictionary.m_lastOrderIndex || index == -1)
				{
					throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
				}
				return current.Value;
			}
		}

		internal ReverseEnumerator(OrderedDictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
		{
			this.dictionary = dictionary;
			version = dictionary.version;
			index = dictionary.m_lastOrderIndex;
			this.getEnumeratorRetType = getEnumeratorRetType;
			current = default(KeyValuePair<TKey, TValue>);
		}

		internal ReverseEnumerator(OrderedDictionary<TKey, TValue> dictionary, int getEnumeratorRetType, int startingIndex)
		{
			this.dictionary = dictionary;
			version = dictionary.version;
			index = startingIndex;
			this.getEnumeratorRetType = getEnumeratorRetType;
			current = default(KeyValuePair<TKey, TValue>);
		}

		public bool MoveNext()
		{
			if (version != dictionary.version)
			{
				throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
			}
			if (index != -1)
			{
				current = new KeyValuePair<TKey, TValue>(dictionary.entries[index].key, dictionary.entries[index].value);
				index = dictionary.entries[index].previousOrder;
				return true;
			}
			index = -1;
			current = default(KeyValuePair<TKey, TValue>);
			return false;
		}

		public void Dispose()
		{
		}

		void IEnumerator.Reset()
		{
			if (version != dictionary.version)
			{
				throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
			}
			index = dictionary.m_lastOrderIndex;
			current = default(KeyValuePair<TKey, TValue>);
		}
	}

	[Serializable]
	[DebuggerTypeProxy(typeof(KeyCollectionDebugView<, >))]
	[DebuggerDisplay("Count = {Count}")]
	[JsonConverter(typeof(KeyCollectionJsonConverter<, >))]
	public sealed class KeyCollection : ICollection<TKey>, IEnumerable<TKey>, IEnumerable, ICollection, IReadOnlyCollection<TKey>
	{
		[Serializable]
		public struct Enumerator : IEnumerator<TKey>, IDisposable, IEnumerator
		{
			private OrderedDictionary<TKey, TValue> dictionary;

			private int index;

			private int version;

			private TKey currentKey;

			public TKey Current => currentKey;

			object IEnumerator.Current
			{
				get
				{
					if (index == dictionary.m_firstOrderIndex || index == -1)
					{
						throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
					}
					return currentKey;
				}
			}

			internal Enumerator(OrderedDictionary<TKey, TValue> dictionary)
			{
				this.dictionary = dictionary;
				version = dictionary.version;
				index = dictionary.m_firstOrderIndex;
				currentKey = default(TKey);
			}

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				if (version != dictionary.version)
				{
					throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
				}
				if (index != -1)
				{
					currentKey = dictionary.entries[index].key;
					index = dictionary.entries[index].nextOrder;
					return true;
				}
				index = -1;
				currentKey = default(TKey);
				return false;
			}

			void IEnumerator.Reset()
			{
				if (version != dictionary.version)
				{
					throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
				}
				index = dictionary.m_firstOrderIndex;
				currentKey = default(TKey);
			}
		}

		private OrderedDictionary<TKey, TValue> dictionary;

		public int Count => dictionary.Count;

		bool ICollection<TKey>.IsReadOnly => true;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => ((ICollection)dictionary).SyncRoot;

		public KeyCollection(OrderedDictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}
			this.dictionary = dictionary;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(dictionary);
		}

		public void CopyTo(TKey[] array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (index < 0 || index > array.Length)
			{
				throw new CustomArgumentOutOfRangeException("index", index, "ArgumentOutOfRange_Index");
			}
			if (array.Length - index < dictionary.Count)
			{
				throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
			}
			int num = 0;
			Entry[] entries = dictionary.entries;
			for (int num2 = dictionary.m_firstOrderIndex; num2 != -1; num2 = entries[num2].nextOrder)
			{
				array[index + num] = entries[num2].key;
				num++;
			}
		}

		void ICollection<TKey>.Add(TKey item)
		{
			throw new NotSupportedException("NotSupported_KeyCollectionSet");
		}

		void ICollection<TKey>.Clear()
		{
			throw new NotSupportedException("NotSupported_KeyCollectionSet");
		}

		bool ICollection<TKey>.Contains(TKey item)
		{
			return dictionary.ContainsKey(item);
		}

		bool ICollection<TKey>.Remove(TKey item)
		{
			throw new NotSupportedException("NotSupported_KeyCollectionSet");
		}

		IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
		{
			return new Enumerator(dictionary);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(dictionary);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (array.Rank != 1)
			{
				throw new ArgumentException("Arg_RankMultiDimNotSupported", "array");
			}
			if (array.GetLowerBound(0) != 0)
			{
				throw new ArgumentException("Arg_NonZeroLowerBound", "array");
			}
			if (index < 0 || index > array.Length)
			{
				throw new CustomArgumentOutOfRangeException("index", index, "ArgumentOutOfRange_Index");
			}
			if (array.Length - index < dictionary.Count)
			{
				throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
			}
			if (array is TKey[] array2)
			{
				CopyTo(array2, index);
				return;
			}
			if (!(array is object[] array3))
			{
				throw new ArgumentException("Argument_InvalidArrayType", "array");
			}
			int num = 0;
			Entry[] entries = dictionary.entries;
			try
			{
				for (int num2 = dictionary.m_firstOrderIndex; num2 != -1; num2 = entries[num2].nextOrder)
				{
					array3[index + num] = entries[num2].key;
					num++;
				}
			}
			catch (ArrayTypeMismatchException)
			{
				throw new ArgumentException("Argument_InvalidArrayType", "array");
			}
		}
	}

	[Serializable]
	[DebuggerTypeProxy(typeof(ValueCollectionDebugView<, >))]
	[DebuggerDisplay("Count = {Count}")]
	[JsonConverter(typeof(ValueCollectionJsonConverter<, >))]
	public sealed class ValueCollection : ICollection<TValue>, IEnumerable<TValue>, IEnumerable, ICollection, IReadOnlyCollection<TValue>
	{
		[Serializable]
		public struct Enumerator : IEnumerator<TValue>, IDisposable, IEnumerator
		{
			private OrderedDictionary<TKey, TValue> dictionary;

			private int index;

			private int version;

			private TValue currentValue;

			public TValue Current => currentValue;

			object IEnumerator.Current
			{
				get
				{
					if (index == dictionary.m_firstOrderIndex || index == -1)
					{
						throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
					}
					return currentValue;
				}
			}

			internal Enumerator(OrderedDictionary<TKey, TValue> dictionary)
			{
				this.dictionary = dictionary;
				version = dictionary.version;
				index = dictionary.m_firstOrderIndex;
				currentValue = default(TValue);
			}

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				if (version != dictionary.version)
				{
					throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
				}
				if (index != -1)
				{
					currentValue = dictionary.entries[index].value;
					index = dictionary.entries[index].nextOrder;
					return true;
				}
				index = -1;
				currentValue = default(TValue);
				return false;
			}

			void IEnumerator.Reset()
			{
				if (version != dictionary.version)
				{
					throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
				}
				index = dictionary.m_firstOrderIndex;
				currentValue = default(TValue);
			}
		}

		private OrderedDictionary<TKey, TValue> dictionary;

		public int Count => dictionary.Count;

		bool ICollection<TValue>.IsReadOnly => true;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => ((ICollection)dictionary).SyncRoot;

		public ValueCollection(OrderedDictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}
			this.dictionary = dictionary;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(dictionary);
		}

		public void CopyTo(TValue[] array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (index < 0 || index > array.Length)
			{
				throw new CustomArgumentOutOfRangeException("index", index, "ArgumentOutOfRange_Index");
			}
			if (array.Length - index < dictionary.Count)
			{
				throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
			}
			int num = 0;
			Entry[] entries = dictionary.entries;
			for (int num2 = dictionary.m_firstOrderIndex; num2 != -1; num2 = entries[num2].nextOrder)
			{
				array[index + num] = entries[num2].value;
				num++;
			}
		}

		void ICollection<TValue>.Add(TValue item)
		{
			throw new NotSupportedException("NotSupported_ValueCollectionSet");
		}

		bool ICollection<TValue>.Remove(TValue item)
		{
			throw new NotSupportedException("NotSupported_ValueCollectionSet");
		}

		void ICollection<TValue>.Clear()
		{
			throw new NotSupportedException("NotSupported_ValueCollectionSet");
		}

		bool ICollection<TValue>.Contains(TValue item)
		{
			return dictionary.ContainsValue(item);
		}

		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
		{
			return new Enumerator(dictionary);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(dictionary);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (array.Rank != 1)
			{
				throw new ArgumentException("Arg_RankMultiDimNotSupported", "array");
			}
			if (array.GetLowerBound(0) != 0)
			{
				throw new ArgumentException("Arg_NonZeroLowerBound", "array");
			}
			if (index < 0 || index > array.Length)
			{
				throw new CustomArgumentOutOfRangeException("index", index, "ArgumentOutOfRange_Index");
			}
			if (array.Length - index < dictionary.Count)
			{
				throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
			}
			if (array is TValue[] array2)
			{
				CopyTo(array2, index);
				return;
			}
			if (!(array is object[] array3))
			{
				throw new ArgumentException("Argument_InvalidArrayType", "array");
			}
			int num = 0;
			Entry[] entries = dictionary.entries;
			try
			{
				for (int num2 = dictionary.m_firstOrderIndex; num2 != -1; num2 = entries[num2].nextOrder)
				{
					array3[index + num] = entries[num2].value;
					num++;
				}
			}
			catch (ArrayTypeMismatchException)
			{
				throw new ArgumentException("Argument_InvalidArrayType", "array");
			}
		}
	}

	private int[] buckets;

	private Entry[] entries;

	private int count;

	private int version;

	private int m_firstOrderIndex;

	private int m_lastOrderIndex;

	private int freeList;

	private int freeCount;

	private IEqualityComparer<TKey> comparer;

	private KeyCollection keys;

	private ValueCollection values;

	private object _syncRoot;

	private const string VersionName = "Version";

	private const string HashSizeName = "HashSize";

	private const string KeyValuePairsName = "KeyValuePairs";

	private const string ComparerName = "Comparer";

	public Reader Items => new Reader(this);

	public ReverseReader Reversed => new ReverseReader(this);

	public IEqualityComparer<TKey> Comparer => comparer;

	public int Count => count - freeCount;

	public KeyCollection Keys
	{
		get
		{
			if (keys == null)
			{
				keys = new KeyCollection(this);
			}
			return keys;
		}
	}

	ICollection<TKey> IDictionary<TKey, TValue>.Keys
	{
		get
		{
			if (keys == null)
			{
				keys = new KeyCollection(this);
			}
			return keys;
		}
	}

	IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
	{
		get
		{
			if (keys == null)
			{
				keys = new KeyCollection(this);
			}
			return keys;
		}
	}

	public ValueCollection Values
	{
		get
		{
			if (values == null)
			{
				values = new ValueCollection(this);
			}
			return values;
		}
	}

	ICollection<TValue> IDictionary<TKey, TValue>.Values
	{
		get
		{
			if (values == null)
			{
				values = new ValueCollection(this);
			}
			return values;
		}
	}

	IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
	{
		get
		{
			if (values == null)
			{
				values = new ValueCollection(this);
			}
			return values;
		}
	}

	public TValue this[TKey key]
	{
		get
		{
			int num = FindEntry(key);
			if (num >= 0)
			{
				return entries[num].value;
			}
			throw new KeyNotFoundException();
		}
		set
		{
			Insert(key, value, add: false);
		}
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot
	{
		get
		{
			if (_syncRoot == null)
			{
				Interlocked.CompareExchange<object>(ref _syncRoot, new object(), (object)null);
			}
			return _syncRoot;
		}
	}

	bool IDictionary.IsFixedSize => false;

	bool IDictionary.IsReadOnly => false;

	ICollection IDictionary.Keys => Keys;

	ICollection IDictionary.Values => Values;

	object IDictionary.this[object key]
	{
		get
		{
			if (IsCompatibleKey(key))
			{
				int num = FindEntry((TKey)key);
				if (num >= 0)
				{
					return entries[num].value;
				}
			}
			return null;
		}
		set
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			if (value == null && default(TValue) != null)
			{
				throw new ArgumentNullException("value");
			}
			try
			{
				TKey key2 = (TKey)key;
				try
				{
					this[key2] = (TValue)value;
				}
				catch (InvalidCastException)
				{
					throw new ArgumentException($"The value '{value}' is not of type '{typeof(TValue)}' and cannot be used in this generic collection.", "value");
				}
			}
			catch (InvalidCastException)
			{
				throw new ArgumentException($"The value '{key}' is not of type '{typeof(TKey)}' and cannot be used in this generic collection.", "key");
			}
		}
	}

	public OrderedDictionary()
		: this(0, (IEqualityComparer<TKey>)null)
	{
	}

	public OrderedDictionary(int capacity)
		: this(capacity, (IEqualityComparer<TKey>)null)
	{
	}

	public OrderedDictionary(IEqualityComparer<TKey> comparer)
		: this(0, comparer)
	{
	}

	public OrderedDictionary(int capacity, IEqualityComparer<TKey> comparer)
	{
		if (capacity < 0)
		{
			throw new CustomArgumentOutOfRangeException("capacity", capacity, "ArgumentOutOfRange_NeedNonNegNum");
		}
		if (capacity > 0)
		{
			Initialize(capacity);
		}
		else
		{
			m_firstOrderIndex = (m_lastOrderIndex = -1);
		}
		this.comparer = comparer ?? EqualityComparer<TKey>.Default;
	}

	public OrderedDictionary(IDictionary<TKey, TValue> dictionary)
		: this(dictionary, (IEqualityComparer<TKey>)null)
	{
	}

	public OrderedDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
		: this(dictionary?.Count ?? 0, comparer)
	{
		if (dictionary == null)
		{
			throw new ArgumentNullException("dictionary");
		}
		if (dictionary.GetType() == typeof(OrderedDictionary<TKey, TValue>))
		{
			foreach (KeyValuePair<TKey, TValue> item in (OrderedDictionary<TKey, TValue>)dictionary)
			{
				Add(item.Key, item.Value);
			}
			return;
		}
		foreach (KeyValuePair<TKey, TValue> item2 in dictionary)
		{
			Add(item2.Key, item2.Value);
		}
	}

	protected OrderedDictionary(SerializationInfo info, StreamingContext context)
	{
		HashHelpers.SerializationInfoTable.Add(this, info);
	}

	public void Add(TKey key, TValue value)
	{
		Insert(key, value, add: true);
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
	{
		Add(keyValuePair.Key, keyValuePair.Value);
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
	{
		int num = FindEntry(keyValuePair.Key);
		if (num >= 0 && EqualityComparer<TValue>.Default.Equals(entries[num].value, keyValuePair.Value))
		{
			return true;
		}
		return false;
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
	{
		int num = FindEntry(keyValuePair.Key);
		if (num >= 0 && EqualityComparer<TValue>.Default.Equals(entries[num].value, keyValuePair.Value))
		{
			Remove(keyValuePair.Key);
			return true;
		}
		return false;
	}

	public void Clear()
	{
		if (count > 0)
		{
			for (int i = 0; i < buckets.Length; i++)
			{
				buckets[i] = -1;
			}
			Array.Clear(entries, 0, count);
			freeList = -1;
			count = 0;
			freeCount = 0;
			m_firstOrderIndex = -1;
			m_lastOrderIndex = -1;
			version++;
		}
	}

	public bool ContainsKey(TKey key)
	{
		return FindEntry(key) >= 0;
	}

	public bool ContainsValue(TValue value)
	{
		if (value == null)
		{
			for (int i = 0; i < count; i++)
			{
				if (entries[i].hashCode >= 0 && entries[i].value == null)
				{
					return true;
				}
			}
		}
		else
		{
			EqualityComparer<TValue> equalityComparer = EqualityComparer<TValue>.Default;
			for (int j = 0; j < count; j++)
			{
				if (entries[j].hashCode >= 0 && equalityComparer.Equals(entries[j].value, value))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (index < 0 || index > array.Length)
		{
			throw new CustomArgumentOutOfRangeException("index", index, "ArgumentOutOfRange_Index");
		}
		if (array.Length - index < Count)
		{
			throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
		}
		int num = 0;
		Entry[] array2 = entries;
		for (int num2 = m_firstOrderIndex; num2 != -1; num2 = array2[num2].nextOrder)
		{
			array[index + num] = new KeyValuePair<TKey, TValue>(array2[num2].key, array2[num2].value);
			num++;
		}
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this, 2);
	}

	public Enumerator GetEnumerator(TKey startingElement)
	{
		int startingIndex = FindEntry(startingElement);
		return new Enumerator(this, 2, startingIndex);
	}

	public ReverseEnumerator GetReverseEnumerator()
	{
		return new ReverseEnumerator(this, 2);
	}

	public ReverseEnumerator GetReverseEnumerator(TKey startingElement)
	{
		int startingIndex = FindEntry(startingElement);
		return new ReverseEnumerator(this, 2, startingIndex);
	}

	IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
	{
		return new Enumerator(this, 2);
	}

	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		info.AddValue("Version", version);
		info.AddValue("Comparer", HashHelpers.GetEqualityComparerForSerialization(comparer), typeof(IEqualityComparer<TKey>));
		info.AddValue("HashSize", (buckets != null) ? buckets.Length : 0);
		if (buckets != null)
		{
			KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[Count];
			CopyTo(array, 0);
			info.AddValue("KeyValuePairs", array, typeof(KeyValuePair<TKey, TValue>[]));
		}
	}

	private int FindEntry(TKey key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (buckets != null)
		{
			int num = comparer.GetHashCode(key) & 0x7FFFFFFF;
			for (int num2 = buckets[num % buckets.Length]; num2 >= 0; num2 = entries[num2].next)
			{
				if (entries[num2].hashCode == num && comparer.Equals(entries[num2].key, key))
				{
					return num2;
				}
			}
		}
		return -1;
	}

	private void Initialize(int capacity)
	{
		int prime = HashHelpers.GetPrime(capacity);
		buckets = new int[prime];
		for (int i = 0; i < buckets.Length; i++)
		{
			buckets[i] = -1;
		}
		entries = new Entry[prime];
		freeList = -1;
		m_firstOrderIndex = -1;
		m_lastOrderIndex = -1;
	}

	private void Insert(TKey key, TValue value, bool add)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (buckets == null)
		{
			Initialize(0);
		}
		int num = comparer.GetHashCode(key) & 0x7FFFFFFF;
		int num2 = num % buckets.Length;
		for (int num3 = buckets[num2]; num3 >= 0; num3 = entries[num3].next)
		{
			if (entries[num3].hashCode == num && comparer.Equals(entries[num3].key, key))
			{
				if (add)
				{
					TKey val = key;
					throw new ArgumentException("Argument_AddingDuplicate" + val);
				}
				entries[num3].value = value;
				version++;
				return;
			}
		}
		int num4;
		if (freeCount > 0)
		{
			num4 = freeList;
			freeList = entries[num4].next;
			freeCount--;
		}
		else
		{
			if (count == entries.Length)
			{
				Resize();
				num2 = num % buckets.Length;
			}
			num4 = count;
			count++;
		}
		entries[num4].hashCode = num;
		entries[num4].next = buckets[num2];
		entries[num4].key = key;
		entries[num4].value = value;
		if (m_lastOrderIndex != -1)
		{
			entries[m_lastOrderIndex].nextOrder = num4;
		}
		if (m_firstOrderIndex == -1)
		{
			m_firstOrderIndex = num4;
		}
		entries[num4].nextOrder = -1;
		entries[num4].previousOrder = m_lastOrderIndex;
		m_lastOrderIndex = num4;
		buckets[num2] = num4;
		version++;
	}

	public virtual void OnDeserialization(object sender)
	{
		HashHelpers.SerializationInfoTable.TryGetValue(this, out SerializationInfo value);
		if (value == null)
		{
			return;
		}
		int @int = value.GetInt32("Version");
		int int2 = value.GetInt32("HashSize");
		comparer = (IEqualityComparer<TKey>)value.GetValue("Comparer", typeof(IEqualityComparer<TKey>));
		if (int2 != 0)
		{
			buckets = new int[int2];
			for (int i = 0; i < buckets.Length; i++)
			{
				buckets[i] = -1;
			}
			entries = new Entry[int2];
			freeList = -1;
			m_firstOrderIndex = -1;
			m_lastOrderIndex = -1;
			KeyValuePair<TKey, TValue>[] array = (KeyValuePair<TKey, TValue>[])value.GetValue("KeyValuePairs", typeof(KeyValuePair<TKey, TValue>[]));
			if (array == null)
			{
				throw new SerializationException("Serialization_MissingKeys");
			}
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j].Key == null)
				{
					throw new SerializationException("Serialization_NullKey");
				}
				Insert(array[j].Key, array[j].Value, add: true);
			}
		}
		else
		{
			buckets = null;
		}
		version = @int;
		HashHelpers.SerializationInfoTable.Remove(this);
	}

	private void Resize()
	{
		Resize(HashHelpers.ExpandPrime(count), forceNewHashCodes: false);
	}

	private void Resize(int newSize, bool forceNewHashCodes)
	{
		int[] array = new int[newSize];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = -1;
		}
		Entry[] array2 = new Entry[newSize];
		Array.Copy(entries, 0, array2, 0, count);
		if (forceNewHashCodes)
		{
			for (int j = 0; j < count; j++)
			{
				if (array2[j].hashCode != -1)
				{
					array2[j].hashCode = comparer.GetHashCode(array2[j].key) & 0x7FFFFFFF;
				}
			}
		}
		for (int k = 0; k < count; k++)
		{
			if (array2[k].hashCode >= 0)
			{
				int num = array2[k].hashCode % newSize;
				array2[k].next = array[num];
				array[num] = k;
			}
		}
		buckets = array;
		entries = array2;
	}

	public bool Remove(TKey key, out TValue value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (buckets != null)
		{
			int num = comparer.GetHashCode(key) & 0x7FFFFFFF;
			int num2 = num % buckets.Length;
			int num3 = -1;
			for (int num4 = buckets[num2]; num4 >= 0; num4 = entries[num4].next)
			{
				if (entries[num4].hashCode == num && comparer.Equals(entries[num4].key, key))
				{
					if (num3 < 0)
					{
						buckets[num2] = entries[num4].next;
					}
					else
					{
						entries[num3].next = entries[num4].next;
					}
					value = entries[num4].value;
					entries[num4].hashCode = -1;
					entries[num4].next = freeList;
					entries[num4].key = default(TKey);
					entries[num4].value = default(TValue);
					if (m_firstOrderIndex == num4)
					{
						m_firstOrderIndex = entries[num4].nextOrder;
					}
					if (m_lastOrderIndex == num4)
					{
						m_lastOrderIndex = entries[num4].previousOrder;
					}
					int nextOrder = entries[num4].nextOrder;
					int previousOrder = entries[num4].previousOrder;
					if (nextOrder != -1)
					{
						entries[nextOrder].previousOrder = previousOrder;
					}
					if (previousOrder != -1)
					{
						entries[previousOrder].nextOrder = nextOrder;
					}
					entries[num4].previousOrder = -1;
					entries[num4].nextOrder = -1;
					freeList = num4;
					freeCount++;
					version++;
					return true;
				}
				num3 = num4;
			}
		}
		value = default(TValue);
		return false;
	}

	public bool Remove(TKey key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (buckets != null)
		{
			int num = comparer.GetHashCode(key) & 0x7FFFFFFF;
			int num2 = num % buckets.Length;
			int num3 = -1;
			for (int num4 = buckets[num2]; num4 >= 0; num4 = entries[num4].next)
			{
				if (entries[num4].hashCode == num && comparer.Equals(entries[num4].key, key))
				{
					if (num3 < 0)
					{
						buckets[num2] = entries[num4].next;
					}
					else
					{
						entries[num3].next = entries[num4].next;
					}
					entries[num4].hashCode = -1;
					entries[num4].next = freeList;
					entries[num4].key = default(TKey);
					entries[num4].value = default(TValue);
					if (m_firstOrderIndex == num4)
					{
						m_firstOrderIndex = entries[num4].nextOrder;
					}
					if (m_lastOrderIndex == num4)
					{
						m_lastOrderIndex = entries[num4].previousOrder;
					}
					int nextOrder = entries[num4].nextOrder;
					int previousOrder = entries[num4].previousOrder;
					if (nextOrder != -1)
					{
						entries[nextOrder].previousOrder = previousOrder;
					}
					if (previousOrder != -1)
					{
						entries[previousOrder].nextOrder = nextOrder;
					}
					entries[num4].previousOrder = -1;
					entries[num4].nextOrder = -1;
					freeList = num4;
					freeCount++;
					version++;
					return true;
				}
				num3 = num4;
			}
		}
		return false;
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		int num = FindEntry(key);
		if (num >= 0)
		{
			value = entries[num].value;
			return true;
		}
		value = default(TValue);
		return false;
	}

	public bool MoveFirst(TKey key)
	{
		int num = FindEntry(key);
		if (num != -1)
		{
			int previousOrder = entries[num].previousOrder;
			if (previousOrder != -1)
			{
				int nextOrder = entries[num].nextOrder;
				if (nextOrder == -1)
				{
					m_lastOrderIndex = previousOrder;
				}
				else
				{
					entries[nextOrder].previousOrder = previousOrder;
				}
				entries[previousOrder].nextOrder = nextOrder;
				entries[num].previousOrder = -1;
				entries[num].nextOrder = m_firstOrderIndex;
				entries[m_firstOrderIndex].previousOrder = num;
				m_firstOrderIndex = num;
			}
			return true;
		}
		return false;
	}

	public bool MoveLast(TKey key)
	{
		int num = FindEntry(key);
		if (num != -1)
		{
			int nextOrder = entries[num].nextOrder;
			if (nextOrder != -1)
			{
				int previousOrder = entries[num].previousOrder;
				if (previousOrder == -1)
				{
					m_firstOrderIndex = nextOrder;
				}
				else
				{
					entries[previousOrder].nextOrder = nextOrder;
				}
				entries[nextOrder].previousOrder = previousOrder;
				entries[num].nextOrder = -1;
				entries[num].previousOrder = m_lastOrderIndex;
				entries[m_lastOrderIndex].nextOrder = num;
				m_lastOrderIndex = num;
			}
			return true;
		}
		return false;
	}

	public bool MoveBefore(TKey keyToMove, TKey mark)
	{
		int num = FindEntry(keyToMove);
		int num2 = FindEntry(mark);
		if (num != -1 && num2 != -1 && num != num2)
		{
			int nextOrder = entries[num].nextOrder;
			int previousOrder = entries[num].previousOrder;
			if (previousOrder == -1)
			{
				m_firstOrderIndex = nextOrder;
			}
			else
			{
				entries[previousOrder].nextOrder = nextOrder;
			}
			if (nextOrder == -1)
			{
				m_lastOrderIndex = previousOrder;
			}
			else
			{
				entries[nextOrder].previousOrder = previousOrder;
			}
			int previousOrder2 = entries[num2].previousOrder;
			entries[num].nextOrder = num2;
			entries[num].previousOrder = previousOrder2;
			entries[num2].previousOrder = num;
			if (previousOrder2 == -1)
			{
				m_firstOrderIndex = num;
			}
			else
			{
				entries[previousOrder2].nextOrder = num;
			}
			return true;
		}
		return false;
	}

	public bool MoveAfter(TKey keyToMove, TKey mark)
	{
		int num = FindEntry(keyToMove);
		int num2 = FindEntry(mark);
		if (num != -1 && num2 != -1 && num != num2)
		{
			int nextOrder = entries[num].nextOrder;
			int previousOrder = entries[num].previousOrder;
			if (previousOrder == -1)
			{
				m_firstOrderIndex = nextOrder;
			}
			else
			{
				entries[previousOrder].nextOrder = nextOrder;
			}
			if (nextOrder == -1)
			{
				m_lastOrderIndex = previousOrder;
			}
			else
			{
				entries[nextOrder].previousOrder = previousOrder;
			}
			int nextOrder2 = entries[num2].nextOrder;
			entries[num].previousOrder = num2;
			entries[num].nextOrder = nextOrder2;
			entries[num2].nextOrder = num;
			if (nextOrder2 == -1)
			{
				m_lastOrderIndex = num;
			}
			else
			{
				entries[nextOrder2].previousOrder = num;
			}
			return true;
		}
		return false;
	}

	internal TValue GetValueOrDefault(TKey key)
	{
		int num = FindEntry(key);
		if (num >= 0)
		{
			return entries[num].value;
		}
		return default(TValue);
	}

	void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
	{
		CopyTo(array, index);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (array.Rank != 1)
		{
			throw new ArgumentException("Arg_RankMultiDimNotSupported", "array");
		}
		if (array.GetLowerBound(0) != 0)
		{
			throw new ArgumentException("Arg_NonZeroLowerBound", "array");
		}
		if (index < 0 || index > array.Length)
		{
			throw new CustomArgumentOutOfRangeException("index", index, "ArgumentOutOfRange_Index");
		}
		if (array.Length - index < Count)
		{
			throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
		}
		if (array is KeyValuePair<TKey, TValue>[] array2)
		{
			CopyTo(array2, index);
			return;
		}
		if (array is DictionaryEntry[])
		{
			DictionaryEntry[] array3 = array as DictionaryEntry[];
			Entry[] array4 = entries;
			int num = 0;
			for (int num2 = m_firstOrderIndex; num2 != -1; num2 = array4[num2].nextOrder)
			{
				array3[index + num] = new DictionaryEntry(array4[num2].key, array4[num2].value);
				num++;
			}
			return;
		}
		if (!(array is object[] array5))
		{
			throw new ArgumentException("Argument_InvalidArrayType", "array");
		}
		try
		{
			Entry[] array6 = entries;
			int num3 = 0;
			for (int num4 = m_firstOrderIndex; num4 != -1; num4 = array6[num4].nextOrder)
			{
				array5[index + num3] = new DictionaryEntry(array6[num4].key, array6[num4].value);
				num3++;
			}
		}
		catch (ArrayTypeMismatchException)
		{
			throw new ArgumentException("Argument_InvalidArrayType", "array");
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this, 2);
	}

	private static bool IsCompatibleKey(object key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		return key is TKey;
	}

	void IDictionary.Add(object key, object value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (value == null && default(TValue) != null)
		{
			throw new ArgumentNullException("value");
		}
		try
		{
			TKey key2 = (TKey)key;
			try
			{
				Add(key2, (TValue)value);
			}
			catch (InvalidCastException)
			{
				throw new ArgumentException($"The value '{value}' is not of type '{typeof(TValue)}' and cannot be used in this generic collection.", "value");
			}
		}
		catch (InvalidCastException)
		{
			throw new ArgumentException($"The value '{key}' is not of type '{typeof(TKey)}' and cannot be used in this generic collection.", "key");
		}
	}

	bool IDictionary.Contains(object key)
	{
		if (IsCompatibleKey(key))
		{
			return ContainsKey((TKey)key);
		}
		return false;
	}

	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		return new Enumerator(this, 1);
	}

	void IDictionary.Remove(object key)
	{
		if (IsCompatibleKey(key))
		{
			Remove((TKey)key);
		}
	}
}
