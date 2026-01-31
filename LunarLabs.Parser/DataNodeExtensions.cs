using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace LunarLabs.Parser;

public static class DataNodeExtensions
{
	public static DataNode FromDictionary(this IDictionary dic, string name)
	{
		if (dic == null)
		{
			return null;
		}
		DataNode dataNode = DataNode.CreateObject(name);
		foreach (object key in dic.Keys)
		{
			dataNode.AddField(key.ToString().ToLower(), dic[key]);
		}
		return dataNode;
	}

	public static Dictionary<string, T> ToDictionary<T>(this DataNode node, string name)
	{
		if (node == null)
		{
			return null;
		}
		Dictionary<string, T> dictionary = new Dictionary<string, T>();
		foreach (DataNode child in node.Children)
		{
			if (!string.IsNullOrEmpty(child.Value))
			{
				dictionary[child.Name] = child.AsObject<T>();
			}
		}
		return dictionary;
	}

	public static DataNode ToDataNode<T>(this IEnumerable<T> obj, string name)
	{
		DataNode dataNode = DataNode.CreateArray(name);
		foreach (T item in obj)
		{
			DataNode node = item.ToDataNode();
			dataNode.AddNode(node);
		}
		return dataNode;
	}

	public static T[] ToArray<T>(this DataNode node)
	{
		if (node == null)
		{
			return new T[0];
		}
		string value = typeof(T).Name.ToLower();
		int num = 0;
		foreach (DataNode child in node.Children)
		{
			if (child.Name == null || child.Name.Equals(value))
			{
				num++;
			}
		}
		T[] array = new T[num];
		int num2 = 0;
		foreach (DataNode child2 in node.Children)
		{
			if (child2.Name == null || child2.Name.Equals(value))
			{
				array[num2] = child2.ToObject<T>();
				num2++;
			}
		}
		return array;
	}

	public static bool IsPrimitive(this Type type)
	{
		if (!(type == typeof(byte)) && !(type == typeof(sbyte)) && !(type == typeof(short)) && !(type == typeof(ushort)) && !(type == typeof(int)) && !(type == typeof(uint)) && !(type == typeof(long)) && !(type == typeof(ulong)) && !(type == typeof(float)) && !(type == typeof(double)) && !(type == typeof(decimal)) && !(type == typeof(bool)) && !(type == typeof(string)))
		{
			return type == typeof(DateTime);
		}
		return true;
	}

	private static DataNode FromArray(object obj, string arrayName = null)
	{
		DataNode dataNode = DataNode.CreateArray(arrayName);
		Array array = (Array)obj;
		Type type = array.GetType();
		if (array != null && array.Length > 0)
		{
			Type elementType = type.GetElementType();
			for (int i = 0; i < array.Length; i++)
			{
				object value = array.GetValue(i);
				if (elementType.IsPrimitive())
				{
					dataNode.AddValue(value);
					continue;
				}
				DataNode node = value.ToDataNode(null, isArrayElement: true);
				dataNode.AddNode(node);
			}
		}
		return dataNode;
	}

	public static DataNode ToDataNode(this object obj, string name = null, bool isArrayElement = false)
	{
		if (obj == null)
		{
			return null;
		}
		Type type = obj.GetType();
		if (type.IsArray)
		{
			return FromArray(obj);
		}
		if (type.IsPrimitive())
		{
			throw new Exception("Can't convert primitive type to DataNode");
		}
		IEnumerable<FieldInfo> enumerable = Enumerable.Empty<FieldInfo>();
		Type type2 = type;
		do
		{
			TypeInfo typeInfo = type2.GetTypeInfo();
			_ = type2 == type;
			IEnumerable<FieldInfo> enumerable2 = typeInfo.DeclaredFields.Where((FieldInfo f) => f.IsPublic);
			enumerable2.ToArray();
			enumerable = enumerable2.Concat(enumerable);
			type2 = typeInfo.BaseType;
		}
		while (!(type2 == typeof(object)));
		if (name == null && !isArrayElement)
		{
			name = type.Name.ToLower();
		}
		DataNode dataNode = DataNode.CreateObject(name);
		foreach (FieldInfo item in enumerable)
		{
			object value = item.GetValue(obj);
			string text = item.Name.ToLower();
			TypeInfo typeInfo2 = item.FieldType.GetTypeInfo();
			if (item.FieldType.IsPrimitive() || typeInfo2.IsEnum)
			{
				dataNode.AddField(text, value);
			}
			else if (typeInfo2.IsArray)
			{
				DataNode node = FromArray(value, text);
				dataNode.AddNode(node);
			}
			else if (value != null)
			{
				DataNode node2 = value.ToDataNode(text);
				dataNode.AddNode(node2);
			}
			else
			{
				dataNode.AddField(text, null);
			}
		}
		return dataNode;
	}

	public static T ToObject<T>(this DataNode node)
	{
		if (node == null)
		{
			return default(T);
		}
		return (T)node.ToObject(typeof(T));
	}

	public static object ToObject(this DataNode node, Type objectType)
	{
		if (node == null)
		{
			return null;
		}
		IEnumerable<FieldInfo> enumerable = objectType.GetTypeInfo().DeclaredFields.Where((FieldInfo f) => f.IsPublic);
		object obj = Activator.CreateInstance(objectType);
		foreach (FieldInfo item in enumerable)
		{
			if (!node.HasNode(item.Name))
			{
				continue;
			}
			Type fieldType = item.FieldType;
			if (fieldType.IsPrimitive())
			{
				string text = node.GetString(item.Name);
				if (fieldType == typeof(string))
				{
					item.SetValue(obj, text);
					continue;
				}
				if (fieldType == typeof(byte))
				{
					byte.TryParse(text, out var result);
					item.SetValue(obj, result);
					continue;
				}
				if (fieldType == typeof(sbyte))
				{
					sbyte.TryParse(text, out var result2);
					item.SetValue(obj, result2);
					continue;
				}
				if (fieldType == typeof(short))
				{
					short.TryParse(text, out var result3);
					item.SetValue(obj, result3);
					continue;
				}
				if (fieldType == typeof(ushort))
				{
					ushort.TryParse(text, out var result4);
					item.SetValue(obj, result4);
					continue;
				}
				if (fieldType == typeof(int))
				{
					int.TryParse(text, out var result5);
					item.SetValue(obj, result5);
					continue;
				}
				if (fieldType == typeof(uint))
				{
					uint.TryParse(text, out var result6);
					item.SetValue(obj, result6);
					continue;
				}
				if (fieldType == typeof(long))
				{
					long.TryParse(text, out var result7);
					item.SetValue(obj, result7);
					continue;
				}
				if (fieldType == typeof(ulong))
				{
					ulong.TryParse(text, out var result8);
					item.SetValue(obj, result8);
					continue;
				}
				if (fieldType == typeof(float))
				{
					float.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out var result9);
					item.SetValue(obj, result9);
					continue;
				}
				if (fieldType == typeof(double))
				{
					double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out var result10);
					item.SetValue(obj, result10);
					continue;
				}
				if (fieldType == typeof(decimal))
				{
					decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out var result11);
					item.SetValue(obj, result11);
					continue;
				}
				if (!(fieldType == typeof(bool)))
				{
					throw new Exception("Cannot unserialize field of type " + objectType.Name);
				}
				bool.TryParse(text, out var result12);
				item.SetValue(obj, result12);
			}
			else
			{
				object value = node.GetNodeByName(item.Name).ToObject(fieldType);
				item.SetValue(obj, value);
			}
		}
		return Convert.ChangeType(obj, objectType);
	}

	public static DataNode FromHashSet<T>(this HashSet<T> set, string name)
	{
		DataNode dataNode = DataNode.CreateArray(name);
		foreach (T item in set)
		{
			dataNode.AddValue(item);
		}
		return dataNode;
	}

	public static HashSet<T> ToHashSet<T>(this DataNode node, string name)
	{
		HashSet<T> hashSet = new HashSet<T>();
		foreach (DataNode child in node.Children)
		{
			T item = child.AsObject<T>();
			hashSet.Add(item);
		}
		return hashSet;
	}
}
