using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using MenuAPI;

namespace Gtacnr.Client.Admin.Menus.EntitiesManagement;

public class EntitiesManagementPeds : EntitiesManagementBase<Ped>
{
	public EntitiesManagementPeds()
		: base(new Menu("Peds", "Manage nearby peds"))
	{
	}

	protected override string GetEntityName(Ped entity)
	{
		return ((object)((Entity)entity).Model).ToString();
	}

	protected override List<Ped> GetAllEntities()
	{
		return (from e in World.GetAllPeds()
			where EntitiesManagementScript.Instance.ShouldDrawEntity((Entity)(object)e)
			select e).OrderBy(delegate(Ped e)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			Vector3 position = ((Entity)e).Position;
			return ((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position);
		}).ToList();
	}
}
