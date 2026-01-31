using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace LunarLabs.Parser;

public class DataNode : IEnumerable<DataNode>, IEnumerable
{
	protected List<DataNode> _children = new List<DataNode>();

	private static readonly long epochTicks = new DateTime(1970, 1, 1).Ticks;

	public IEnumerable<DataNode> Children => _children;

	public DataNode Parent { get; private set; }

	public int ChildCount => _children.Count;

	public bool HasChildren => _children.Count > 0;

	public string Name { get; set; }

	public string Value { get; set; }

	public NodeKind Kind { get; private set; }

	public DataNode this[string name]
	{
		get
		{
			DataNode nodeByName = GetNodeByName(name);
			if (nodeByName == null)
			{
				return AddEmptyNode(name);
			}
			return nodeByName;
		}
	}

	public DataNode this[int index] => GetNodeByIndex(index);

	private DataNode(NodeKind kind, string name = null, string value = null)
	{
		Kind = kind;
		Parent = null;
		Name = name;
		Value = value;
	}

	public IEnumerator<DataNode> GetEnumerator()
	{
		return _children.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _children.GetEnumerator();
	}

	public static DataNode CreateString(string name = null)
	{
		return new DataNode(NodeKind.String, name);
	}

	public static DataNode CreateNumber(string name = null)
	{
		return new DataNode(NodeKind.Numeric, name);
	}

	public static DataNode CreateObject(string name = null)
	{
		return new DataNode(NodeKind.Object, name);
	}

	public static DataNode CreateArray(string name = null)
	{
		return new DataNode(NodeKind.Array, name);
	}

	public static DataNode CreateValue(object value)
	{
		NodeKind kind;
		string value2 = ConvertValue(value, out kind);
		return new DataNode(kind, null, value2);
	}

	public override string ToString()
	{
		if (ChildCount == 0 && !string.IsNullOrEmpty(Value))
		{
			return Value;
		}
		if (!string.IsNullOrEmpty(Name))
		{
			return Name ?? "";
		}
		if (Parent == null)
		{
			return "[Root]";
		}
		return "[Null]";
	}

	public bool RemoveNode(DataNode node)
	{
		if (node == null)
		{
			return false;
		}
		int count = _children.Count;
		_children.Remove(node);
		return _children.Count < count;
	}

	public bool RemoveNodeByName(string name)
	{
		DataNode nodeByName = GetNodeByName(name);
		return RemoveNode(nodeByName);
	}

	public bool RemoveNodeByIndex(int index)
	{
		if (index < 0 || index >= _children.Count)
		{
			return false;
		}
		_children.RemoveAt(index);
		return true;
	}

	public DataNode AddNode(DataNode node)
	{
		if (node == null)
		{
			return null;
		}
		_children.Add(node);
		node.Parent = this;
		return node;
	}

	public DataNode AddEmptyNode(string name)
	{
		DataNode node = CreateObject(name);
		return AddNode(node);
	}

	public DataNode AddValue(object value)
	{
		return AddField(null, value);
	}

	public DataNode AddField(string name, object value)
	{
		if (Kind != NodeKind.Array && Kind != NodeKind.Object)
		{
			throw new Exception("The kind of this node is not 'object'!");
		}
		if (value is DataNode)
		{
			throw new Exception("Cannot add a node as a field!");
		}
		NodeKind kind;
		string value2 = ConvertValue(value, out kind);
		DataNode dataNode = new DataNode(kind, name, value2);
		AddNode(dataNode);
		return dataNode;
	}

	public DataNode SetField(string name, object value)
	{
		DataNode nodeByName = GetNodeByName(name);
		if (nodeByName == null)
		{
			return AddField(name, value);
		}
		nodeByName.SetValue(value);
		return nodeByName;
	}

	public void SetValue(object value)
	{
		Value = ConvertValue(value, out var _);
	}

	private static string ConvertValue(object value, out NodeKind kind)
	{
		if (value == null)
		{
			kind = NodeKind.Null;
			return "";
		}
		string result;
		if (value is int num)
		{
			result = num.ToString(CultureInfo.InvariantCulture);
			kind = NodeKind.Numeric;
		}
		else if (value is uint num2)
		{
			result = num2.ToString(CultureInfo.InvariantCulture);
			kind = NodeKind.Numeric;
		}
		else if (value is long num3)
		{
			result = num3.ToString(CultureInfo.InvariantCulture);
			kind = NodeKind.Numeric;
		}
		else if (value is ulong num4)
		{
			result = num4.ToString(CultureInfo.InvariantCulture);
			kind = NodeKind.Numeric;
		}
		else if (value is byte b)
		{
			result = b.ToString(CultureInfo.InvariantCulture);
			kind = NodeKind.Numeric;
		}
		else if (value is sbyte b2)
		{
			result = b2.ToString(CultureInfo.InvariantCulture);
			kind = NodeKind.Numeric;
		}
		else if (value is short num5)
		{
			result = num5.ToString(CultureInfo.InvariantCulture);
			kind = NodeKind.Numeric;
		}
		else if (value is ushort num6)
		{
			result = num6.ToString(CultureInfo.InvariantCulture);
			kind = NodeKind.Numeric;
		}
		else if (value is float num7)
		{
			result = num7.ToString(CultureInfo.InvariantCulture);
			kind = NodeKind.Numeric;
		}
		else if (value is double num8)
		{
			result = num8.ToString(CultureInfo.InvariantCulture);
			kind = NodeKind.Numeric;
		}
		else if (value is decimal num9)
		{
			result = num9.ToString(CultureInfo.InvariantCulture);
			kind = NodeKind.Numeric;
		}
		else if (value is bool)
		{
			result = (((bool)value) ? "true" : "false");
			kind = NodeKind.Boolean;
		}
		else
		{
			result = value.ToString();
			kind = NodeKind.String;
		}
		return result;
	}

	public bool HasNode(string name, int index = 0)
	{
		return GetNodeByName(name, index) != null;
	}

	private DataNode FindNode(string name, int ndepth, int maxdepth)
	{
		if (string.Compare(Name, name, StringComparison.OrdinalIgnoreCase) == 0)
		{
			return this;
		}
		if (ndepth >= maxdepth)
		{
			return null;
		}
		foreach (DataNode child in _children)
		{
			DataNode dataNode = child.FindNode(name, ndepth + 1, maxdepth);
			if (dataNode != null)
			{
				return dataNode;
			}
		}
		return null;
	}

	public DataNode FindNode(string name, int maxdepth = 0)
	{
		return FindNode(name, 0, (maxdepth > 0) ? maxdepth : int.MaxValue);
	}

	[Obsolete("GetNode is deprecated, please use GetNodeByName instead.")]
	public DataNode GetNode(string name, int index = 0)
	{
		return GetNodeByName(name, index);
	}

	public DataNode GetNodeByName(string name, int index = 0)
	{
		int num = 0;
		foreach (DataNode child in _children)
		{
			if (string.Compare(child.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
			{
				if (num >= index)
				{
					return child;
				}
				num++;
			}
		}
		return null;
	}

	public DataNode GetNodeByIndex(int index)
	{
		if (index < 0 || index >= _children.Count)
		{
			return null;
		}
		return _children[index];
	}

	public long AsInt64(long defaultValue = 0L)
	{
		long result = defaultValue;
		if (long.TryParse(Value, out result))
		{
			return result;
		}
		return defaultValue;
	}

	public long GetInt64(string name, long defaultValue = 0L)
	{
		return GetNodeByName(name)?.AsInt64(defaultValue) ?? defaultValue;
	}

	public long GetInt64(int index, long defaultValue = 0L)
	{
		return GetNodeByIndex(index)?.AsInt64(defaultValue) ?? defaultValue;
	}

	public ulong AsUInt64(ulong defaultValue = 0uL)
	{
		ulong result = defaultValue;
		if (ulong.TryParse(Value, out result))
		{
			return result;
		}
		return defaultValue;
	}

	public ulong GetUInt64(string name, ulong defaultValue = 0uL)
	{
		return GetNodeByName(name)?.AsUInt64(defaultValue) ?? defaultValue;
	}

	public ulong GetUInt64(int index, ulong defaultValue = 0uL)
	{
		return GetNodeByIndex(index)?.AsUInt64(defaultValue) ?? defaultValue;
	}

	public int AsInt32(int defaultValue = 0)
	{
		int result = defaultValue;
		if (int.TryParse(Value, out result))
		{
			return result;
		}
		return defaultValue;
	}

	public int GetInt32(string name, int defaultValue = 0)
	{
		return GetNodeByName(name)?.AsInt32(defaultValue) ?? defaultValue;
	}

	public int GetInt32(int index, int defaultValue = 0)
	{
		return GetNodeByIndex(index)?.AsInt32(defaultValue) ?? defaultValue;
	}

	public uint AsUInt32(uint defaultValue = 0u)
	{
		uint result = defaultValue;
		if (uint.TryParse(Value, out result))
		{
			return result;
		}
		return defaultValue;
	}

	public uint GetUInt32(string name, uint defaultValue = 0u)
	{
		return GetNodeByName(name)?.AsUInt32(defaultValue) ?? defaultValue;
	}

	public byte AsByte(byte defaultValue = 0)
	{
		byte result = defaultValue;
		if (byte.TryParse(Value, out result))
		{
			return result;
		}
		return defaultValue;
	}

	public byte GetByte(string name, byte defaultValue = 0)
	{
		return GetNodeByName(name)?.AsByte(defaultValue) ?? defaultValue;
	}

	public byte GetByte(int index, byte defaultValue = 0)
	{
		return GetNodeByIndex(index)?.AsByte(defaultValue) ?? defaultValue;
	}

	public sbyte AsSByte(sbyte defaultValue = 0)
	{
		sbyte result = defaultValue;
		if (sbyte.TryParse(Value, out result))
		{
			return result;
		}
		return defaultValue;
	}

	public sbyte GetSByte(string name, sbyte defaultValue = 0)
	{
		return GetNodeByName(name)?.AsSByte(defaultValue) ?? defaultValue;
	}

	public sbyte GetSByte(int index, sbyte defaultValue = 0)
	{
		return GetNodeByIndex(index)?.AsSByte(defaultValue) ?? defaultValue;
	}

	public T AsEnum<T>(T defaultValue = default(T)) where T : Enum
	{
		return _AsEnum(defaultValue);
	}

	public T _AsEnum<T>(T defaultValue = default(T))
	{
		try
		{
			return (T)Enum.Parse(typeof(T), Value, ignoreCase: true);
		}
		catch (Exception)
		{
			int result = 0;
			if (int.TryParse(Value, out result))
			{
				return (T)(object)result;
			}
			return defaultValue;
		}
	}

	public T GetEnum<T>(string name, T defaultValue = default(T)) where T : Enum
	{
		DataNode nodeByName = GetNodeByName(name);
		if (nodeByName != null)
		{
			return nodeByName.AsEnum(defaultValue);
		}
		return defaultValue;
	}

	public T GetEnum<T>(int index, T defaultValue = default(T)) where T : Enum
	{
		DataNode nodeByIndex = GetNodeByIndex(index);
		if (nodeByIndex != null)
		{
			return nodeByIndex.AsEnum(defaultValue);
		}
		return defaultValue;
	}

	public bool AsBool(bool defaultValue = false)
	{
		if (Value.Equals("1") || string.Equals(Value, "true", StringComparison.CurrentCultureIgnoreCase))
		{
			return true;
		}
		if (Value.Equals("0") || string.Equals(Value, "false", StringComparison.CurrentCultureIgnoreCase))
		{
			return false;
		}
		return defaultValue;
	}

	public bool GetBool(string name, bool defaultValue = false)
	{
		return GetNodeByName(name)?.AsBool(defaultValue) ?? defaultValue;
	}

	public bool GetBool(int index, bool defaultValue = false)
	{
		return GetNodeByIndex(index)?.AsBool(defaultValue) ?? defaultValue;
	}

	public float AsFloat(float defaultValue = 0f)
	{
		float result = defaultValue;
		if (float.TryParse(Value, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out result))
		{
			return result;
		}
		return defaultValue;
	}

	public float GetFloat(string name, float defaultValue = 0f)
	{
		return GetNodeByName(name)?.AsFloat(defaultValue) ?? defaultValue;
	}

	public float GetFloat(int index, float defaultValue = 0f)
	{
		return GetNodeByIndex(index)?.AsFloat(defaultValue) ?? defaultValue;
	}

	public double AsDouble(double defaultValue = 0.0)
	{
		double result = defaultValue;
		if (double.TryParse(Value, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out result))
		{
			return result;
		}
		return defaultValue;
	}

	public double GetDouble(string name, double defaultValue = 0.0)
	{
		return GetNodeByName(name)?.AsDouble(defaultValue) ?? defaultValue;
	}

	public double GetDouble(int index, double defaultValue = 0.0)
	{
		return GetNodeByIndex(index)?.AsDouble(defaultValue) ?? defaultValue;
	}

	public decimal AsDecimal(decimal defaultValue = 0m)
	{
		decimal result = defaultValue;
		if (decimal.TryParse(Value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture.NumberFormat, out result))
		{
			return result;
		}
		return defaultValue;
	}

	public decimal GetDecimal(string name, decimal defaultValue = 0m)
	{
		return GetNodeByName(name)?.AsDecimal(defaultValue) ?? defaultValue;
	}

	public decimal GetDecimal(int index, decimal defaultValue = 0m)
	{
		return GetNodeByIndex(index)?.AsDecimal(defaultValue) ?? defaultValue;
	}

	public string AsString(string defaultValue = "")
	{
		if (Value != null)
		{
			return Value;
		}
		return defaultValue;
	}

	public string GetString(string name, string defaultValue = "")
	{
		DataNode nodeByName = GetNodeByName(name);
		if (nodeByName != null)
		{
			return nodeByName.Value;
		}
		return defaultValue;
	}

	public string GetString(int index, string defaultValue = "")
	{
		DataNode nodeByIndex = GetNodeByIndex(index);
		if (nodeByIndex != null)
		{
			return nodeByIndex.Value;
		}
		return defaultValue;
	}

	public DateTime AsDateTime(DateTime defaultValue = default(DateTime))
	{
		if (DateTime.TryParse(Value, out var result))
		{
			return result;
		}
		return defaultValue;
	}

	public DateTime GetDateTime(string name, DateTime defaultValue = default(DateTime))
	{
		return GetNodeByName(name)?.AsDateTime(defaultValue) ?? defaultValue;
	}

	public T AsObject<T>()
	{
		Type typeFromHandle = typeof(T);
		if (typeFromHandle == typeof(int))
		{
			return (T)(object)AsInt32();
		}
		if (typeFromHandle == typeof(uint))
		{
			return (T)(object)AsUInt32();
		}
		if (typeFromHandle == typeof(string))
		{
			return (T)(object)AsString();
		}
		if (typeFromHandle == typeof(bool))
		{
			return (T)(object)AsBool();
		}
		if (typeFromHandle == typeof(DateTime))
		{
			return (T)(object)AsDateTime();
		}
		if (typeFromHandle == typeof(float))
		{
			return (T)(object)AsFloat();
		}
		if (typeFromHandle == typeof(decimal))
		{
			return (T)(object)AsDecimal();
		}
		if (typeFromHandle == typeof(byte))
		{
			return (T)(object)AsByte(0);
		}
		if (typeFromHandle == typeof(sbyte))
		{
			return (T)(object)AsSByte(0);
		}
		if (typeFromHandle == typeof(double))
		{
			return (T)(object)AsDouble();
		}
		if (typeFromHandle == typeof(long))
		{
			return (T)(object)AsInt64(0L);
		}
		if (typeFromHandle == typeof(ulong))
		{
			return (T)(object)AsUInt64(0uL);
		}
		if (typeFromHandle.IsEnum)
		{
			return _AsEnum<T>();
		}
		return default(T);
	}

	public T GetObject<T>(string name, T defaultValue)
	{
		DataNode nodeByName = GetNodeByName(name);
		if (nodeByName != null)
		{
			return nodeByName.AsObject<T>();
		}
		return defaultValue;
	}
}
