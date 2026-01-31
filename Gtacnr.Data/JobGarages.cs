using System.Collections.Generic;
using System.Linq;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Data;

public static class JobGarages
{
	private static Dictionary<string, JobGarage> _jobGaragesById;

	private static Dictionary<JobsEnum, List<JobGarage>> _jobGaragesByJob;

	static JobGarages()
	{
		InitializeJobGarages();
	}

	private static void InitializeJobGarages()
	{
		_jobGaragesById = (from g in Utils.LoadJson<List<JobGarage>>("data/estates/garages/jobGarages.json")
			where g.HasRequiredResource()
			select g).ToDictionary((JobGarage g) => g.Id, (JobGarage g) => g);
		_jobGaragesByJob = (from g in _jobGaragesById.Values
			group g by Utils.JobMapper.JobToEnum(g.Job)).ToDictionary((IGrouping<JobsEnum, JobGarage> g) => g.Key, (IGrouping<JobsEnum, JobGarage> g) => g.ToList());
	}

	public static JobGarage? GetJobGarageById(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		return _jobGaragesById.TryGetRefOrNull(id);
	}

	public static ICollection<JobGarage> GetJobGaragesByJobEnum(JobsEnum jobsEnum)
	{
		return _jobGaragesByJob.TryGetRefOrNull(jobsEnum) ?? new List<JobGarage>();
	}
}
