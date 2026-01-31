using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Gtacnr.Client.Admin;
using Gtacnr.Client.API;
using Gtacnr.Client.Vehicles;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Communication;

public class RadioScript : Script
{
	private static RadioScript instance;

	private static readonly Dictionary<int, string> powerStates = new Dictionary<int, string>
	{
		{
			0,
			LocalizationController.S(Entries.Businesses.RADIO_OFF)
		},
		{
			1,
			LocalizationController.S(Entries.Businesses.RADIO_ON)
		}
	};

	private static readonly Dictionary<int, string> volumes = new Dictionary<int, string>
	{
		{
			0,
			LocalizationController.S(Entries.Businesses.RADIO_MUTE)
		},
		{ 10, "~o~10%" },
		{ 25, "~o~25%" },
		{ 50, "~g~50%" },
		{ 75, "~g~75%" },
		{ 100, "~g~100%" }
	};

	public static readonly Menu MainMenu = new Menu(LocalizationController.S(Entries.Businesses.MENU_RADIO_TITLE), LocalizationController.S(Entries.Businesses.MENU_RADIO_TITLE))
	{
		PlaySelectSound = false
	};

	public static readonly MenuListItem PowerMenuItem = new MenuListItem(LocalizationController.S(Entries.Businesses.MENU_RADIO_POWER), powerStates.Values.ToList());

	public static readonly MenuListItem ChannelMenuItem = new MenuListItem(LocalizationController.S(Entries.Businesses.MENU_RADIO_CHANNEL), new List<string> { LocalizationController.S(Entries.Businesses.RADIO_CHANNEL_NONE) });

	public static readonly MenuListItem VolumeMenuItem = new MenuListItem(LocalizationController.S(Entries.Businesses.MENU_RADIO_VOLUME), volumes.Values.ToList(), 3);

	private List<RadioChannel> channels = Gtacnr.Utils.LoadJson<List<RadioChannel>>("data/radio/channels.json");

	private List<RadioChannel> availableChannels = new List<RadioChannel>();

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private Vehicle lastVeh;

	private bool pilotRadioInfoShown;

	private DateTime lastClickT;

	private int clickCount;

	private DateTime timeoutT = DateTime.MinValue;

	public static bool IsRadioOn { get; private set; }

	public static RadioChannel RadioChannel { get; private set; }

	public static IEnumerable<RadioChannel> AvailableChannels => instance.availableChannels;

	public RadioScript()
	{
		instance = this;
		MainMenu.AddMenuItem(PowerMenuItem);
		MainMenu.AddMenuItem(ChannelMenuItem);
		MainMenu.AddMenuItem(VolumeMenuItem);
		MainMenu.OnListIndexChange += OnListIndexChange;
		MenuController.AddMenu(MainMenu);
		((dynamic)((BaseScript)this).Exports["pma-voice"]).setVoiceProperty("micClicks", Preferences.RadioClicksEnabled.Get());
		((dynamic)((BaseScript)this).Exports["pma-voice"]).setVoiceProperty("radioEnabled", false);
		((dynamic)((BaseScript)this).Exports["pma-voice"]).setRadioVolume(25);
		Chat.AddSuggestion("/radio", LocalizationController.S(Entries.Businesses.RADIO_SUGGESTION));
		Chat.AddSuggestion("!", LocalizationController.S(Entries.Businesses.RADIO_TEXT_SUGGESTION), new ChatParamSuggestion(LocalizationController.S(Entries.Businesses.RADIO_TEXT_SUGGESTION_MESSAGE), LocalizationController.S(Entries.Businesses.RADIO_TEXT_SUGGESTION_MESSAGE)));
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
		VehicleEvents.LeftVehicle += OnLeftVehicle;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	public static async void RefreshChannels()
	{
		MenuListItem listItem = ChannelMenuItem;
		instance.availableChannels.Clear();
		int levelByXP = Gtacnr.Utils.GetLevelByXP(await Users.GetXP());
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		StaffLevel staffLevel = StaffLevelScript.StaffLevel;
		foreach (RadioChannel channel in instance.channels)
		{
			if ((int)staffLevel >= (int)channel.StaffLevel && ((int)staffLevel >= 100 || ((channel.Jobs.Count <= 0 || channel.Jobs.Contains(Gtacnr.Client.API.Jobs.CachedJob)) && (channel.RequiredLevel <= 0 || levelByXP >= channel.RequiredLevel) && (!channel.IsAviation || (CanUseAviationFrequencies(currentVehicle) && !channel.RequiresController)))))
			{
				instance.availableChannels.Add(channel);
			}
		}
		listItem.ListItems = instance.availableChannels.Select((RadioChannel c) => (c.Tag != null) ? (c.Tag + " - " + c.DisplayName) : c.DisplayName).ToList();
	}

	private static bool CanUseAviationFrequencies(Vehicle vehicle)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		if ((Entity)(object)vehicle != (Entity)null && ((int)vehicle.ClassType == 16 || (int)vehicle.ClassType == 15))
		{
			if (!((Entity)(object)vehicle.Driver == (Entity)(object)Game.PlayerPed))
			{
				return (Entity)(object)vehicle.GetPedOnSeat((VehicleSeat)0) == (Entity)(object)Game.PlayerPed;
			}
			return true;
		}
		return false;
	}

	public static void OpenRadioMenu()
	{
		if (instance.availableChannels.Count == 0)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Info, LocalizationController.S(Entries.Businesses.RADIO_NOT_AVAILABLE));
			return;
		}
		instance.UpdateChannelDescription();
		MainMenu.OpenMenu();
	}

	public static void ToggleRadio(bool on)
	{
		instance.ToggleRadioInternal(on, refreshMenuSelectedIndex: true);
	}

	public static void SetChannel(float frequency)
	{
		instance.SetChannelInternal(frequency, refreshMenuSelectedIndex: true);
	}

	public static void SetVolume(float volume)
	{
		instance.SetVolumeInternal(volume);
	}

	public static void AddChannel(RadioChannel channel)
	{
		if (!instance.channels.Contains(channel))
		{
			instance.channels.Add(channel);
		}
	}

	public static void RemoveChannel(RadioChannel channel)
	{
		instance.channels.Remove(channel);
	}

	private async void ToggleRadioInternal(bool on, bool refreshMenuSelectedIndex = false)
	{
		IsRadioOn = on;
		BaseScript.TriggerServerEvent("gtacnr:radio:toggle", new object[1] { on });
		((dynamic)((BaseScript)this).Exports["pma-voice"]).setVoiceProperty("radioEnabled", on);
		if (!on || RadioChannel == null)
		{
			((dynamic)((BaseScript)this).Exports["pma-voice"]).removePlayerFromRadio();
		}
		else
		{
			((dynamic)((BaseScript)this).Exports["pma-voice"]).setRadioChannel(RadioChannel.Frequency);
			BaseScript.TriggerServerEvent("gtacnr:radio:tunedIn", new object[1] { RadioChannel.Frequency });
		}
		Print("Radio " + (on ? "^2ON" : "^1OFF"));
		if (refreshMenuSelectedIndex)
		{
			PowerMenuItem.ListIndex = (on ? 1 : 0);
		}
		ChannelMenuItem.Enabled = on;
		VolumeMenuItem.Enabled = on;
	}

	private void SetChannelInternal(float frequency, bool refreshMenuSelectedIndex = false)
	{
		RadioChannel radioChannel = channels.FirstOrDefault((RadioChannel c) => c.Frequency == frequency);
		if (radioChannel == null)
		{
			Print($"^1Unable to tune in to {frequency:0.000}MHz because it is not a defined radio frequency.");
			return;
		}
		RadioChannel = radioChannel;
		BaseScript.TriggerServerEvent("gtacnr:radio:tunedIn", new object[1] { frequency });
		if (!IsRadioOn || !(frequency >= 0f))
		{
			((dynamic)((BaseScript)this).Exports["pma-voice"]).removePlayerFromRadio();
		}
		else
		{
			((dynamic)((BaseScript)this).Exports["pma-voice"]).setRadioChannel(frequency);
			Print($"Radio channel: ^4{frequency:0.000}MHz ^0({RadioChannel.DisplayName})");
		}
		if (refreshMenuSelectedIndex)
		{
			MenuListItem channelMenuItem = ChannelMenuItem;
			int num = -1;
			foreach (RadioChannel availableChannel in availableChannels)
			{
				num++;
				if (availableChannel.Frequency == frequency)
				{
					break;
				}
			}
			if (num > -1)
			{
				channelMenuItem.ListIndex = num;
			}
		}
		UpdateChannelDescription();
	}

	private void SetVolumeInternal(float volume)
	{
		((dynamic)((BaseScript)this).Exports["pma-voice"]).setRadioVolume(volume);
	}

	[Command("radio")]
	private void OnRadioCmd()
	{
		OpenRadioMenu();
		MainMenu.ParentMenu = null;
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		RefreshChannels();
		if ((int)StaffLevelScript.StaffLevel <= 0)
		{
			Job jobData = Gtacnr.Data.Jobs.GetJobData(e.CurrentJobId);
			if (jobData != null && jobData.DefaultRadio > 0f)
			{
				ToggleRadio(on: true);
				SetChannel(jobData.DefaultRadio);
			}
			else if (!CanUseAviationFrequencies(Game.PlayerPed.CurrentVehicle))
			{
				ToggleRadio(on: false);
			}
		}
	}

	[EventHandler("gtacnr:spawned")]
	private void OnSpawned()
	{
		if ((int)StaffLevelScript.StaffLevel > 0)
		{
			RadioChannel radioChannel = channels.FirstOrDefault((RadioChannel c) => c.StaffLevel == (StaffLevel)1);
			if (radioChannel != null)
			{
				ToggleRadio(on: true);
				SetChannel(radioChannel.Frequency);
			}
		}
	}

	[EventHandler("pma-voice:radioActive")]
	private void OnStoppedTalkingOnRadio(bool started)
	{
		if (started)
		{
			return;
		}
		if (!Gtacnr.Utils.CheckTimePassed(lastClickT, 1000.0))
		{
			clickCount++;
			if (clickCount >= 5)
			{
				clickCount = 0;
				lastClickT = DateTime.MinValue;
				Utils.DisplayHelpText("~r~Spamming ~s~the PTT key will cause you to be ~r~timed out ~s~ from the ~b~radio~s~.");
				BaseScript.TriggerServerEvent("pma-voice:setTalkingOnRadio", new object[1] { false });
				ToggleRadio(on: false);
				timeoutT = DateTime.UtcNow;
				return;
			}
		}
		else
		{
			clickCount = 0;
		}
		lastClickT = DateTime.UtcNow;
	}

	private void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		RefreshChannels();
		if (!CanUseAviationFrequencies(e.Vehicle))
		{
			return;
		}
		RadioChannel radioChannel = availableChannels.FirstOrDefault((RadioChannel c) => c.IsAviation);
		if (radioChannel == null)
		{
			return;
		}
		dynamic val = Game.Player.State.Get("radioChannel");
		if (val <= 0f)
		{
			ToggleRadio(on: true);
			SetChannel(radioChannel.Frequency);
			if (!pilotRadioInfoShown)
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Info, "You can use the radio to communicate with other pilots. Type /radio to view the radio controls.");
				pilotRadioInfoShown = true;
			}
		}
	}

	private void OnLeftVehicle(object sender, VehicleEventArgs e)
	{
		RefreshChannels();
		if (!IsRadioOn)
		{
			return;
		}
		dynamic currentFreq = Game.Player.State.Get("radioChannel");
		if (!availableChannels.Any((RadioChannel c) => c.Frequency == currentFreq))
		{
			if (availableChannels.Count > 0)
			{
				SetChannel(availableChannels.First().Frequency);
			}
			else
			{
				ToggleRadio(on: false);
			}
		}
	}

	private void OnListIndexChange(Menu menu, MenuListItem listItem, int oldSelectionIndex, int newSelectionIndex, int itemIndex)
	{
		if (listItem == PowerMenuItem)
		{
			TimeSpan timeSpan = TimeSpan.FromSeconds(30.0);
			if (!Gtacnr.Utils.CheckTimePassed(timeoutT, timeSpan))
			{
				listItem.ListIndex = 0;
				Utils.DisplayHelpText($"You must wait ~r~{Gtacnr.Utils.GetCooldownTimeLeft(timeoutT, timeSpan).TotalSeconds:0} seconds ~s~before turning your radio on again.");
			}
			else
			{
				ToggleRadioInternal(newSelectionIndex == 1);
			}
		}
		else if (listItem == ChannelMenuItem)
		{
			if (PowerMenuItem.ListIndex == 1)
			{
				RadioChannel radioChannel = availableChannels.ElementAt(newSelectionIndex);
				SetChannelInternal(radioChannel.Frequency);
			}
		}
		else if (listItem == VolumeMenuItem)
		{
			int key = volumes.ElementAt(newSelectionIndex).Key;
			SetVolumeInternal(key);
		}
	}

	private void UpdateChannelDescription()
	{
		MenuListItem channelMenuItem = ChannelMenuItem;
		channelMenuItem.Description = "";
		if (channelMenuItem.ItemsCount != 0)
		{
			RadioChannel radioChannel = availableChannels[channelMenuItem.ListIndex];
			if (radioChannel != null)
			{
				channelMenuItem.Description = radioChannel.Description + "\n~s~" + LocalizationController.S(Entries.Businesses.RADIO_FEATURE_NOTIFICATION);
				MainMenu.CounterPreText = $"{radioChannel.Frequency:0.000}MHz";
			}
		}
	}
}
