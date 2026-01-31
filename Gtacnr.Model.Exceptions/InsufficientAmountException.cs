using System;

namespace Gtacnr.Model.Exceptions;

public class InsufficientAmountException : Exception
{
	public InsufficientAmountException(float amount)
		: base($"Insufficient amount: {amount}")
	{
	}

	public InsufficientAmountException(decimal amount)
		: base($"Insufficient amount: {amount}")
	{
	}
}
