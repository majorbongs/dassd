using System;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.API;

public class JobArgs : EventArgs
{
	public string PreviousJobId { get; private set; }

	public JobsEnum PreviousJobEnum { get; private set; }

	public string CurrentJobId { get; private set; }

	public JobsEnum CurrentJobEnum { get; private set; }

	public JobArgs(string previousJobId, string currentJobId)
	{
		PreviousJobId = previousJobId;
		PreviousJobEnum = Gtacnr.Utils.JobMapper.JobToEnum(previousJobId);
		CurrentJobId = currentJobId;
		CurrentJobEnum = Gtacnr.Utils.JobMapper.JobToEnum(currentJobId);
	}
}
