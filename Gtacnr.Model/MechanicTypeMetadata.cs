using System.Collections.Generic;

namespace Gtacnr.Model;

public class MechanicTypeMetadata : BusinessTypeMetadata
{
	public List<int> BlacklistedClasses { get; set; }

	public List<int> WhitelistedClasses { get; set; }

	public int RepairType { get; set; }
}
