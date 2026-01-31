using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.API;
using Gtacnr.Client.Crews;
using Gtacnr.Client.IMenu;
using Gtacnr.Data;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.Model.PrefixedGUIDs;

namespace Gtacnr.Client.Premium;

public class CustomScript : Script
{
	private class CustomInfo
	{
		public string UserId { get; set; }

		public HashSet<string> UserIds { get; set; } = new HashSet<string>();

		public CrewId? CrewId { get; set; }

		public string Motto { get; set; }

		public List<int[]> Colors { get; set; } = new List<int[]>();

		public Dictionary<string, List<int>> Liveries { get; set; } = new Dictionary<string, List<int>>();

		public Dictionary<string, List<string>> Clothes { get; set; } = new Dictionary<string, List<string>>();

		public Dictionary<string, List<string>> Vehicles { get; set; } = new Dictionary<string, List<string>>();
	}

	private class CommandInfo
	{
		public Predicate<CustomInfo> CanExecute { get; set; }

		public Action<string, string[]> Action { get; set; }

		public ChatSuggestion Suggestion { get; set; }

		public CommandInfo(Action<string, string[]> action, Predicate<CustomInfo> canExecute, ChatSuggestion suggestion)
		{
			Action = action;
			CanExecute = canExecute;
			Suggestion = suggestion;
		}
	}

	private static readonly Dictionary<string, CustomInfo> infoDictionary = Gtacnr.Utils.LoadJson<Dictionary<string, CustomInfo>>("gtacnr_items", "data/custom/custom.json");

	private static readonly Dictionary<int, HashSet<int>> restrictedLiveries = InitializeRestrictedLiveries();

	private static readonly HashSet<string> restrictedClothingIds = new HashSet<string>();

	private static readonly HashSet<string> authorizedClothingIds = new HashSet<string>();

	private bool isDrivingVehicle;

	private bool enableManagerAccess;

	private StaffLevel staffLevel;

	private static Dictionary<string, List<string>> wardrobeAddedClothing = new Dictionary<string, List<string>>();

	private static CustomScript instance;

	public static bool DataLoaded { get; private set; } = false;

	public static HashSet<string> GetUnauthorizedClothingIds()
	{
		HashSet<string> hashSet = new HashSet<string>(restrictedClothingIds);
		hashSet.ExceptWith(authorizedClothingIds);
		return hashSet;
	}

	public static bool IsLiveryRestricted(int vehicleHash, int livery)
	{
		if (!restrictedLiveries.ContainsKey(vehicleHash))
		{
			return false;
		}
		return restrictedLiveries[vehicleHash].Contains(livery);
	}

	public static IReadonlyHashSet<int> GetRestrictedLiveries(int vehicleHash)
	{
		if (!restrictedLiveries.ContainsKey(vehicleHash))
		{
			return new HashSet<int>().AsReadOnly();
		}
		return restrictedLiveries[vehicleHash].AsReadOnly();
	}

	[Command("auth-custom")]
	private async void AuthCustomCommand()
	{
		if (!enableManagerAccess && (int)staffLevel >= 130)
		{
			enableManagerAccess = true;
			Chat.AddMessage(Gtacnr.Utils.Colors.Moderation, "Enabling all custom assets...");
			await RegisterCommands();
			await GiveClothes();
			await GiveVehicles();
			Chat.AddMessage(Gtacnr.Utils.Colors.Moderation, "You have obtained access to all custom assets.");
		}
	}

	[Command("wear-clothing")]
	private void WearClothingCommand(string[] args)
	{
		if ((int)StaffLevelScript.StaffLevel >= 130)
		{
			if (args.Length < 1)
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "Usage: /wear-clothing [itemId]");
			}
			else
			{
				Clothes.CurrentApparel.Replace(args[0]);
			}
		}
	}

	public CustomScript()
	{
		instance = this;
		foreach (CustomInfo value in infoDictionary.Values)
		{
			foreach (List<string> value2 in value.Clothes.Values)
			{
				restrictedClothingIds.UnionWith(value2);
			}
		}
		CrewScript.OnCrewChanged += OnCrewChanged;
	}

	private async void OnCrewChanged(object sender, CrewScript.CrewChangedEventArgs e)
	{
		int num;
		if (num == 0)
		{
			TaskAwaiter taskAwaiter2 = default(TaskAwaiter);
			TaskAwaiter taskAwaiter = taskAwaiter2;
			taskAwaiter.GetResult();
		}
	}

	public static async Task LoadCustom()
	{
		await instance.LoadCustomInternal();
	}

	private async Task LoadCustomInternal()
	{
		_ = 4;
		try
		{
			staffLevel = (StaffLevel)(await TriggerServerEventAsync<int>("gtacnr:admin:getLevel", new object[0]));
			DateTime start = DateTime.UtcNow;
			while (!CrewScript.CrewDataLoaded && !Gtacnr.Utils.CheckTimePassed(start, 5000.0))
			{
				await BaseScript.Delay(100);
			}
			await RegisterCommands();
			await GiveClothes();
			await GiveVehicles();
			DataLoaded = true;
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	private async Task<bool> IsAuthorized(CustomInfo info)
	{
		if ((int)staffLevel >= 130 && enableManagerAccess)
		{
			return true;
		}
		string customAccountId = await Authentication.GetAccountId();
		await Authentication.GetAccountName();
		return info.UserId == customAccountId || info.UserIds.Contains(customAccountId) || (info.CrewId != null && CrewScript.CrewData?.Id == info.CrewId);
	}

	private async Task RegisterCommands()
	{
		Dictionary<string, CommandInfo> commands = new Dictionary<string, CommandInfo>
		{
			{
				"paint",
				new CommandInfo(PaintCommand, (CustomInfo ci) => ci.Colors.Count > 0, new ChatSuggestion("/paint", "Applies the custom paint.", new ChatParamSuggestion("index", "The index of the paintjob to apply.")))
			},
			{
				"livery",
				new CommandInfo(LiveryCommand, (CustomInfo ci) => ci.Liveries.Count > 0, new ChatSuggestion("/livery", "Applies the custom livery.", new ChatParamSuggestion("index", "The index of the livery to apply.", isOptional: true)))
			}
		};
		Print("Authorizing custom assets...");
		foreach (string key in infoDictionary.Keys)
		{
			try
			{
				CustomInfo info = infoDictionary[key];
				if (!(await IsAuthorized(info)))
				{
					continue;
				}
				Print("    Authorized: " + key);
				foreach (string key2 in commands.Keys)
				{
					try
					{
						CommandInfo cmdAction = commands[key2];
						if (cmdAction.CanExecute(info))
						{
							string text = key + "-" + key2;
							API.RegisterCommand(text, InputArgument.op_Implicit((Delegate)(Action<int, List<object>, string>)delegate(int source, List<object> args, string raw)
							{
								cmdAction.Action(key, args.Cast<string>().ToArray());
							}), false);
							if (cmdAction.Suggestion != null)
							{
								cmdAction.Suggestion.Command = "/" + text;
								Chat.AddSuggestion(cmdAction.Suggestion);
							}
						}
					}
					catch (Exception exception)
					{
						Print(exception);
					}
				}
			}
			catch (Exception exception2)
			{
				Print(exception2);
			}
		}
	}

	private async Task GiveClothes()
	{
		await BaseScript.Delay(5000);
		foreach (string key in infoDictionary.Keys)
		{
			CustomInfo info = infoDictionary[key];
			if (!(await IsAuthorized(info)))
			{
				continue;
			}
			foreach (string key2 in info.Clothes.Keys)
			{
				List<string> list = info.Clothes[key2];
				authorizedClothingIds.UnionWith(list);
				foreach (string item in list)
				{
					WardrobeMenuScript.AddExtraClothingItem(key2, item, info.Motto);
				}
			}
		}
	}

	private async Task GiveVehicles()
	{
		await BaseScript.Delay(7000);
		foreach (string key in infoDictionary.Keys)
		{
			CustomInfo info = infoDictionary[key];
			if (!(await IsAuthorized(info)))
			{
				continue;
			}
			foreach (string key2 in info.Vehicles.Keys)
			{
				foreach (string item in info.Vehicles[key2])
				{
					VehiclesMenuScript.AddExtraVehicle(key2, item);
				}
			}
		}
	}

	private async void PaintCommand(string key, string[] args)
	{
		if (!infoDictionary.TryGetValue(key, out CustomInfo info))
		{
			return;
		}
		if (!(await IsAuthorized(info)))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "You are not authorized to use this command.");
			return;
		}
		if (info.Colors.Count == 0)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "No colors available for " + key + ".");
			return;
		}
		int index;
		if (info.Colors.Count == 1)
		{
			index = 0;
		}
		else
		{
			if (args.Length < 1 || !int.TryParse(args[0], out var result) || result < 1 || result > info.Colors.Count)
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Error, $"Usage: /{key}-paint [1-{info.Colors.Count}].");
				return;
			}
			index = result - 1;
		}
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "You must be in a vehicle to use that command.");
			return;
		}
		if ((Entity)(object)currentVehicle.Driver != (Entity)(object)Game.PlayerPed)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "You must be driving the vehicle to use that command.");
			return;
		}
		currentVehicle.Mods.PrimaryColor = (VehicleColor)info.Colors[index][0];
		currentVehicle.Mods.SecondaryColor = (VehicleColor)info.Colors[index][1];
		Chat.AddMessage(Gtacnr.Utils.Colors.Info, "You applied the " + key + " paint.");
		Utils.DisplaySubtitle(info.Motto);
	}

	private async void LiveryCommand(string key, string[] args)
	{
		if (!infoDictionary.TryGetValue(key, out CustomInfo info))
		{
			return;
		}
		if (!(await IsAuthorized(info)))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "You are not authorized to use this command.");
			return;
		}
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "You must be in a vehicle to use that command.");
			return;
		}
		if ((Entity)(object)currentVehicle.Driver != (Entity)(object)Game.PlayerPed)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "You must be driving the vehicle to use that command.");
			return;
		}
		Dictionary<int, List<int>> dictionary = info.Liveries.ToDictionary<KeyValuePair<string, List<int>>, int, List<int>>((KeyValuePair<string, List<int>> x) => Game.GenerateHash(x.Key), (KeyValuePair<string, List<int>> x) => x.Value);
		IEnumerable<string> values = info.Liveries.Keys.Select((string x) => Game.GetGXTEntry(Vehicle.GetModelDisplayName(Model.op_Implicit(x))));
		if (!dictionary.ContainsKey(Model.op_Implicit(((Entity)currentVehicle).Model)))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "Supported vehicles: " + string.Join(", ", values) + ".");
			return;
		}
		List<int> list = dictionary[Model.op_Implicit(((Entity)currentVehicle).Model)];
		int result = new Random().Next(list.Count);
		if (args.Length != 0 && int.TryParse(args[0], out result))
		{
			result--;
		}
		if (result < 0 || result >= list.Count)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, $"Valid liveries: 1-{list.Count}.");
			return;
		}
		currentVehicle.Mods.Livery = list[result];
		Chat.AddMessage(Gtacnr.Utils.Colors.Info, "You applied the " + key + " livery.");
		Utils.DisplaySubtitle(info.Motto);
	}

	[Update]
	private async Coroutine RemoveLiveryTask()
	{
		await Script.Wait(100);
		Ped playerPed = Game.PlayerPed;
		Vehicle vehicle = playerPed.CurrentVehicle;
		if (API.IsPedInAnyVehicle(((PoolObject)Game.PlayerPed).Handle, false) && (Entity)(object)vehicle.Driver == (Entity)(object)playerPed)
		{
			if (isDrivingVehicle)
			{
				return;
			}
			isDrivingVehicle = true;
			foreach (string key in infoDictionary.Keys)
			{
				CustomInfo info = infoDictionary[key];
				if (!(await IsAuthorized(info)))
				{
					Dictionary<int, List<int>> dictionary = info.Liveries.ToDictionary<KeyValuePair<string, List<int>>, int, List<int>>((KeyValuePair<string, List<int>> x) => Game.GenerateHash(x.Key), (KeyValuePair<string, List<int>> x) => x.Value);
					if (dictionary.ContainsKey(Model.op_Implicit(((Entity)vehicle).Model)) && dictionary[Model.op_Implicit(((Entity)vehicle).Model)].Contains(vehicle.Mods.Livery))
					{
						vehicle.Mods.Livery = 0;
						Chat.AddMessage(Gtacnr.Utils.Colors.Error, "This vehicle livery is reserved.");
					}
				}
			}
		}
		else
		{
			isDrivingVehicle = false;
		}
	}

	private static Dictionary<int, HashSet<int>> InitializeRestrictedLiveries()
	{
		Dictionary<int, HashSet<int>> dictionary = new Dictionary<int, HashSet<int>>();
		foreach (CustomInfo value in infoDictionary.Values)
		{
			foreach (KeyValuePair<string, List<int>> livery in value.Liveries)
			{
				int key = Game.GenerateHash(livery.Key);
				if (!dictionary.ContainsKey(key))
				{
					dictionary[key] = new HashSet<int>(livery.Value);
				}
				dictionary[key].UnionWith(livery.Value);
			}
		}
		return dictionary;
	}

	[EventHandler("gtacnr:custom:disableDamage")]
	private void OnDisableDamage()
	{
		foreach (WeaponDefinition allWeaponDefinition in Gtacnr.Data.Items.GetAllWeaponDefinitions())
		{
			API.SetWeaponDamageModifier((uint)allWeaponDefinition.Hash, 0f);
		}
	}
}
