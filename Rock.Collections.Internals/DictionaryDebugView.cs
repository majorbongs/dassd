using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rock.Collections.Internals;

public sealed class DictionaryDebugView<K, V>
{
	private readonly IDictionary<K, V> m_dict;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public KeyValuePair<K, V>[] Items
	{
		get
		{
			KeyValuePair<K, V>[] array = new KeyValuePair<K, V>[m_dict.Count];
			m_dict.CopyTo(array, 0);
			return array;
		}
	}

	public DictionaryDebugView(IDictionary<K, V> dictionary)
	{
		if (dictionary == null)
		{
			throw new ArgumentNullException("dictionary");
		}
		m_dict = dictionary;
	}
}
