using System;

namespace Gtacnr.Model.Exceptions;

public class InvalidItemException : Exception
{
	public InvalidItemException(string itemId)
		: base(itemId)
	{
	}
}
