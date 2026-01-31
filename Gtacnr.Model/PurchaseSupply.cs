using System;

namespace Gtacnr.Model;

public class PurchaseSupply
{
	public float Amount { get; set; }

	public int Price { get; set; }

	public float SalesTax { get; set; } = 0.01f;

	public int CalculateFinalPrice(float multiplier = 1f)
	{
		return Convert.ToInt32((double)((float)Price * multiplier) + Math.Ceiling((float)Price * multiplier * SalesTax));
	}

	public int CalculateFinalUnitPrice(float multiplier = 1f)
	{
		return Convert.ToInt32((float)CalculateFinalPrice(multiplier) / Amount);
	}
}
