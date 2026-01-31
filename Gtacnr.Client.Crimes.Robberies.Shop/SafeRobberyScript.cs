using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Businesses;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Crimes.Robberies.Shop;

public class SafeRobberyScript : Script
{
	private Blip blip;

	private bool instructionalButtonEnabled;

	private bool instructionalStopButtonEnabled;

	private bool tasksAttached;

	private bool canCrackSafe;

	private bool isRobbingSafe;

	public static SafeRobberyScript Instance { get; private set; }

	public SafeRobberyScript()
	{
		Instance = this;
	}

	public void EnableSafe()
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		if (!tasksAttached)
		{
			base.Update += DrawTask;
			base.Update += UpdateTask;
			tasksAttached = true;
			KeysScript.AttachListener((Control)29, OnKeyEvent, 10);
			blip = World.CreateBlip(ShopRobberyScript.CurrentRobbery.Business.SafeData.Position.XYZ());
			blip.Sprite = (BlipSprite)434;
			blip.Color = (BlipColor)3;
			Utils.SetBlipName(blip, "Safe", "safe");
			blip.Scale = 0.6f;
			blip.IsShortRange = true;
		}
	}

	private void DisableSafe()
	{
		if (tasksAttached)
		{
			base.Update -= DrawTask;
			base.Update -= UpdateTask;
			tasksAttached = false;
			KeysScript.DetachListener((Control)29, OnKeyEvent);
			if (blip != (Blip)null)
			{
				((PoolObject)blip).Delete();
				blip = null;
			}
		}
	}

	private async void StartCrackSafe()
	{
		ShopRobberyScript.CurrentRobbery.SafeCrackStarted = true;
		DisableSafe();
		DisableInstructionalButtons();
		if (ShopRobberyScript.CurrentRobbery == null || ShopRobberyScript.CurrentRobbery.Info.PlayerRobbingSafe != 0 || ShopRobberyScript.CurrentRobbery.Info.IsSafeEmpty || ShopRobberyScript.CurrentRobbery.Info.Players.Count == 1)
		{
			return;
		}
		if (!(await TriggerServerEventAsync<bool>("gtacnr:businesses:robbery:startCrackSafe", new object[1] { ShopRobberyScript.CurrentRobbery.Business.Id })))
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
			return;
		}
		isRobbingSafe = true;
		Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_CRACKING_SAFE));
		Vector4 safePos = ShopRobberyScript.CurrentRobbery.Business.SafeData.Position;
		Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
		Game.PlayerPed.Task.AchieveHeading(safePos.W, 1000);
		await BaseScript.Delay(1000);
		if (!isRobbingSafe)
		{
			return;
		}
		((Entity)Game.PlayerPed).PositionNoOffset = new Vector3(safePos.X, safePos.Y, ((Entity)Game.PlayerPed).Position.Z);
		((Entity)Game.PlayerPed).Heading = safePos.W;
		((Entity)Game.PlayerPed).IsPositionFrozen = true;
		await BaseScript.Delay(100);
		if (isRobbingSafe)
		{
			Game.PlayerPed.Task.PlayAnimation("mini@safe_cracking", "step_into", 4f, 2000, (AnimationFlags)2);
			await BaseScript.Delay(2000);
			if (isRobbingSafe)
			{
				Game.PlayerPed.Task.PlayAnimation("mini@safe_cracking", "dial_turn_clock_normal", 4f, -1, (AnimationFlags)1);
				EnableStopInstructionalButtons();
				KeysScript.AttachListener((Control)29, OnKeyEvent, 10);
			}
		}
	}

	private async void StartStealingSafeMoney()
	{
		if (ShopRobberyScript.CurrentRobbery == null || ShopRobberyScript.CurrentRobbery.Info.PlayerRobbingSafe != Game.Player.ServerId || ShopRobberyScript.CurrentRobbery.Info.IsSafeEmpty)
		{
			return;
		}
		List<Prop> createdProps = new List<Prop>();
		Game.PlayerPed.Task.PlayAnimation("mini@safe_cracking", "dial_turn_succeed_2", 4f, 3000, (AnimationFlags)2);
		await BaseScript.Delay(3000);
		if (!isRobbingSafe)
		{
			return;
		}
		Prop doorProp = World.GetAllProps().FirstOrDefault((Prop p) => ((Entity)p).Model.In((Model[])(object)new Model[3]
		{
			Model.op_Implicit("bkr_prop_biker_safedoor_01a"),
			Model.op_Implicit("v_ilev_gangsafedoor"),
			Model.op_Implicit("pil_prop_fs_safedoor")
		}));
		if ((Entity)(object)doorProp != (Entity)null)
		{
			for (int i = 0; i < 40; i++)
			{
				((Entity)doorProp).Heading = ((Entity)doorProp).Heading + 3f;
				await BaseScript.Delay(10);
			}
			int n = Gtacnr.Utils.GetRandomInt(2, 6);
			for (int i = 0; i < n; i++)
			{
				Vector3 val = ((Entity)Game.PlayerPed).Position + ((Entity)Game.PlayerPed).ForwardVector * 0.6f + ((Entity)Game.PlayerPed).RightVector * 0.08f * (float)((i % 2 == 0) ? i : (-i)) + new Vector3(0f, 0f, -0.8f);
				Prop val2 = await World.CreateProp(new Model("h4_prop_h4_cash_stack_02a"), val, false, false);
				((Entity)val2).Heading = ((Entity)Game.PlayerPed).Heading;
				createdProps.Add(val2);
			}
		}
		Game.PlayerPed.Task.PlayAnimation("mini@safe_cracking", "step_out", 4f, 2000, (AnimationFlags)2);
		await BaseScript.Delay(2000);
		if (!isRobbingSafe)
		{
			return;
		}
		Prop bagProp = await World.CreateProp(new Model("ch_prop_ch_duffelbag_01x"), ((Entity)Game.PlayerPed).Position, false, false);
		createdProps.Add(bagProp);
		AttachBag();
		Game.PlayerPed.Task.PlayAnimation("anim@heists@money_grab@duffel", "enter", 4f, 2500, (AnimationFlags)2);
		await BaseScript.Delay(1500);
		if (!isRobbingSafe)
		{
			return;
		}
		API.DetachEntity(((PoolObject)bagProp).Handle, false, false);
		API.PlaceObjectOnGroundProperly(((PoolObject)bagProp).Handle);
		Prop moneyProp1 = await World.CreateProp(new Model("prop_anim_cash_pile_02"), ((Entity)Game.PlayerPed).Position, false, false);
		Prop val3 = await World.CreateProp(new Model("prop_anim_cash_pile_02"), ((Entity)Game.PlayerPed).Position, false, false);
		createdProps.Add(moneyProp1);
		createdProps.Add(val3);
		API.AttachEntityToEntity(((PoolObject)moneyProp1).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 28422), 0f, 0f, 0f, 0f, 0f, 0f, true, true, false, true, 1, true);
		API.AttachEntityToEntity(((PoolObject)val3).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 60309), 0f, 0f, 0f, 0f, 0f, 0f, true, true, false, true, 1, true);
		AntiEntitySpawnScript.RegisterEntities(createdProps.Cast<Entity>().ToList());
		await BaseScript.Delay(1000);
		Game.PlayerPed.Task.PlayAnimation("anim@heists@money_grab@duffel", "loop", 4f, -1, (AnimationFlags)1);
		while (ShopRobberyScript.CurrentRobbery != null && !ShopRobberyScript.CurrentRobbery.Info.IsSafeEmpty && isRobbingSafe)
		{
			await BaseScript.Delay(500);
		}
		Game.PlayerPed.Task.ClearAll();
		Game.PlayerPed.Task.PlayAnimation("anim@heists@money_grab@duffel", "exit", 4f, 3000, (AnimationFlags)2);
		AttachBag();
		await BaseScript.Delay(3000);
		foreach (Prop item in createdProps)
		{
			((PoolObject)item).Delete();
		}
		OnSafeEnd(playAnim: false);
		void AttachBag()
		{
			API.AttachEntityToEntity(((PoolObject)bagProp).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 57005), 0.33f, -0.055f, -0.015f, 258.5f, -191.5f, -72f, true, true, false, true, 1, true);
		}
	}

	public void EndImmediately()
	{
		DisableSafe();
		OnSafeEnd();
	}

	private async void CancelSafeRobbery()
	{
		if (ShopRobberyScript.CurrentRobbery != null && ShopRobberyScript.CurrentRobbery.Info.PlayerRobbingSafe == Game.Player.ServerId && !ShopRobberyScript.CurrentRobbery.Info.IsSafeEmpty)
		{
			BaseScript.TriggerServerEvent("gtacnr:businesses:robbery:cancelSafeRobbery", new object[1] { ShopRobberyScript.CurrentRobbery.Business.Id });
			OnSafeEnd();
		}
	}

	private async void OnSafeEnd(bool playAnim = true)
	{
		if (isRobbingSafe)
		{
			ShopRobberyScript.CurrentRobbery.SafeCrackStarted = false;
			isRobbingSafe = false;
			DisableStopInstructionalButtons();
			KeysScript.DetachListener((Control)29, OnKeyEvent);
			((Entity)Game.PlayerPed).IsPositionFrozen = false;
			if (playAnim)
			{
				Game.PlayerPed.Task.PlayAnimation("mini@safe_cracking", "step_out", 4f, 2000, (AnimationFlags)2);
				await BaseScript.Delay(2000);
				Game.PlayerPed.Task.ClearAll();
			}
		}
	}

	[EventHandler("gtacnr:businesses:robbery:onStartCrackSafe")]
	private void OnRobberyStartCrackSafe(string businessId, int playerId)
	{
		Business closestBusiness = BusinessScript.ClosestBusiness;
		if (ShopRobberyScript.CurrentRobbery != null && closestBusiness != null && !(closestBusiness.Id != businessId) && playerId != Game.Player.ServerId)
		{
			PlayerState playerState = LatentPlayers.Get(playerId);
			Utils.SendNotification(LocalizationController.S(Entries.Jobs.ROBBERY_PLAYER_CRACKING_SAFE, playerState.FullyFormatted));
			ShopRobberyScript.CurrentRobbery.SafeCrackStarted = true;
			DisableSafe();
		}
	}

	[EventHandler("gtacnr:businesses:robbery:onSafeCracked")]
	private void OnSafeCracked(string businessId, int playerId)
	{
		if (ShopRobberyScript.CurrentRobbery != null && !(businessId != ShopRobberyScript.CurrentRobbery.Business.Id))
		{
			if (playerId == Game.Player.ServerId)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_CRACKED_SAFE));
				StartStealingSafeMoney();
			}
			else
			{
				PlayerState playerState = LatentPlayers.Get(playerId);
				Utils.SendNotification(LocalizationController.S(Entries.Jobs.ROBBERY_PLAYER_SUCCESSFULLY_CRACKING_SAFE, playerState.FullyFormatted));
			}
		}
	}

	[EventHandler("gtacnr:businesses:robbery:onSafeCrackFailed")]
	private async void OnSafeCrackFailed(string businessId, int playerId)
	{
		if (ShopRobberyScript.CurrentRobbery != null && !(businessId != ShopRobberyScript.CurrentRobbery.Business.Id))
		{
			if (playerId == Game.Player.ServerId)
			{
				Game.PlayerPed.Task.PlayAnimation("mini@safe_cracking", "dial_turn_fail_4", 4f, 3000, (AnimationFlags)2);
				await BaseScript.Delay(3000);
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_EXTRA_SECURITY_SAFE));
				OnSafeEnd();
			}
			else
			{
				PlayerState playerState = LatentPlayers.Get(playerId);
				Utils.SendNotification(LocalizationController.S(Entries.Jobs.ROBBERY_PLAYER_FAILED_CRACKING_SAFE, playerState.FullyFormatted));
			}
		}
	}

	[EventHandler("gtacnr:businesses:robbery:onSafeCrackCanceled")]
	private void OnSafeCrackCanceled(string businessId, int playerId)
	{
		if (ShopRobberyScript.CurrentRobbery != null && !(businessId != ShopRobberyScript.CurrentRobbery.Business.Id))
		{
			if (playerId == Game.Player.ServerId)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_CANCELED_SAFE));
				return;
			}
			PlayerState playerState = LatentPlayers.Get(playerId);
			Utils.SendNotification(LocalizationController.S(Entries.Jobs.ROBBERY_PLAYER_CANCELED_CRACKING_SAFE, playerState.FullyFormatted));
		}
	}

	private async Coroutine DrawTask()
	{
		Business closestBusiness = BusinessScript.ClosestBusiness;
		if (ShopRobberyScript.CurrentRobbery != null && closestBusiness != null && closestBusiness.SafeData != null)
		{
			Vector4 position = closestBusiness.SafeData.Position;
			Vector3 val = default(Vector3);
			((Vector3)(ref val))._002Ector(0.5f, 0.5f, 0.4f);
			Color color = Color.FromUint(10483072u);
			float z = 0f;
			if (API.GetGroundZFor_3dCoord(position.X, position.Y, position.Z, ref z, false))
			{
				position.Z = z;
			}
			API.DrawMarker(1, position.X, position.Y, position.Z, 0f, 0f, 0f, 0f, 0f, 0f, val.X, val.Y, val.Z, (int)color.R, (int)color.G, (int)color.B, (int)color.A, false, true, 2, false, (string)null, (string)null, false);
		}
	}

	private async Coroutine UpdateTask()
	{
		await Script.Wait(100);
		Business closestBusiness = BusinessScript.ClosestBusiness;
		if (ShopRobberyScript.CurrentRobbery != null && closestBusiness != null && closestBusiness.SafeData != null)
		{
			SafeRobberyScript safeRobberyScript = this;
			int num;
			if (!ShopRobberyScript.CurrentRobbery.SafeCrackStarted)
			{
				Vector3 val = closestBusiness.SafeData.Position.XYZ();
				num = ((((Vector3)(ref val)).DistanceToSquared(((Entity)Game.PlayerPed).Position) <= 0.36f) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			safeRobberyScript.canCrackSafe = (byte)num != 0;
			if (canCrackSafe)
			{
				EnableInstructionalButtons();
			}
			else
			{
				DisableInstructionalButtons();
			}
		}
	}

	private void EnableStopInstructionalButtons()
	{
		if (!instructionalStopButtonEnabled)
		{
			instructionalStopButtonEnabled = true;
			Utils.AddInstructionalButton("stopSafe", new InstructionalButton(LocalizationController.S(Entries.Main.BTN_HOLD, LocalizationController.S(Entries.Jobs.ROBBERY_INSTRUCTIONAL_STOP)), 2, (Control)29));
		}
	}

	private void DisableStopInstructionalButtons()
	{
		if (instructionalStopButtonEnabled)
		{
			instructionalStopButtonEnabled = false;
			Utils.RemoveInstructionalButton("stopSafe");
		}
	}

	private void EnableInstructionalButtons()
	{
		if (!instructionalButtonEnabled)
		{
			instructionalButtonEnabled = true;
			Utils.AddInstructionalButton("safeCrack", new InstructionalButton(LocalizationController.S(Entries.Jobs.ROBBERY_INSTRUCTIONAL_CRACK), 2, (Control)29));
		}
	}

	private void DisableInstructionalButtons()
	{
		if (instructionalButtonEnabled)
		{
			instructionalButtonEnabled = false;
			Utils.RemoveInstructionalButton("safeCrack");
		}
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)control == 29)
		{
			switch (eventType)
			{
			case KeyEventType.JustPressed:
				if (!canCrackSafe)
				{
					return false;
				}
				StartCrackSafe();
				return true;
			case KeyEventType.Held:
				if (!isRobbingSafe)
				{
					return false;
				}
				CancelSafeRobbery();
				return true;
			}
		}
		return false;
	}
}
