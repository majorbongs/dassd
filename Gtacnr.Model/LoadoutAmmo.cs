using CitizenFX.Core.Native;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class LoadoutAmmo
{
	public string ItemId { get; set; }

	public int Amount { get; set; }

	[JsonIgnore]
	public uint Hash => (uint)API.GetHashKey(ItemId);

	public override string ToString()
	{
		return ItemId;
	}
}
