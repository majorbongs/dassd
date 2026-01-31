using CitizenFX.Core;
using Gtacnr.Model;

namespace Gtacnr.Client.Jobs;

public class CallInfo : DispatchInfoBase
{
	public Vector3 Position { get; set; }

	public bool Responded { get; set; }

	public string Details { get; set; }

	public override string GetMenuItemDescription()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		string locationName = Utils.GetLocationName(Position);
		string text = ((!string.IsNullOrEmpty(Details)) ? ("Details: " + Details + "~n~") : "");
		return "Location: " + locationName + "~n~" + text + "Responded: " + (Responded ? "~g~Yes" : "~r~No");
	}
}
