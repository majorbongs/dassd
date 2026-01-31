using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.Police;

public class ServeProtectScript : Script
{
	public static async void OnKill(Entity victim, Entity aggressor, int weapon)
	{
		if (API.IsPedAPlayer(((PoolObject)victim).Handle))
		{
			return;
		}
		Ped val = (Ped)(object)((victim is Ped) ? victim : null);
		if (val != null && !val.IsInCombat && !val.IsInMeleeCombat && (int)val.Weapons.Current.Hash == -1569615261 && API.GetPedType(((PoolObject)victim).Handle) != 28)
		{
			JobsEnum cachedJobEnum = Gtacnr.Client.API.Jobs.CachedJobEnum;
			if (cachedJobEnum.IsPolice())
			{
				Utils.SendNotification("~b~Police officers ~s~are expected to protect and serve citizens. Innocent civilian NPC casualties will result in small ~r~XP losses~s~.");
			}
			else if (cachedJobEnum.IsEMSOrFD())
			{
				Utils.SendNotification("~p~Paramedics ~s~are expected to save the lives of citizens. Innocent civilian NPC casualties will result in small ~r~XP losses~s~.");
			}
			BaseScript.TriggerServerEvent("gtacnr:police:onNPCCasualty", new object[1] { victim.NetworkId });
		}
	}
}
