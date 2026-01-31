using CitizenFX.Core;

namespace Gtacnr.Model;

public struct EntityInfo
{
	public Vector3 Position;

	public int Model;

	public bool Exists;

	public static EntityInfo FromEntity(Entity? entity)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if (entity == (Entity)null)
		{
			return new EntityInfo
			{
				Position = default(Vector3),
				Model = 0,
				Exists = false
			};
		}
		return new EntityInfo
		{
			Position = entity.Position,
			Model = Model.op_Implicit(entity.Model),
			Exists = true
		};
	}
}
