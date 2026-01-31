using System.Collections.Generic;

namespace Gtacnr.Model;

public interface IEconomyItem
{
	List<string> EconomyMultipliers { get; set; }

	bool ShouldAddDefaultMultipliers { get; set; }
}
