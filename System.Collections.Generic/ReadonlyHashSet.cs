namespace System.Collections.Generic;

public class ReadonlyHashSet<T> : IReadonlyHashSet<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
{
	private HashSet<T> set;

	public int Count => set.Count;

	public ReadonlyHashSet(HashSet<T> set)
	{
		this.set = set;
	}

	public bool Contains(T i)
	{
		return set.Contains(i);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return set.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return set.GetEnumerator();
	}
}
