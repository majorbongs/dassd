namespace System.Collections.Generic;

public static class HashSetExtensions
{
	public static ReadonlyHashSet<T> AsReadOnly<T>(this HashSet<T> s)
	{
		return new ReadonlyHashSet<T>(s);
	}

	public static IReadonlyHashSet<T> ToReadOnlyHashSet<T>(this IEnumerable<T> enumerable)
	{
		return new HashSet<T>(enumerable).AsReadOnly();
	}
}
