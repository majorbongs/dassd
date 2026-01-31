using System;

namespace Gtacnr;

public class ValueAttribute : Attribute
{
	public string Value { get; set; }

	public ValueAttribute(string value)
	{
		Value = value;
	}
}
