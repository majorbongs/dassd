using System;
using System.Collections.Generic;
using CitizenFX.Core.Native;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class LoadoutWeapon
{
	public string ItemId { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public int Tint { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public List<LoadoutAttachment> Attachments { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public Tuple<int, int> Livery { get; set; }

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public bool IsEquipped { get; set; }

	[JsonIgnore]
	public uint Hash => (uint)API.GetHashKey(ItemId);

	public override string ToString()
	{
		return ItemId;
	}
}
