using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rock.Collections.Internals;

public sealed class ValueCollectionDebugView<TKey, TValue>
{
	private readonly ICollection<TValue> m_collection;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public TValue[] Items
	{
		get
		{
			TValue[] array = new TValue[m_collection.Count];
			m_collection.CopyTo(array, 0);
			return array;
		}
	}

	public ValueCollectionDebugView(ICollection<TValue> collection)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		m_collection = collection;
	}
}
