using System;
using System.Collections.Generic;

namespace Gtacnr.Model;

public class DealershipSupply
{
	private bool hasDiscounts;

	public string Vehicle { get; set; }

	public int Price { get; set; }

	public float SalesTax { get; set; } = 0.01f;

	public bool Unlisted { get; set; }

	public string Notice { get; set; }

	public string Credits { get; set; }

	public PersonalVehicleModel ModelData { get; set; }

	public VehicleColorSet Colors { get; set; }

	public List<int> Liveries { get; set; }

	public List<int> RoofLiveries { get; set; }

	public void ApplyDiscount(double percentOff)
	{
		if (!hasDiscounts)
		{
			Price = Convert.ToInt32(Math.Round((double)Price * (1.0 - percentOff)));
			hasDiscounts = true;
		}
	}

	public int CalculateFinalPrice(float multiplier = 1f)
	{
		return Convert.ToInt32((double)((float)Price * multiplier) + Math.Ceiling((float)Price * multiplier * SalesTax));
	}

	public override string ToString()
	{
		return Vehicle + " | " + Price.ToCurrencyString();
	}
}
