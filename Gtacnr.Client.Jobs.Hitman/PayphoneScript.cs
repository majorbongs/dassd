using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Localization;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Hitman;

public class PayphoneScript : Script
{
	private static readonly HashSet<uint> payphoneHashes = new string[10] { "ch_chint02_phonebox001", "hei_prop_carrier_phone_02", "prop_phonebox_01a", "prop_phonebox_01b", "prop_phonebox_01c", "prop_phonebox_02", "prop_phonebox_03", "prop_phonebox_04", "p_phonebox_01b_s", "p_phonebox_02_s" }.Select((string s) => (uint)API.GetHashKey(s)).ToHashSet();

	private bool canUse;

	private Prop? closestPayphoneProp;

	private bool isBusy;

	private DateTime lastUsedT = DateTime.MinValue;

	private bool payphoneActionAttached;

	public PayphoneScript()
	{
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	private async void UsePayphone()
	{
		if (isBusy || (Entity)(object)closestPayphoneProp == (Entity)null)
		{
			return;
		}
		isBusy = true;
		try
		{
			if (HitmanScript.CurrentTarget == 0)
			{
				DispatchScript.HitmanDispatch.CallsMenu.OpenMenu();
				Menu.MenuClosedEvent continueAction = null;
				continueAction = delegate
				{
					if (payphoneActionAttached)
					{
						DispatchScript.HitmanDispatch.CallsMenu.OnMenuClose -= continueAction;
						payphoneActionAttached = false;
					}
					if (HitmanScript.CurrentTarget != 0)
					{
						UsePayphone();
					}
				};
				if (!payphoneActionAttached)
				{
					DispatchScript.HitmanDispatch.CallsMenu.OnMenuClose += continueAction;
					payphoneActionAttached = true;
				}
			}
			else if (!Gtacnr.Utils.CheckTimePassed(lastUsedT, HitmanScript.TARGET_POSITION_UPDATE_DELAY))
			{
				TimeSpan timeSpan = HitmanScript.TARGET_POSITION_UPDATE_DELAY - (DateTime.UtcNow - lastUsedT);
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.HITMAN_PAYPHONE_COOLDOWN, timeSpan.TotalSeconds.ToInt()));
				Utils.PlayErrorSound();
			}
			else
			{
				Game.PlayerPed.Task.ClearAllImmediately();
				API.TaskTurnPedToFaceEntity(API.PlayerPedId(), ((PoolObject)closestPayphoneProp).Handle, 1000);
				await BaseScript.Delay(1000);
				API.TaskStartScenarioInPlace(API.PlayerPedId(), "PROP_HUMAN_ATM", 0, true);
				await BaseScript.Delay(5000);
				lastUsedT = DateTime.UtcNow;
				HitmanScript.ShowNearbyTargetPosition();
				Game.PlayerPed.Task.ClearAll();
				await BaseScript.Delay(4000);
				Game.PlayerPed.Task.ClearAllImmediately();
			}
		}
		finally
		{
			isBusy = false;
		}
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		if (e.PreviousJobEnum == JobsEnum.Hitman)
		{
			base.Update -= FindTick;
			DisableControls();
			Prop? obj = closestPayphoneProp;
			if (obj != null)
			{
				Blip attachedBlip = ((Entity)obj).AttachedBlip;
				if (attachedBlip != null)
				{
					((PoolObject)attachedBlip).Delete();
				}
			}
		}
		if (e.CurrentJobEnum == JobsEnum.Hitman)
		{
			base.Update += FindTick;
		}
	}

	private async Coroutine FindTick()
	{
		await BaseScript.Delay(500);
		bool flag = canUse;
		Prop val = closestPayphoneProp;
		closestPayphoneProp = null;
		canUse = false;
		float num = 10000f;
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		Prop[] allProps = World.GetAllProps();
		foreach (Prop val2 in allProps)
		{
			if (payphoneHashes.Contains((uint)((Entity)val2).Model.Hash) && !(Math.Abs(position.Z - ((Entity)val2).Position.Z) > 5f))
			{
				float num2 = ((Vector3)(ref position)).DistanceToSquared(((Entity)val2).Position);
				if (num2 < num)
				{
					closestPayphoneProp = val2;
					num = num2;
					canUse = num2 < 2f;
				}
			}
		}
		if ((Entity)(object)val != (Entity)(object)closestPayphoneProp)
		{
			if (val != null)
			{
				Blip attachedBlip = ((Entity)val).AttachedBlip;
				if (attachedBlip != null)
				{
					((PoolObject)attachedBlip).Delete();
				}
			}
			if ((Entity)(object)closestPayphoneProp != (Entity)null && ((Entity)closestPayphoneProp).AttachedBlip == (Blip)null)
			{
				Blip obj = ((Entity)closestPayphoneProp).AttachBlip();
				obj.Sprite = (BlipSprite)817;
				obj.Color = (BlipColor)1;
				obj.Scale = 0.7f;
				Utils.SetBlipName(obj, "Payphone", "payphone");
				obj.IsShortRange = true;
			}
		}
		if (!flag && canUse)
		{
			EnableControls();
		}
		else if (flag && !canUse)
		{
			DisableControls();
		}
	}

	private void EnableControls()
	{
		Utils.AddInstructionalButton("payhoneUse", new InstructionalButton(LocalizationController.S(Entries.Main.BTN_USE), 2, 51));
		KeysScript.AttachListener((Control)51, OnKeyEvent, 10);
	}

	private void DisableControls()
	{
		Utils.RemoveInstructionalButton("payhoneUse");
		KeysScript.DetachListener((Control)51, OnKeyEvent);
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)control == 51 && eventType == KeyEventType.JustPressed)
		{
			UsePayphone();
			return true;
		}
		return false;
	}
}
