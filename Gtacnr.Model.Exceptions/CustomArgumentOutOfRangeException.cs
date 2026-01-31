using System;
using System.Runtime.Serialization;

namespace Gtacnr.Model.Exceptions;

[Serializable]
public class CustomArgumentOutOfRangeException : Exception
{
	public string ParamName { get; }

	public object ActualValue { get; }

	public CustomArgumentOutOfRangeException()
		: base("Value is out of range.")
	{
	}

	public CustomArgumentOutOfRangeException(string message)
		: base(message)
	{
	}

	public CustomArgumentOutOfRangeException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	public CustomArgumentOutOfRangeException(string paramName, string message)
		: base(message)
	{
		ParamName = paramName;
	}

	public CustomArgumentOutOfRangeException(string paramName, object actualValue, string message)
		: base(message)
	{
		ParamName = paramName;
		ActualValue = actualValue;
	}

	protected CustomArgumentOutOfRangeException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		if (info != null)
		{
			ParamName = info.GetString("ParamName");
			ActualValue = info.GetValue("ActualValue", typeof(object));
		}
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		if (info != null)
		{
			info.AddValue("ParamName", ParamName);
			info.AddValue("ActualValue", ActualValue, typeof(object));
		}
	}

	public override string ToString()
	{
		string text = base.ToString();
		if (ParamName != null)
		{
			text = text + Environment.NewLine + "Parameter name: " + ParamName;
		}
		if (ActualValue != null)
		{
			text = text + Environment.NewLine + "Actual value: " + ActualValue;
		}
		return text;
	}
}
