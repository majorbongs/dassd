using System;

namespace Gtacnr.Client.Jobs;

public class SaleInfo
{
	public DateTime DateTime { get; set; }

	public int CustomerId { get; set; }

	public string ItemId { get; set; }

	public float Amount { get; set; }

	public int Price { get; set; }
}
