using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using MenuAPI;

namespace Gtacnr.Client.Admin.Menus.EntitiesManagement;

public class EntitiesManagementVehicles : EntitiesManagementBase<Vehicle>
{
	public EntitiesManagementVehicles()
		: base(new Menu("Vehicles", "Manage nearby vehicles"))
	{
	}

	protected override string GetEntityName(Vehicle entity)
	{
		return entity.DisplayName.ToString();
	}

	protected override List<Vehicle> GetAllEntities()
	{
		return (from e in World.GetAllVehicles()
			where EntitiesManagementScript.Instance.ShouldDrawEntity((Entity)(object)e)
			select e).OrderBy(delegate(Vehicle e)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			Vector3 position = ((Entity)e).Position;
			return ((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position);
		}).ToList();
	}
}
