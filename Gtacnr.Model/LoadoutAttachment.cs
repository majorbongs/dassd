using CitizenFX.Core.Native;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class LoadoutAttachment
{
	public string ItemId { get; set; }

	public bool IsEquipped { get; set; }

	[JsonIgnore]
	public uint Hash => (uint)API.GetHashKey(ItemId);

	public override string ToString()
	{
		return ItemId;
	}
}
