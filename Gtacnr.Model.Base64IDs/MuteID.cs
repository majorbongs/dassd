namespace Gtacnr.Model.Base64IDs;

public class MuteID : Base64ID
{
	protected override string Prefix => "mute-";

	public MuteID(ulong id)
		: base(id)
	{
	}

	public MuteID(string base64Id)
		: base(base64Id)
	{
	}
}
