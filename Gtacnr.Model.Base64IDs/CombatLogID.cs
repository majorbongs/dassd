namespace Gtacnr.Model.Base64IDs;

public class CombatLogID : Base64ID
{
	protected override string Prefix => "cl-";

	public CombatLogID(ulong id)
		: base(id)
	{
	}

	public CombatLogID(string base64Id)
		: base(base64Id)
	{
	}
}
