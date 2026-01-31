using System;

namespace Gtacnr;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class UpdateAttribute : Attribute
{
	public bool StopOnException { get; set; }
}
