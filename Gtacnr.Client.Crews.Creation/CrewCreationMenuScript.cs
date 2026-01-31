using System.Collections.Generic;
using Gtacnr.Client.API;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Crews.Creation;

public class CrewCreationMenuScript : Script
{
	private static readonly CrewConfig Config = Gtacnr.Utils.LoadJson<CrewConfig>("data/crews/config.json");

	private static MenuItem CreateCrewItem = new MenuItem("Create Crew", "Start the crew creation process.");

	private static MenuItem PendingRequestsItem = new MenuItem("Pending Requests", "View your pending crew creation requests.");

	private static Menu CreationMenu = new Menu("Create Crew");

	private static MenuItem CrewAcronymItem = new MenuItem("Crew Acronym", "Set the acronym for your crew.");

	private static MenuItem CrewNameItem = new MenuItem("Crew Name", "Set the name for your crew.");

	private static MenuItem SendCreationRequestItem = new MenuItem("Send Request", "Send your crew creation request.");

	private static bool isBusy = false;

	public static Menu MainMenu { get; private set; } = new Menu("Crew Creation", "Create your crew");

	public CrewCreationMenuScript()
	{
		MenuController.AddMenu(MainMenu);
		MainMenu.AddMenuItem(CreateCrewItem);
		MainMenu.AddMenuItem(PendingRequestsItem);
		MainMenu.OnMenuOpen += OnMainMenuOpen;
		MenuController.BindMenuItem(MainMenu, CreationMenu, CreateCrewItem);
		MenuController.BindMenuItem(MainMenu, CrewCreationRequestsMenuScript.MainMenu, PendingRequestsItem);
		MenuController.AddMenu(CreationMenu);
		CreationMenu.AddMenuItem(CrewAcronymItem);
		CreationMenu.AddMenuItem(CrewNameItem);
		CreationMenu.AddMenuItem(SendCreationRequestItem);
		CreationMenu.OnMenuOpen += OnCreationMenuOpen;
		CreationMenu.OnItemSelect += OnCreationMenuItemSelect;
	}

	private async void OnMainMenuOpen(Menu menu)
	{
		int price = Config.CreationPrice;
		int reqLevel = Config.RequiredLevel;
		List<string> errors = new List<string>();
		if (Gtacnr.Utils.GetLevelByXP(Users.CachedXP) < reqLevel)
		{
			errors.Add($"You need to be at least level {reqLevel} to create a crew.");
		}
		if (await Money.GetBalance(AccountType.Bank) < price)
		{
			errors.Add($"You need at least ${price:N0} in your bank account to create a crew.");
		}
		if (errors.Count > 0)
		{
			CreateCrewItem.Enabled = false;
			CreateCrewItem.Description = "You cannot create a crew right now for the following reasons:\n- " + string.Join("\n- ", errors);
		}
		else
		{
			CreateCrewItem.Enabled = true;
			CreateCrewItem.Description = $"You can create a crew! This will cost you ~g~${price:N0}~s~ and requires level ~b~{reqLevel}~s~.";
		}
	}

	private void OnCreationMenuOpen(Menu menu)
	{
		CrewAcronymItem.Description = null;
		CrewNameItem.Description = null;
	}

	private async void OnCreationMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == CrewAcronymItem)
		{
			if (isBusy)
			{
				Utils.PlayErrorSound();
				return;
			}
			string text = await Utils.GetUserInput("Crew Acronym", "Enter the acronym for your crew (1-6 characters):", "", 6);
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			if (!text.Contains(" ") && !text.Contains("\u200e"))
			{
				CrewAcronymItem.Description = text;
				try
				{
					isBusy = true;
					if (!(await TriggerServerEventAsync<bool>("gtacnr:crews:creation:checkAcronym", new object[1] { text })))
					{
						Utils.DisplayHelpText("~r~This acronym is already taken. Please choose another one.");
						CrewAcronymItem.Description = null;
					}
					return;
				}
				finally
				{
					isBusy = false;
				}
			}
			Utils.DisplayHelpText("~r~Crew acronym cannot contain spaces.");
		}
		else
		{
			if (menuItem == CrewNameItem)
			{
				if (isBusy)
				{
					Utils.PlayErrorSound();
					return;
				}
				string text2 = await Utils.GetUserInput("Crew Name", "Enter the name for your crew (1-32 characters):", "", 32);
				if (string.IsNullOrWhiteSpace(text2))
				{
					return;
				}
				CrewNameItem.Description = text2;
				try
				{
					isBusy = true;
					if (!(await TriggerServerEventAsync<bool>("gtacnr:crews:creation:checkName", new object[1] { text2 })))
					{
						Utils.DisplayHelpText("~r~This name is already taken. Please choose another one.");
						CrewNameItem.Description = null;
					}
					return;
				}
				finally
				{
					isBusy = false;
				}
			}
			if (menuItem != SendCreationRequestItem)
			{
				return;
			}
			string acronym = CrewAcronymItem.Description;
			string name = CrewNameItem.Description;
			if (string.IsNullOrWhiteSpace(acronym) || acronym.Length > 6 || acronym.Length < 2)
			{
				Utils.DisplayHelpText("~r~Invalid crew acronym. It must be 2-6 characters long.");
			}
			else if (string.IsNullOrWhiteSpace(name) || name.Length > 32 || name.Length < 2)
			{
				Utils.DisplayHelpText("~r~Invalid crew name. It must be 2-32 characters long.");
			}
			else
			{
				if (!(await Utils.ShowConfirm("Are you sure you want to send this crew creation request?\n\nThis request might be denied by admins, if it happens you will not receive a refund - so make sure that your crew name and acronym are compliant with the rules and fivem's PLA.", "Confirm Crew Creation")) || isBusy)
				{
					return;
				}
				try
				{
					isBusy = true;
					CrewsCreationResponse crewsCreationResponse = (CrewsCreationResponse)(await TriggerServerEventAsync<int>("gtacnr:crews:creation:newRequest", new object[2] { acronym, name }));
					switch (crewsCreationResponse)
					{
					case CrewsCreationResponse.Success:
						Utils.PlaySelectSound();
						Utils.DisplayHelpText("Your crew ~b~" + acronym + "~s~ creation request have been sent successfully!", playSound: false);
						MenuController.CloseAllMenus();
						break;
					case CrewsCreationResponse.AcronymTaken:
						Utils.DisplayHelpText("~r~The crew acronym is already taken. Please choose another one.");
						Utils.PlayErrorSound();
						break;
					case CrewsCreationResponse.NameTaken:
						Utils.DisplayHelpText("~r~The crew name is already taken. Please choose another one.");
						Utils.PlayErrorSound();
						break;
					case CrewsCreationResponse.AcronymDisallowed:
						Utils.DisplayHelpText("~r~This crew acronym is disallowed. Please choose another one.");
						Utils.PlayErrorSound();
						break;
					case CrewsCreationResponse.InsufficientFunds:
						Utils.DisplayHelpText("~r~You do not have enough money to create a crew.");
						Utils.PlayErrorSound();
						break;
					case CrewsCreationResponse.InsufficientLevel:
						Utils.DisplayHelpText("~r~You do not meet the level requirement to create a crew.");
						Utils.PlayErrorSound();
						break;
					case CrewsCreationResponse.PendingRequest:
						Utils.DisplayHelpText("~r~You already have a pending crew creation request.");
						Utils.PlayErrorSound();
						break;
					default:
						Utils.DisplayErrorMessage(0, -1, $"{crewsCreationResponse}");
						Utils.PlayErrorSound();
						break;
					}
				}
				finally
				{
					isBusy = false;
				}
			}
		}
	}
}
