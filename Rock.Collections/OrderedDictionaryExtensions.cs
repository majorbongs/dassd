using System;
using System.Collections.Generic;

namespace Rock.Collections;

public static class OrderedDictionaryExtensions
{
	public static OrderedDictionary<TKey, TValue> ToOrderedDictionary<TKey, TValue>(this IEnumerable<TValue> source, Func<TValue, TKey> keySelector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		OrderedDictionary<TKey, TValue> orderedDictionary = new OrderedDictionary<TKey, TValue>();
		foreach (TValue item in source)
		{
			TKey val = keySelector(item);
			if (!orderedDictionary.ContainsKey(val))
			{
				orderedDictionary.Add(val, item);
				continue;
			}
			throw new ArgumentException($"An element with the same key ({val}) already exists.");
		}
		return orderedDictionary;
	}
}
