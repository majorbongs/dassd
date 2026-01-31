using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Weapons;
using Gtacnr.Data;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.Keybinder;

public class KeybindScript : Script
{
	private static DateTime lastTimestamp;

	private static Dictionary<string, Dictionary<string, BindableAction>> keybindsPerJob = new Dictionary<string, Dictionary<string, BindableAction>>();

	private static Menu quickActionsMenu;

	private Dictionary<int, Tuple<Control, MenuItem.Icon>> quickSelect = new Dictionary<int, Tuple<Control, MenuItem.Icon>>
	{
		{
			1,
			Tuple.Create<Control, MenuItem.Icon>((Control)203, MenuItem.Icon.XBOX_X)
		},
		{
			2,
			Tuple.Create<Control, MenuItem.Icon>((Control)204, MenuItem.Icon.XBOX_Y)
		},
		{
			3,
			Tuple.Create<Control, MenuItem.Icon>((Control)205, MenuItem.Icon.XBOX_LB)
		},
		{
			4,
			Tuple.Create<Control, MenuItem.Icon>((Control)206, MenuItem.Icon.XBOX_RB)
		},
		{
			5,
			Tuple.Create<Control, MenuItem.Icon>((Control)207, MenuItem.Icon.XBOX_LT)
		},
		{
			6,
			Tuple.Create<Control, MenuItem.Icon>((Control)208, MenuItem.Icon.XBOX_RT)
		},
		{
			7,
			Tuple.Create<Control, MenuItem.Icon>((Control)209, MenuItem.Icon.XBOX_LS)
		},
		{
			8,
			Tuple.Create<Control, MenuItem.Icon>((Control)210, MenuItem.Icon.XBOX_RS)
		},
		{
			9,
			Tuple.Create<Control, MenuItem.Icon>((Control)217, MenuItem.Icon.XBOX_VIEW)
		}
	};

	private DateTime k1PressT = DateTime.MinValue;

	private DateTime k2PressT = DateTime.MinValue;

	private static string CurrentJobLoadoutId
	{
		get
		{
			if (!string.IsNullOrEmpty(Gtacnr.Client.API.Jobs.CachedJob))
			{
				Job? jobData = Gtacnr.Data.Jobs.GetJobData(Gtacnr.Client.API.Jobs.CachedJob);
				if (jobData == null || jobData.SeparateLoadout)
				{
					return Gtacnr.Client.API.Jobs.CachedJob;
				}
			}
			return "none";
		}
	}

	private static Dictionary<string, BindableAction> CurrentJobKeybinds
	{
		get
		{
			if (!keybindsPerJob.TryGetValue(CurrentJobLoadoutId, out Dictionary<string, BindableAction> value))
			{
				value = new Dictionary<string, BindableAction>();
				keybindsPerJob[CurrentJobLoadoutId] = value;
			}
			return value;
		}
	}

	public KeybindScript()
	{
		LoadBinds();
		AddSuggestions();
		CreateQuickActionsMenu();
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChanged;
	}

	private void OnJobChanged(object sender, JobArgs e)
	{
		bool flag = Gtacnr.Data.Jobs.GetJobData(e.PreviousJobId)?.SeparateLoadout ?? false;
		bool flag2 = Gtacnr.Data.Jobs.GetJobData(e.CurrentJobId)?.SeparateLoadout ?? false;
		if (flag2 || flag)
		{
			if (!keybindsPerJob.ContainsKey(e.CurrentJobId) && flag2 && keybindsPerJob.TryGetValue("none", out Dictionary<string, BindableAction> value))
			{
				keybindsPerJob[e.CurrentJobId] = new Dictionary<string, BindableAction>(value);
				SaveKeybinds();
			}
			RefreshQuickActionsMenu();
		}
	}

	private void LoadBinds()
	{
		string resourceKvpString = API.GetResourceKvpString("keybindsv2");
		if (!string.IsNullOrWhiteSpace(resourceKvpString))
		{
			try
			{
				keybindsPerJob = resourceKvpString.Unjson<Dictionary<string, Dictionary<string, BindableAction>>>();
			}
			catch (Exception exception)
			{
				Debug.WriteLine("Failed to load keybinds.");
				Gtacnr.Utils.PrintException(exception);
				keybindsPerJob = new Dictionary<string, Dictionary<string, BindableAction>>();
			}
		}
		else
		{
			Dictionary<string, BindableAction> dictionary = new Dictionary<string, BindableAction>();
			for (int i = 0; i < 10; i++)
			{
				string text = $"kb_{i}";
				string resourceKvpString2 = API.GetResourceKvpString(text);
				if (!string.IsNullOrWhiteSpace(resourceKvpString2))
				{
					BindableAction value = resourceKvpString2.Unjson<BindableAction>();
					dictionary[text] = value;
				}
			}
			if (dictionary.Count > 0)
			{
				keybindsPerJob["none"] = dictionary;
				SaveKeybinds();
			}
		}
		List<string> list = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
		for (int j = 0; j < 10; j++)
		{
			string key = $"kb_{j}";
			API.RegisterKeyMapping(key, $"Hotkey {j + 1:00}", "keyboard", list[j]);
			API.RegisterCommand(key, InputArgument.op_Implicit((Delegate)(Action)delegate
			{
				RunAction(key);
			}), false);
		}
	}

	private static void SaveKeybinds()
	{
		API.SetResourceKvp("keybindsv2", keybindsPerJob.Json());
	}

	private void AddSuggestions()
	{
		Chat.AddSuggestion("/addhotkey", "Binds a hotkey to an action.", new ChatParamSuggestion("key", "The hotkey index to bind (1-10)."), new ChatParamSuggestion("type", "The action type ('weapon', 'item', 'command')."), new ChatParamSuggestion("parameter", "The action parameter (weapon id, item id, command)."));
		Chat.AddSuggestion("/remhotkey", "Unbinds a hotkey.", new ChatParamSuggestion("key", "The hotkey index to unbind (1-10)."));
	}

	public static void SetBind(string key, BindableAction action)
	{
		CurrentJobKeybinds[key] = action;
		SaveKeybinds();
	}

	public static void ResetBind(string key)
	{
		if (CurrentJobKeybinds.Remove(key))
		{
			SaveKeybinds();
		}
	}

	public static IReadOnlyDictionary<string, BindableAction> GetAllBinds()
	{
		return new Dictionary<string, BindableAction>(CurrentJobKeybinds);
	}

	[Command("addhotkey")]
	private void AddHotkeyCommand(string[] args)
	{
		if (args.Length < 3)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "Usage: /addhotkey [index] [type] [parameter].");
			return;
		}
		if (!int.TryParse(args[0], out var result))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "The hotkey index must be numeric (1-10).");
			return;
		}
		if (result < 1 || result > 10)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "The hotkey index must be in the range 1-10.");
			return;
		}
		result--;
		string key = $"kb_{result}";
		string text = args[2];
		BindableAction bindableAction;
		if (args[1] == "weapon")
		{
			bindableAction = new BindableAction
			{
				Type = BindableActionType.EquipWeapon,
				Param = text
			};
			if (!Gtacnr.Data.Items.IsWeaponDefined(text))
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "The specified weapon is undefined.");
				return;
			}
			WeaponDefinition weaponDefinition = Gtacnr.Data.Items.GetWeaponDefinition(text);
			Chat.AddMessage(Gtacnr.Utils.Colors.Info, $"You've bound hotkey #{result + 1} to select weapon {weaponDefinition.Name}.");
		}
		else if (args[1] == "item")
		{
			bindableAction = new BindableAction
			{
				Type = BindableActionType.UseItem,
				Param = text
			};
			if (!Gtacnr.Data.Items.IsItemDefined(text))
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "The specified item is undefined.");
				return;
			}
			InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(text);
			Chat.AddMessage(Gtacnr.Utils.Colors.Info, $"You've bound hotkey #{result + 1} to use item {itemDefinition.Name}.");
		}
		else
		{
			if (!(args[1] == "command"))
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "The valid types are 'weapon', 'item' and 'command'.");
				return;
			}
			text = string.Join(" ", args.Skip(2));
			bindableAction = new BindableAction
			{
				Type = BindableActionType.Custom,
				Param = text
			};
			text = text.TrimStart('/').Trim();
			Chat.AddMessage(Gtacnr.Utils.Colors.Info, $"You've bound hotkey #{result + 1} to run command '/{text}'.");
		}
		if (bindableAction != null)
		{
			SetBind(key, bindableAction);
		}
	}

	[Command("remhotkey")]
	private void RemoveHotkeyCommand(string[] args)
	{
		if (args.Length < 1)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "Usage: /remhotkey [index].");
			return;
		}
		if (!int.TryParse(args[0], out var result))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "The hotkey index must be numeric (1-10).");
			return;
		}
		if (result < 1 || result > 10)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "The hotkey index must be in the range 1-10.");
			return;
		}
		result--;
		string key = $"kb_{result}";
		Chat.AddMessage(Gtacnr.Utils.Colors.Info, $"You've reset hotkey #{result + 1}.");
		ResetBind(key);
	}

	private void RunAction(string key)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		if ((Entity)(object)Game.PlayerPed == (Entity)null || !SpawnScript.HasSpawned || Utils.IsOnScreenKeyboardActive || !CurrentJobKeybinds.TryGetValue(key, out BindableAction value))
		{
			return;
		}
		switch (value.Type)
		{
		case BindableActionType.EquipWeapon:
		{
			WeaponHash val = (WeaponHash)API.GetHashKey(value.Param);
			if (Game.PlayerPed.Weapons.Current.Hash != val)
			{
				WeaponBehaviorScript.QuickSwitchToWeapon(val);
			}
			else
			{
				WeaponBehaviorScript.QuickSwitchToWeapon((WeaponHash)API.GetHashKey("weapon_unarmed"));
			}
			break;
		}
		case BindableActionType.UseItem:
			if (!Gtacnr.Utils.CheckTimePassed(lastTimestamp, 200.0))
			{
				Utils.PlayErrorSound();
				return;
			}
			lastTimestamp = DateTime.UtcNow;
			API.ExecuteCommand("use " + value.Param + " 1");
			break;
		case BindableActionType.Custom:
			if (!Gtacnr.Utils.CheckTimePassed(lastTimestamp, 200.0))
			{
				Utils.PlayErrorSound();
				return;
			}
			lastTimestamp = DateTime.UtcNow;
			API.ExecuteCommand(value.Param);
			break;
		}
		BaseScript.TriggerEvent("gtacnr:keybinds:event", new object[1] { key });
	}

	private void CreateQuickActionsMenu()
	{
		quickActionsMenu = new Menu("Quick Actions", "Select an action");
		MenuController.AddMenu(quickActionsMenu);
		quickActionsMenu.OnItemSelect += OnQuickActionsMenuItemSelect;
		RefreshQuickActionsMenu();
		KeysScript.AttachListener((IEnumerable<Control>)(object)new Control[2]
		{
			(Control)36,
			(Control)26
		}, HandleControllerOpenQuickMenuEvent, 1000);
		KeysScript.AttachListener(quickSelect.Values.Select((Tuple<Control, MenuItem.Icon> v) => v.Item1), HandleControllerHotkeyEvents, 1000);
	}

	private void RefreshQuickActionsMenu()
	{
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		quickActionsMenu.ClearMenuItems();
		quickActionsMenu.ButtonPressHandlers.Clear();
		int idx = 0;
		foreach (KeyValuePair<string, BindableAction> currentJobKeybind in CurrentJobKeybinds)
		{
			string key = currentJobKeybind.Key;
			BindableAction value = currentJobKeybind.Value;
			string description = Gtacnr.Utils.GetDescription(value.Type);
			string text = "";
			if (value.Type == BindableActionType.EquipWeapon)
			{
				text = Gtacnr.Data.Items.GetWeaponDefinition(value.Param)?.Name ?? "~r~invalid";
			}
			else if (value.Type == BindableActionType.UseItem)
			{
				text = Gtacnr.Data.Items.GetItemDefinition(value.Param)?.Name ?? "~r~invalid";
			}
			else if (value.Type == BindableActionType.Custom)
			{
				text = "/" + value.Param;
			}
			MenuItem menuItem = new MenuItem(description + " ~b~" + text)
			{
				Description = "You can configure these in ~y~Menu ~s~> ~y~Options ~s~> ~y~Hotkeys~s~.",
				ItemData = key
			};
			quickActionsMenu.AddMenuItem(menuItem);
			Control control;
			if (quickSelect.ContainsKey(idx))
			{
				Tuple<Control, MenuItem.Icon> tuple = quickSelect[idx];
				control = tuple.Item1;
				MenuItem.Icon item = tuple.Item2;
				AddHandler();
				menuItem.RightIcon = item;
			}
			int num = idx;
			idx = num + 1;
			async void AddHandler()
			{
				while (Game.IsControlPressed(2, control))
				{
					await BaseScript.Delay(0);
				}
				quickActionsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(control, Menu.ControlPressCheckType.JUST_PRESSED, delegate
				{
					OnQuickActionsMenuItemSelect(quickActionsMenu, menuItem, idx);
				}, disableControl: true));
			}
		}
	}

	private void OpenQuickActionsMenu()
	{
		if (quickActionsMenu.Visible)
		{
			quickActionsMenu.CloseMenu();
			return;
		}
		RefreshQuickActionsMenu();
		if (quickActionsMenu.GetMenuItems().Count != 0)
		{
			MenuController.CloseAllMenus();
			quickActionsMenu.OpenMenu();
		}
	}

	private void OnQuickActionsMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem.ItemData is string key)
		{
			MenuController.CloseAllMenus();
			RunAction(key);
		}
	}

	private bool HandleControllerOpenQuickMenuEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		if (eventType == KeyEventType.JustPressed && inputType == InputType.Controller)
		{
			if ((int)control == 36)
			{
				k1PressT = DateTime.UtcNow;
			}
			if ((int)control == 26)
			{
				k2PressT = DateTime.UtcNow;
			}
			if (!Gtacnr.Utils.CheckTimePassed(k1PressT, 100.0) && !Gtacnr.Utils.CheckTimePassed(k2PressT, 100.0))
			{
				OpenQuickActionsMenu();
				return true;
			}
		}
		return false;
	}

	private bool HandleControllerHotkeyEvents(Control control, KeyEventType eventType, InputType inputType)
	{
		if (quickActionsMenu.Visible && inputType == InputType.Controller)
		{
			return true;
		}
		return false;
	}
}
