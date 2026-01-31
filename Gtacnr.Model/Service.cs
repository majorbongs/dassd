using System.Collections.Generic;

namespace Gtacnr.Model;

public class Service : IItemOrService, IEconomyItem
{
	public string Id { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }

	public string Type { get; set; }

	public string BuyerName { get; set; }

	public string BuyerDescription { get; set; }

	public string License { get; set; }

	public bool NeedsVehicle { get; set; }

	public bool NeedsToOwnVehicle { get; set; }

	public bool NeedsToBeAtModShop { get; set; }

	public bool MustNotBeWanted { get; set; }

	public MechanicShopWorkAreaType? WorkAreaType { get; set; }

	public string VehicleType { get; set; }

	public List<string> UseItems { get; set; } = new List<string>();

	public bool CanUseOnSelf { get; set; }

	public List<string> EconomyMultipliers { get; set; } = new List<string>();

	public bool ShouldAddDefaultMultipliers { get; set; } = true;

	public string Disclaimer { get; set; }
}
