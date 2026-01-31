using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rock.Collections.Internals;

public sealed class KeyCollectionDebugView<TKey, TValue>
{
	private readonly ICollection<TKey> m_collection;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public TKey[] Items
	{
		get
		{
			TKey[] array = new TKey[m_collection.Count];
			m_collection.CopyTo(array, 0);
			return array;
		}
	}

	public KeyCollectionDebugView(ICollection<TKey> collection)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		m_collection = collection;
	}
}
