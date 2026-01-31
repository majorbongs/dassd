using System;
using System.Collections.Generic;
using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class InventoryItemBase : IItemOrService, IEconomyItem, IExtraDataContainer
{
	public string Id { get; set; }

	public string Name { get; set; } = "<unnamed item>";

	public string Description { get; set; } = "";

	public float Weight { get; set; }

	public float Limit { get; set; }

	public ItemCategory Category { get; set; } = ItemCategory.Other;

	public ItemRarity Rarity { get; set; }

	public string Unit { get; set; }

	public List<string> EconomyMultipliers { get; set; } = new List<string>();

	public bool ShouldAddDefaultMultipliers { get; set; } = true;

	public string Model { get; set; }

	public bool CanSell { get; set; } = true;

	public bool CanMove { get; set; } = true;

	public bool IsIllegal { get; set; }

	public bool IsIllegalForSelling { get; set; }

	public bool IsStolen { get; set; }

	public int SeizeValue { get; set; }

	public bool IsPoliceOnly { get; set; }

	public bool CanBeUsedByEMS { get; set; } = true;

	public string Alias { get; set; }

	public int RequiredLevel { get; set; }

	public MembershipTier RequiredMembership { get; set; }

	public DateTime CreationDate { get; set; }

	public DateTime DisabledDate { get; set; } = DateTime.MaxValue;

	public List<SellableItemSupply> DefaultSupplies { get; set; } = new List<SellableItemSupply>();

	public string DefaultPath { get; set; } = "";

	public Dictionary<string, float> JobLimits { get; set; } = new Dictionary<string, float>();

	public string Disclaimer { get; set; }

	public string Credits { get; set; }

	public Dictionary<string, object> ExtraData { get; set; } = new Dictionary<string, object>();
}
