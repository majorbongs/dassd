using System;

namespace CitizenFX.Core;

public class CoroutineCanceledException : Exception
{
	public CoroutineCanceledException()
	{
	}

	public CoroutineCanceledException(string message)
		: base(message)
	{
	}
}
