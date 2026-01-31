using System;

namespace Gtacnr.Client.Crimes;

public class PlayerDamagedEvent : EventArgs
{
	public int playerId;

	public int playerPed;

	public int weapon;

	public PlayerDamagedEvent(int playerId, int playerPed, int weapon)
	{
		this.playerId = playerId;
		this.playerPed = playerPed;
		this.weapon = weapon;
	}
}
