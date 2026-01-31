using System;
using System.Collections.Generic;
using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class BusinessSupply
{
	public BusinessSupplyType Type { get; set; }

	public string Item { get; set; }

	public int Price { get; set; }

	public float SalesTax { get; set; } = 0.01f;

	public string Path { get; set; }

	public List<float> PurchaseAmounts { get; set; } = new List<float>();

	public bool AutoSelectAmount { get; set; }

	public List<PurchaseSupply> PurchaseSupplies { get; set; } = new List<PurchaseSupply>();

	public bool IsJobSupply { get; set; }

	public string Job { get; set; }

	public string Department { get; set; }

	public object Extra { get; set; }

	public bool IsVisible { get; set; } = true;

	public float Amount { get; set; } = -1f;

	public float RandomAmountMin { get; set; }

	public float RandomAmountMax { get; set; }

	public float InStockChance { get; set; } = 1f;

	public string Id { get; set; }

	public string BusinessId { get; set; }

	public float Count { get; set; }

	public bool CanRemove { get; set; }

	public override string ToString()
	{
		return Item + " | " + Price.ToCurrencyString();
	}

	public int CalculateFinalPrice(float multiplier = 1f)
	{
		return Convert.ToInt32((double)((float)Price * multiplier) + Math.Ceiling((float)Price * multiplier * SalesTax));
	}

	public int CalculateFinalPrice(Business business, string category)
	{
		return Convert.ToInt32((double)((float)Price * business.PriceMultiplier * business.PriceMultipliers[category]) + Math.Ceiling((float)Price * business.PriceMultiplier * business.PriceMultipliers[category] * SalesTax));
	}

	public int CalculateFinalDemandPayout(float multiplier = 1f)
	{
		return Convert.ToInt32((float)Price * multiplier);
	}

	public int CalculateFinalDemandPayout(Business business)
	{
		return Convert.ToInt32((float)Price * business.DemandPayoutMultiplier);
	}
}
