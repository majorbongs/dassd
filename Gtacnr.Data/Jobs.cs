using System.Collections.Generic;
using System.Linq;
using Gtacnr.Model;

namespace Gtacnr.Data;

public static class Jobs
{
	private static readonly Dictionary<string, Job> _jobDefinitions = InitializeJobDefinitions();

	public static IEnumerable<Job> All => _jobDefinitions.Values.ToList();

	private static Dictionary<string, Job> InitializeJobDefinitions()
	{
		Dictionary<string, Job> dictionary = Utils.LoadJson<List<Job>>("data/jobs.json").ToDictionary((Job j) => j.Id, (Job k) => k);
		foreach (Job value in dictionary.Values)
		{
			value.Name = Utils.ResolveLocalization(value.Name);
		}
		return dictionary;
	}

	public static Job? GetJobData(string id)
	{
		if (id == null)
		{
			return null;
		}
		return _jobDefinitions.TryGetRefOrNull(id);
	}
}
