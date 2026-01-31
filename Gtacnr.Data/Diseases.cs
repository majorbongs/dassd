using System.Collections.Generic;
using System.Linq;
using Gtacnr.Model;

namespace Gtacnr.Data;

public static class Diseases
{
	private static Dictionary<string, Disease> _diseasesById;

	private static Dictionary<DiseaseSpawnMode, List<Disease>> _diseasesBySpawnMode;

	static Diseases()
	{
		InitializeDiseases();
	}

	private static void InitializeDiseases()
	{
		_diseasesById = Utils.LoadJson<List<Disease>>("data/diseases.json").ToDictionary((Disease d) => d.Id, (Disease g) => g);
		_diseasesBySpawnMode = (from g in _diseasesById.Values
			group g by g.SpawnMode).ToDictionary((IGrouping<DiseaseSpawnMode, Disease> g) => g.Key, (IGrouping<DiseaseSpawnMode, Disease> g) => g.ToList());
	}

	public static Disease? GetDiseaseById(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		return _diseasesById.TryGetRefOrNull(id);
	}

	public static ICollection<Disease> GetDiseasesBySpawnMode(DiseaseSpawnMode spawnMode)
	{
		return _diseasesBySpawnMode.TryGetRefOrNull(spawnMode) ?? new List<Disease>();
	}
}
