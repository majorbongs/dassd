using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.API.Scripts;

public class DamagedScript : Script
{
	private int health;

	private int armor;

	public static DateTime LastDamageReceivedTimestamp { get; private set; }

	[Update]
	private async Coroutine UpdateTask()
	{
		int num = health;
		int num2 = armor;
		health = ((Entity)Game.PlayerPed).Health;
		armor = Game.PlayerPed.Armor;
		if (health < 0)
		{
			health = 0;
		}
		if (health < num || armor < num2)
		{
			int num3 = 0;
			API.GetPedLastDamageBone(((PoolObject)Game.PlayerPed).Handle, ref num3);
			LastDamageReceivedTimestamp = DateTime.UtcNow;
			Game.Player.State.Set("gtacnr:lastDamageT", (object)LastDamageReceivedTimestamp.Ticks, true);
			Game.Player.State.Set("gtacnr:lastDamageAmount", (object)(num + num2 - (health + armor)), true);
			Game.Player.State.Set("gtacnr:lastDamageBone", (object)num3, true);
		}
	}
}
