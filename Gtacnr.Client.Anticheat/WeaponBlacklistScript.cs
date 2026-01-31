using System.Collections.Generic;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Weapons;
using Gtacnr.Data;
using Gtacnr.Localization;

namespace Gtacnr.Client.Anticheat;

public class WeaponBlacklistScript : Script
{
	private HashSet<int> policeOnlyWeapons = new HashSet<int>
	{
		API.GetHashKey("weapon_stungun"),
		API.GetHashKey("weapon_nightstick")
	};

	public WeaponBlacklistScript()
	{
		WeaponEventsScript.WeaponChanged += OnWeaponChanged;
	}

	private void OnWeaponChanged(object sender, WeaponEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected I4, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Invalid comparison between Unknown and I4
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected I4, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected I4, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected I4, but got Unknown
		if ((int)e.NewWeaponHash != -1569615261)
		{
			bool flag = Gtacnr.Data.Items.GetWeaponDefinitionByHash((uint)(int)e.NewWeaponHash) != null;
			if ((int)e.NewWeaponHash == 966099553)
			{
				flag = true;
			}
			if (!flag)
			{
				API.RemoveWeaponFromPed(API.PlayerPedId(), (uint)(int)e.NewWeaponHash);
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.WEAPON_CANNOT_BE_USED));
			}
			else if (policeOnlyWeapons.Contains((int)e.NewWeaponHash) && !Gtacnr.Client.API.Jobs.CachedJobEnum.IsPolice())
			{
				API.RemoveWeaponFromPed(API.PlayerPedId(), (uint)(int)e.NewWeaponHash);
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.WEAPON_CAN_ONLY_BE_USED_BY_POLICE));
			}
		}
	}
}
