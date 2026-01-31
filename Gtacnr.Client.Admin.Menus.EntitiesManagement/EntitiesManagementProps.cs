using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using MenuAPI;

namespace Gtacnr.Client.Admin.Menus.EntitiesManagement;

public class EntitiesManagementProps : EntitiesManagementBase<Prop>
{
	public EntitiesManagementProps()
		: base(new Menu("Props", "Manage nearby props"))
	{
	}

	protected override string GetEntityName(Prop entity)
	{
		return ((object)((Entity)entity).Model).ToString();
	}

	protected override List<Prop> GetAllEntities()
	{
		return (from e in World.GetAllProps()
			where EntitiesManagementScript.Instance.ShouldDrawEntity((Entity)(object)e)
			select e).OrderBy(delegate(Prop e)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			Vector3 position = ((Entity)e).Position;
			return ((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position);
		}).ToList();
	}
}
