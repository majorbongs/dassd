using System.Collections.Generic;
using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class MechanicShop
{
	public Business ParentBusiness { get; set; }

	public MechanicType Type { get; set; }

	public float SalePercentage { get; set; }

	public List<MechanicShopWorkArea> WorkAreas { get; set; } = new List<MechanicShopWorkArea>();
}
