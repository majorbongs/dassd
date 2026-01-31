using System;

namespace Gtacnr.Model.Exceptions;

public class JobNotAllowedException : Exception
{
	public JobNotAllowedException(string jobId)
		: base(jobId)
	{
	}
}
