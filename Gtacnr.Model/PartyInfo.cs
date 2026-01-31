using System.Collections.Generic;
using System.Linq;

namespace Gtacnr.Model;

public class PartyInfo
{
	public List<int> Players { get; private set; }

	public int Leader { get; set; }

	public PartyInfo(int leader)
	{
		Players = new List<int> { leader };
		Leader = leader;
	}

	public void RemovePlayer(int player)
	{
		Players.Remove(player);
		if (player == Leader)
		{
			Leader = Players.FirstOrDefault();
		}
	}
}
