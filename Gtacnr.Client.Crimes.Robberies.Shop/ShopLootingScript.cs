using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Businesses;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Crimes.Robberies.Shop;

public class ShopLootingScript : Script
{
	private bool instructionalButtonEnabled;

	private bool tasksAttached;

	private bool canLoot;

	private bool isLooting;

	private int currentLootIdx;

	private List<int> disabledIndices = new List<int>();

	public static ShopLootingScript Instance { get; private set; }

	public ShopLootingScript()
	{
		Instance = this;
		ShopRobberyScript.RobberyJoined += OnRobberyJoined;
		ShopRobberyScript.RobberyEnded += OnRobberyEnded;
	}

	private void OnRobberyJoined(object sender, EventArgs e)
	{
		EnableLooting();
	}

	private void OnRobberyEnded(object sender, EventArgs e)
	{
		DisableLooting();
	}

	public void EnableLooting()
	{
		if (!tasksAttached)
		{
			base.Update += DrawTask;
			base.Update += UpdateTask;
			tasksAttached = true;
			KeysScript.AttachListener((Control)29, OnKeyEvent, 10);
			Reset();
		}
	}

	private void DisableLooting()
	{
		if (tasksAttached)
		{
			base.Update -= DrawTask;
			base.Update -= UpdateTask;
			tasksAttached = false;
			KeysScript.DetachListener((Control)29, OnKeyEvent);
		}
		DisableInstructionalButtons();
		Reset();
	}

	private void DisableLootingPosition(int index)
	{
		if (!disabledIndices.Contains(index))
		{
			disabledIndices.Add(index);
		}
	}

	private void Reset()
	{
		currentLootIdx = -1;
		disabledIndices.Clear();
	}

	private async void Loot(int lootIdx)
	{
		if (isLooting || currentLootIdx < 0 || ShopRobberyScript.CurrentRobbery == null)
		{
			return;
		}
		try
		{
			isLooting = true;
			DisableLootingPosition(lootIdx);
			Vector4 lootPos = ShopRobberyScript.CurrentRobbery.Business.RobberyLootCoords[lootIdx];
			Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
			Game.PlayerPed.Task.AchieveHeading(lootPos.W, 600);
			await BaseScript.Delay(600);
			ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:businesses:robbery:loot", ShopRobberyScript.CurrentRobbery.Business.Id, lootIdx);
			switch (responseCode)
			{
			case ResponseCode.NoSpaceLeft:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_LOOTING_NO_SPACE_LEFT));
				break;
			default:
				Utils.DisplayError(responseCode, "", "Loot");
				break;
			case ResponseCode.Success:
				((Entity)Game.PlayerPed).PositionNoOffset = new Vector3(lootPos.X, lootPos.Y, ((Entity)Game.PlayerPed).Position.Z);
				((Entity)Game.PlayerPed).Heading = lootPos.W;
				((Entity)Game.PlayerPed).IsPositionFrozen = true;
				Game.PlayerPed.Task.PlayAnimation("anim@heists@narcotics@trash", "pickup", 4f, 1000, (AnimationFlags)2);
				await BaseScript.Delay(1000);
				Game.PlayerPed.Task.ClearAll();
				((Entity)Game.PlayerPed).IsPositionFrozen = false;
				break;
			}
		}
		finally
		{
			isLooting = false;
		}
	}

	[EventHandler("gtacnr:businesses:robbery:onPlayerLoot")]
	private void OnPlayerLoot(string businessId, int playerId, int index)
	{
		if (ShopRobberyScript.CurrentRobbery != null && BusinessScript.ClosestBusiness != null && !(BusinessScript.ClosestBusiness.Id != businessId) && playerId != Game.Player.ServerId)
		{
			DisableLootingPosition(index);
		}
	}

	private async Coroutine DrawTask()
	{
		Business closestBusiness = BusinessScript.ClosestBusiness;
		if (ShopRobberyScript.CurrentRobbery == null || closestBusiness == null || closestBusiness.RobberyLootCoords == null)
		{
			return;
		}
		Vector3 val2 = default(Vector3);
		for (int i = 0; i < closestBusiness.RobberyLootCoords.Count; i++)
		{
			if (!disabledIndices.Contains(i))
			{
				Vector3 val = closestBusiness.RobberyLootCoords[i].XYZ();
				((Vector3)(ref val2))._002Ector(0.4f, 0.4f, 0.35f);
				Color color = Color.FromUint(4125163648u);
				float z = 0f;
				if (API.GetGroundZFor_3dCoord(val.X, val.Y, val.Z, ref z, false))
				{
					val.Z = z;
				}
				API.DrawMarker(1, val.X, val.Y, val.Z, 0f, 0f, 0f, 0f, 0f, 0f, val2.X, val2.Y, val2.Z, (int)color.R, (int)color.G, (int)color.B, (int)color.A, false, true, 2, false, (string)null, (string)null, false);
			}
		}
	}

	private async Coroutine UpdateTask()
	{
		await Script.Wait(100);
		try
		{
			canLoot = false;
			currentLootIdx = -1;
			if (ShopRobberyScript.CurrentRobbery == null || BusinessScript.ClosestBusiness == null || BusinessScript.ClosestBusiness.RobberyLootCoords == null)
			{
				return;
			}
			for (int i = 0; i < BusinessScript.ClosestBusiness.RobberyLootCoords.Count; i++)
			{
				if (!disabledIndices.Contains(i))
				{
					Vector3 val = BusinessScript.ClosestBusiness.RobberyLootCoords[i].XYZ();
					if (((Vector3)(ref val)).DistanceToSquared(((Entity)Game.PlayerPed).Position) <= 0.6f.Square())
					{
						canLoot = true;
						currentLootIdx = i;
						break;
					}
				}
			}
		}
		finally
		{
			if (canLoot)
			{
				EnableInstructionalButtons();
			}
			else
			{
				DisableInstructionalButtons();
			}
		}
	}

	private void EnableInstructionalButtons()
	{
		if (!instructionalButtonEnabled)
		{
			instructionalButtonEnabled = true;
			Utils.AddInstructionalButton("loot", new InstructionalButton(LocalizationController.S(Entries.Jobs.ROBBERY_INSTRUCTIONAL_LOOT), 2, (Control)29));
		}
	}

	private void DisableInstructionalButtons()
	{
		if (instructionalButtonEnabled)
		{
			instructionalButtonEnabled = false;
			Utils.RemoveInstructionalButton("loot");
		}
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)control == 29 && eventType == KeyEventType.JustPressed)
		{
			if (!canLoot)
			{
				return false;
			}
			Loot(currentLootIdx);
			return true;
		}
		return false;
	}
}
