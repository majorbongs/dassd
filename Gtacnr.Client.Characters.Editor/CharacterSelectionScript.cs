using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Premium;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Newtonsoft.Json;

namespace Gtacnr.Client.Characters.Editor;

public class CharacterSelectionScript : Script
{
	private bool isCharacterHudOpen;

	private static readonly double pressCooldownLong = 250.0;

	private static readonly double pressCooldownShort = 100.0;

	private Dictionary<int, DateTime> lastHoldingTimestamp = new Dictionary<int, DateTime>();

	private Dictionary<int, int> holdingTicks = new Dictionary<int, int>();

	private Character? selectedCharacter;

	private Random random = new Random();

	private List<dynamic> frontendControls = new List<object>
	{
		new
		{
			Control = (Control)188,
			Sound = "NAV_UP_DOWN",
			SoundLibrary = "HUD_FRONTEND_DEFAULT_SOUNDSET",
			NuiParam = "frontendUp",
			HoldToRepeat = true
		},
		new
		{
			Control = (Control)187,
			Sound = "NAV_UP_DOWN",
			SoundLibrary = "HUD_FRONTEND_DEFAULT_SOUNDSET",
			NuiParam = "frontendDown",
			HoldToRepeat = true
		},
		new
		{
			Control = (Control)201,
			Sound = "",
			SoundLibrary = "",
			NuiParam = "frontendAccept",
			HoldToRepeat = false
		},
		new
		{
			Control = (Control)204,
			Sound = "",
			SoundLibrary = "",
			NuiParam = "frontendY",
			HoldToRepeat = false
		}
	};

	protected override void OnStarted()
	{
		API.RegisterNuiCallbackType("onSelectCharacter");
		API.RegisterNuiCallbackType("onCreateCharacter");
		API.RegisterNuiCallbackType("onHighlight");
	}

	private async Coroutine MainTask()
	{
		if (!isCharacterHudOpen)
		{
			return;
		}
		API.SetPauseMenuActive(false);
		foreach (dynamic frontendControl in frontendControls)
		{
			int key = (int)frontendControl.Control;
			if (Game.IsControlPressed(2, frontendControl.Control))
			{
				if (!lastHoldingTimestamp.ContainsKey(key))
				{
					lastHoldingTimestamp[key] = DateTime.MinValue;
				}
				if (!holdingTicks.ContainsKey(key))
				{
					holdingTicks[key] = 0;
				}
				if (!((!frontendControl.HoldToRepeat && holdingTicks[key] > 0) ? true : false))
				{
					if (Gtacnr.Utils.CheckTimePassed(lastHoldingTimestamp[key], (holdingTicks[key] > 2) ? pressCooldownShort : pressCooldownLong))
					{
						holdingTicks[key]++;
						lastHoldingTimestamp[key] = DateTime.UtcNow;
						Game.PlaySound(frontendControl.Sound, frontendControl.SoundLibrary);
						API.SendNuiMessage(JsonConvert.SerializeObject(new
						{
							type = "control",
							control = (object)frontendControl.NuiParam
						}));
					}
					CreateMenuInstructionalButtons();
				}
				break;
			}
			holdingTicks[key] = 0;
			lastHoldingTimestamp[key] = DateTime.MinValue;
		}
	}

	private void AttachTicks()
	{
		base.Update += MainTask;
	}

	private void DetachTicks()
	{
		base.Update -= MainTask;
	}

	protected override void OnStopping()
	{
		HideCharacterSelectionHud();
	}

	[EventHandler("__cfx_nui:onSelectCharacter")]
	private void OnSelectCharacter(IDictionary<string, object> data)
	{
		if (isCharacterHudOpen)
		{
			HideCharacterSelectionHud();
			Game.PlaySound("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
			SelectCharacter((int)data["index"]);
		}
	}

	[EventHandler("__cfx_nui:onCreateCharacter")]
	private void OnCreateCharacter(IDictionary<string, object> data)
	{
		Utils.PlayErrorSound();
	}

	[EventHandler("__cfx_nui:onHighlight")]
	private void OnHighlight(IDictionary<string, object> data)
	{
		Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
	}

	private void CreateMenuInstructionalButtons()
	{
		Utils.AddInstructionalButton("charUse", new InstructionalButton("Use", 13, 201));
		Utils.AddInstructionalButton("charNew", new InstructionalButton("Create new", 13, 204));
	}

	[EventHandler("gtacnr:characters:showSelectionHud")]
	private async void ShowCharacterSelectionHud()
	{
		if (!isCharacterHudOpen)
		{
			isCharacterHudOpen = true;
			AttachTicks();
			MainScript.SetDefaultCamera();
			API.DisplayHud(false);
			API.DisplayRadar(false);
			LoadingPrompt.Hide();
			if (API.IsScreenFadedOut())
			{
				await Utils.FadeIn();
			}
			CreateMenuInstructionalButtons();
			Utils.Blur();
			Game.PlaySound("Hit_In", "PLAYER_SWITCH_CUSTOM_SOUNDSET");
			string text = JsonConvert.SerializeObject(new
			{
				type = "showCharacterSelection",
				characters = MainScript.Characters
			});
			Print($"{MainScript.Characters.Count()} characters.");
			API.SendNuiMessage(text);
			API.SetNuiFocus(true, true);
			API.SetNuiFocusKeepInput(true);
			await BaseScript.Delay(500);
		}
	}

	[EventHandler("gtacnr:characters:hideSelectionHud")]
	private void HideCharacterSelectionHud()
	{
		if (isCharacterHudOpen)
		{
			isCharacterHudOpen = false;
			DetachTicks();
			Utils.RemoveInstructionalButton("charUse");
			Utils.RemoveInstructionalButton("charNew");
			API.SendNuiMessage(JsonConvert.SerializeObject(new
			{
				type = "hideCharacterSelection"
			}));
			API.SetNuiFocus(false, false);
			Game.PlaySound("Hit_Out", "PLAYER_SWITCH_CUSTOM_SOUNDSET");
			Utils.Unblur();
			API.DisplayHud(true);
			API.DisplayRadar(true);
		}
	}

	[EventHandler("gtacnr:characters:selectCharacter")]
	private async void SelectCharacter(int slot)
	{
		try
		{
			selectedCharacter = MainScript.Characters.ElementAt(slot);
			if (selectedCharacter.SyncResult == 99)
			{
				LoadingPrompt.Hide();
				LoadingPrompt.Show("Syncing character", (LoadingSpinnerType)5);
				do
				{
					Print("Character is still syncing...");
					await BaseScript.Delay(10000);
					selectedCharacter = await Gtacnr.Client.API.Characters.Get(slot);
				}
				while (selectedCharacter.SyncResult == 99);
			}
			if (selectedCharacter.SyncResult > 1)
			{
				Print($"^1Character sync failed (0xAA-{selectedCharacter.SyncResult})");
				if (!(await Utils.ShowConfirm($"This ~b~character ~s~has not been synced correctly (error ~y~0xAA-{selectedCharacter.SyncResult}~s~). " + "Do you want to retry?.", "Sync failed", TimeSpan.FromSeconds(0.0), 576, 512)))
				{
					API.RestartGame();
					return;
				}
				BaseScript.TriggerServerEvent("gtacnr:characters:sync", new object[0]);
				await BaseScript.Delay(5000);
				selectedCharacter.SyncResult = 99;
				BaseScript.TriggerEvent("gtacnr:characters:selectCharacter", new object[1] { 0 });
				BaseScript.TriggerServerEvent("gtacnr:characters:characterSelected", new object[1] { 0 });
				return;
			}
			LoadingPrompt.Show("Applying character", (LoadingSpinnerType)5);
			if (!API.IsScreenFadedOut())
			{
				await Utils.FadeOut();
			}
			if (!(await ApplyCharacter(selectedCharacter)))
			{
				await Utils.FadeIn(1);
				await Utils.ShowAlert("A fatal error has occurred. Please, try again.", "Error");
				API.RestartGame();
				return;
			}
			Gtacnr.Client.API.Characters.SetActiveCharacter(slot);
			MainScript.SelectedCharacter = selectedCharacter;
			MainScript.DestroyDefaultCamera();
			Utils.Unfreeze();
			LoadingPrompt.Hide();
			BaseScript.TriggerEvent("gtacnr:spawn", new object[1] { (object)default(Vector4) });
		}
		catch (Exception exception)
		{
			Print(exception);
			await Utils.FadeIn(1);
			await Utils.ShowAlert("A fatal error has occurred. Please, try again.", "Error");
			API.RestartGame();
		}
	}

	private async Task<bool> ApplyCharacter(Character character)
	{
		int modelHash = ((character.Sex == Sex.Male) ? Utils.FreemodeMale : Utils.FreemodeFemale);
		try
		{
			Print($"^5Loading player model ({modelHash:X})...^0");
			using DisposableModel pedModel = new DisposableModel(Model.op_Implicit(modelHash))
			{
				TimeOut = TimeSpan.FromSeconds(120.0)
			};
			await pedModel.Load();
			await Game.Player.ChangeModel(pedModel.Model);
			Print($"^2Player model has been loaded successfully ({modelHash:X})^0");
		}
		catch (Exception exception)
		{
			Debug.WriteLine($"^1Fatal error: ^0Unable to load player model ({modelHash:X})!");
			Print(exception);
		}
		Utils.ApplyAppearance(Game.PlayerPed, character.Appearance);
		LoadingPrompt.Show("Loading clothing", (LoadingSpinnerType)5);
		await CustomScript.LoadCustom();
		Clothes.CurrentApparel = character.Apparel ?? Apparel.GetDefault(character.Job, character.Sex);
		Clothes.CurrentApparel.Remove(CustomScript.GetUnauthorizedClothingIds());
		return true;
	}

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		if (selectedCharacter != null)
		{
			if (selectedCharacter.Health <= 100)
			{
				selectedCharacter.Health = 400;
			}
			API.SetEntityHealth(((PoolObject)Game.PlayerPed).Handle, selectedCharacter.Health);
			API.SetPedArmour(((PoolObject)Game.PlayerPed).Handle, selectedCharacter.Armor);
			AntiHealthLockScript.Initialize(((Entity)Game.PlayerPed).Health, Game.PlayerPed.Armor);
		}
	}
}
