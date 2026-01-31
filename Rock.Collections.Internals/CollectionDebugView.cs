using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rock.Collections.Internals;

public sealed class CollectionDebugView<T>
{
	private readonly ICollection<T> m_collection;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Items
	{
		get
		{
			T[] array = new T[m_collection.Count];
			m_collection.CopyTo(array, 0);
			return array;
		}
	}

	public CollectionDebugView(ICollection<T> collection)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		m_collection = collection;
	}
}
