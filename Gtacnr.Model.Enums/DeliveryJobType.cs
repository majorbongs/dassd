using System.ComponentModel;

namespace Gtacnr.Model.Enums;

public enum DeliveryJobType
{
	[Description("Parcel Delivery")]
	Parcel,
	[Description("Store Delivery")]
	Restock,
	[Description("Long-Haul Delivery")]
	LongHaul,
	[Description("Fuel Delivery")]
	Fuel,
	[Description("Wood Delivery")]
	Logs,
	[Description("Special Delivery")]
	Special,
	[Description("Food Delivery")]
	Food,
	[Description("Trash Pickup")]
	Trash
}
