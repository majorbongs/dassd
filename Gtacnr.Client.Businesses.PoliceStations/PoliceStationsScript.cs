using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Gtacnr.Model;

namespace Gtacnr.Client.Businesses.PoliceStations;

public class PoliceStationsScript : Script
{
	public static List<PoliceDepartment> Departments { get; set; } = Gtacnr.Utils.LoadJson<List<PoliceDepartment>>("data/police/policeDepartments.json");

	public static IEnumerable<PoliceStation> PoliceStations => from b in BusinessScript.Businesses.Values
		where b.PoliceStation != null
		select b.PoliceStation;

	public static PoliceStation CurrentStation => PoliceStations.OrderBy(delegate(PoliceStation s)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		return ((Vector3)(ref position)).DistanceToSquared2D(s.ParentBusiness.Location);
	}).FirstOrDefault();

	public static PoliceStation? GetRandomClosePoliceStation(Vector3 position, bool exceptIfTooClose = false, float maxDistance = 150f, int randomPoolSize = 2)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		return (from station in PoliceStations
			where !exceptIfTooClose || ((Vector3)(ref position)).DistanceToSquared2D(station.ParentBusiness.Location) >= maxDistance.Square()
			orderby ((Vector3)(ref position)).DistanceToSquared2D(station.ParentBusiness.Location)
			select station).Take(randomPoolSize).ToList().Random();
	}
}
