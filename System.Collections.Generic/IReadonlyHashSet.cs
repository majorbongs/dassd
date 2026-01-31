namespace System.Collections.Generic;

public interface IReadonlyHashSet<T> : IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
{
	bool Contains(T i);
}
