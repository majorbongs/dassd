using System;
using System.ComponentModel;

namespace Gtacnr.Model.Enums;

[Flags]
public enum DeliveryJobVehicleType
{
	[Description("Van")]
	Van = 1,
	[Description("Box Truck")]
	BoxTruck = 2,
	[Description("Semi Truck")]
	SemiTruck = 4,
	[Description("Trash")]
	Trash = 8,
	[Description("Food")]
	Food = 0x10,
	[Description("Delivery Truck")]
	DeliveryTruck = 3,
	[Description("Any")]
	Any = 7
}
