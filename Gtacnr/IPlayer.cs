using System.Collections.Generic;
using CitizenFX.Core;

namespace Gtacnr;

public interface IPlayer
{
	Player? Player { get; set; }

	int Id { get; set; }

	string? UserId { get; set; }

	string? CharId { get; set; }

	IDictionary<XAccountType, long> Money { get; set; }

	IList<IInventory> Inventories { get; set; }
}
