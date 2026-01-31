using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Communication;

public class SignScript : Script
{
	private DateTime setTime;

	private Vector3 setCoords;

	private bool taskAttached;

	private Prop signProp;

	private const string animDict = "mp_player_int_uppergang_sign_a";

	private const string animName = "mp_player_int_gang_sign_a";

	protected override void OnStarted()
	{
		Chat.AddSuggestion("/sign", "Shows custom text over your head to nearby players. If you move too far away the sign will be deleted.", new ChatParamSuggestion("text", "The text to show on your sign. Leave blank to delete the sign.", isOptional: true));
	}

	[Command("sign")]
	private async void SignCommand(string[] args)
	{
		if (args.Length == 0)
		{
			RemoveSign();
			return;
		}
		string sign = string.Join(" ", args).Trim();
		await SetSign(sign);
	}

	private async Task SetSign(string text)
	{
		if (text == null)
		{
			text = string.Empty;
		}
		if (!Gtacnr.Utils.CheckTimePassed(setTime, TimeSpan.FromMinutes(1.0)) && text != string.Empty)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "You must wait before setting your sign again.");
			return;
		}
		if (Game.PlayerPed.IsInVehicle() && text != string.Empty)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "You can only use this command when on foot.");
			return;
		}
		if (((Entity)Game.PlayerPed).IsDead && text != string.Empty)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, Gtacnr.Utils.RemoveGta5TextFormatting(LocalizationController.S(Entries.Player.CANT_DO_WHEN_DEAD)));
			return;
		}
		if (text.Length > 100)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, $"The maximum length is {100} characters.");
			return;
		}
		text = text.Replace("~", "&tilde;").Replace("<", "&lt;").Replace(">", "&rt;")
			.CnRChatToGTAUI();
		if (await TriggerServerEventAsync("gtacnr:setSign", text) != ResponseCode.Success)
		{
			return;
		}
		if (text != string.Empty)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Info, "Custom sign set to: {white}" + text.GTAUIToCnRChat());
			setTime = DateTime.UtcNow;
			setCoords = ((Entity)Game.PlayerPed).Position;
			Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
			if ((Entity)(object)signProp == (Entity)null || !signProp.Exists())
			{
				signProp = await World.CreateProp(Model.op_Implicit("prop_cs_protest_sign_02"), ((Entity)Game.PlayerPed).Position, false, false);
				AntiEntitySpawnScript.RegisterEntity((Entity)(object)signProp);
			}
			API.AttachEntityToEntity(((PoolObject)signProp).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 57005), 0.25f, 0.4f, 0.15f, -2.6f, 57.7f, 107.7f, true, true, false, true, 1, true);
			if (!API.IsEntityPlayingAnim(((PoolObject)Game.PlayerPed).Handle, "mp_player_int_uppergang_sign_a", "mp_player_int_gang_sign_a", 3))
			{
				await Game.PlayerPed.Task.PlayAnimation("mp_player_int_uppergang_sign_a", "mp_player_int_gang_sign_a", 4f, -4f, -1, (AnimationFlags)51, 0f);
			}
			await BaseScript.Delay(200);
			if (!taskAttached)
			{
				taskAttached = true;
				base.Update += DeleteSignTask;
			}
		}
		else
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Info, "Your custom sign has been reset.");
			if (taskAttached)
			{
				taskAttached = false;
				base.Update -= DeleteSignTask;
			}
			if ((Entity)(object)signProp != (Entity)null)
			{
				((Entity)signProp).Detach();
				((Entity)signProp).MarkAsNoLongerNeeded();
			}
			Game.PlayerPed.Task.ClearAnimation("mp_player_int_uppergang_sign_a", "mp_player_int_gang_sign_a");
		}
	}

	private async Coroutine DeleteSignTask()
	{
		if (!taskAttached)
		{
			return;
		}
		if (API.IsEntityPlayingAnim(((PoolObject)Game.PlayerPed).Handle, "mp_player_int_uppergang_sign_a", "mp_player_int_gang_sign_a", 3) && !Game.PlayerPed.IsInCombat && !Game.PlayerPed.IsInMeleeCombat && !Game.PlayerPed.IsJumping && !Game.PlayerPed.IsSwimming && !((Entity)(object)Game.PlayerPed.VehicleTryingToEnter != (Entity)null) && (int)Weapon.op_Implicit(Game.PlayerPed.Weapons.Current) == -1569615261)
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			if (!(((Vector3)(ref position)).DistanceToSquared(setCoords) > 20f.Square()))
			{
				goto IL_0175;
			}
		}
		await RemoveSign();
		goto IL_0175;
		IL_0175:
		await BaseScript.Delay(50);
	}

	private async Task RemoveSign()
	{
		await SetSign(string.Empty);
	}
}
