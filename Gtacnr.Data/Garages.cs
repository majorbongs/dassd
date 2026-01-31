using System.Collections.Generic;
using System.Linq;
using Gtacnr.Model;
using Rock.Collections;

namespace Gtacnr.Data;

public static class Garages
{
	private static readonly OrderedDictionary<string, Garage> _garagesById = InitializeGarages();

	public static IEnumerable<Garage> AllGarages => _garagesById.Values.AsEnumerable();

	private static OrderedDictionary<string, Garage> InitializeGarages()
	{
		OrderedDictionary<string, Garage> orderedDictionary = Utils.LoadJson<List<Garage>>("data/estates/garages/garages.json").ToOrderedDictionary((Garage g) => g.Id);
		Dictionary<string, GarageInterior> dictionary = Utils.LoadJson<List<GarageInterior>>("data/estates/garages/garageInteriors.json").ToDictionary((GarageInterior i) => i.Id);
		foreach (Garage value in orderedDictionary.Values)
		{
			if (value.InteriorId != null && dictionary.ContainsKey(value.InteriorId))
			{
				value.Interior = dictionary[value.InteriorId];
			}
		}
		return orderedDictionary;
	}

	public static Garage? GetGarageById(string? garageId)
	{
		if (string.IsNullOrEmpty(garageId))
		{
			return null;
		}
		if (!_garagesById.ContainsKey(garageId))
		{
			return null;
		}
		return _garagesById[garageId];
	}
}
