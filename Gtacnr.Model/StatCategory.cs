using System.ComponentModel;

namespace Gtacnr.Model;

public enum StatCategory
{
	[Description("General")]
	General,
	[Description("Criminal")]
	Criminal,
	[Description("Sales")]
	Sales,
	[Description("Police")]
	Police,
	[Description("Paramedic")]
	Paramedic,
	[Description("Mechanic")]
	Mechanic,
	[Description("Delivery Driver")]
	DeliveryDriver,
	[Description("Hitman")]
	Hitman
}
