using System.Collections.Generic;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.PrefixedGUIDs;
using Gtacnr.ResponseCodes;
using MenuAPI;
using Rock.Collections;

namespace Gtacnr.Client.Crews;

public sealed class CrewJoinMenuScript : Script
{
	private static MenuItem CrewSelectionSearchByAcronymItem = new MenuItem("Search by Acronym", "Search for a crew by its acronym");

	private static MenuItem CrewSelectionEmptyItem = new MenuItem("No crews online", "~r~There are currently no crews online.");

	private static Menu ApplicationMenu = new Menu("Crew Application", "Submit your application to join the crew");

	private static MenuItem IntroItem = new MenuItem("Introduction", "N/A");

	private static MenuItem SendItem = new MenuItem("Send");

	private static bool isBusy = false;

	private static Crew? selectedCrew = null;

	private static OrderedDictionary<CrewId, Crew>? crewsCache;

	private static HashSet<CrewId> foundCrews = new HashSet<CrewId>();

	public static Menu MainMenu { get; private set; } = new Menu("Join a Crew", "Select a crew to join");

	public CrewJoinMenuScript()
	{
		MenuController.AddMenu(MainMenu);
		MainMenu.OnMenuOpen += OnMainMenuOpen;
		MainMenu.OnItemSelect += OnMainMenuItemSelect;
		MenuController.AddMenu(ApplicationMenu);
		ApplicationMenu.AddMenuItem(IntroItem);
		ApplicationMenu.AddMenuItem(SendItem);
		ApplicationMenu.OnMenuOpen += OnApplicationMenuOpen;
		ApplicationMenu.OnItemSelect += OnApplicationItemSelect;
		LocalizationController.LanguageChanged += OnLanguageChanged;
	}

	private void OnApplicationMenuOpen(Menu menu)
	{
		IntroItem.Description = "N/A";
	}

	private async void OnApplicationItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == IntroItem)
		{
			string defaultText = null;
			if (!string.IsNullOrEmpty(IntroItem.Description))
			{
				defaultText = IntroItem.Description;
			}
			string text = await Utils.GetUserInput("Introduction", "Enter a short introduction (50+ char).", "", 200, "text", defaultText);
			if (text != null)
			{
				IntroItem.Description = text;
			}
		}
		else
		{
			if (menuItem != SendItem)
			{
				return;
			}
			if (selectedCrew == null)
			{
				Utils.DisplayErrorMessage(0, -1, "No crew selected.");
				Utils.PlayErrorSound();
				return;
			}
			string description = IntroItem.Description;
			if (string.IsNullOrEmpty(description) || description.Length < 50)
			{
				Utils.DisplayErrorMessage(0, -1, $"Introduction must be at least {50} characters long.");
				Utils.PlayErrorSound();
			}
			else
			{
				if (isBusy)
				{
					return;
				}
				try
				{
					isBusy = true;
					CrewsNewApplicationResponse crewsNewApplicationResponse = (CrewsNewApplicationResponse)(await TriggerServerEventAsync<int>("gtacnr:crews:apply", new object[2]
					{
						selectedCrew.Id.ToString(),
						description
					}));
					switch (crewsNewApplicationResponse)
					{
					case CrewsNewApplicationResponse.Success:
						Utils.PlaySelectSound();
						Utils.DisplayHelpText("Your application to join ~b~" + selectedCrew.Acronym + " ~s~has been sent.", playSound: false);
						MenuController.CloseAllMenus();
						break;
					case CrewsNewApplicationResponse.AlreadyApplied:
						Utils.PlayErrorSound();
						Utils.DisplayErrorMessage(0, -1, "You have already applied to this crew. Please wait for a response.");
						break;
					case CrewsNewApplicationResponse.CrewNotFound:
						Utils.PlayErrorSound();
						Utils.DisplayErrorMessage(0, -1, "The selected crew was not found.");
						break;
					case CrewsNewApplicationResponse.CrewNotAcceptingApplications:
						Utils.PlayErrorSound();
						Utils.DisplayErrorMessage(0, -1, "The selected crew is not accepting applications at this time.");
						break;
					default:
						Utils.DisplayErrorMessage(0, -1, $"{crewsNewApplicationResponse}");
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

	private async void OnMainMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == CrewSelectionSearchByAcronymItem)
		{
			string text = await Utils.GetUserInput("Search Crew", "Enter the crew acronym (3-6 characters).", "", 6);
			if (text == null || text.Length < 2)
			{
				return;
			}
			string text2 = await TriggerServerEventAsync<string>("gtacnr:crews:getByAcronym", new object[1] { text });
			if (string.IsNullOrEmpty(text2))
			{
				Utils.DisplayHelpText("~r~Crew not found.");
				Utils.PlayErrorSound();
				return;
			}
			Crew crew = text2.Unjson<Crew>();
			if (crew == null)
			{
				Utils.DisplayHelpText("~r~Crew not found.");
				Utils.PlayErrorSound();
				return;
			}
			if (crewsCache.ContainsKey(crew.Id))
			{
				Utils.DisplayHelpText("~r~Crew is already listed.");
				Utils.PlayErrorSound();
				return;
			}
			MenuItem menuItem2 = crew.ToMenuItem();
			MainMenu.InsertMenuItem(menuItem2, MainMenu.GetMenuItems().Count);
			MenuController.BindMenuItem(MainMenu, ApplicationMenu, menuItem2);
			MainMenu.RemoveMenuItem(CrewSelectionEmptyItem);
			crewsCache.Add(crew.Id, crew);
		}
		else if (menuItem.ItemData is Crew crew2)
		{
			selectedCrew = crew2;
			ApplicationMenu.OpenMenu();
			ApplicationMenu.CounterPreText = crew2.Acronym;
		}
	}

	private async void OnMainMenuOpen(Menu menu)
	{
		menu.ClearMenuItems();
		menu.AddLoadingMenuItem();
		if (crewsCache == null)
		{
			List<Crew> list = (await TriggerServerEventAsync<string>("gtacnr:crews:getOnline", new object[0])).Unjson<List<Crew>>();
			crewsCache = new OrderedDictionary<CrewId, Crew>();
			if (list != null)
			{
				foreach (Crew item in list)
				{
					crewsCache[item.Id] = item;
				}
			}
		}
		MainMenu.ClearMenuItems();
		if (crewsCache.Count == 0)
		{
			MainMenu.AddMenuItem(new MenuItem("No crews online", "~r~There are currently no crews online."));
		}
		else
		{
			foreach (Crew value in crewsCache.Values)
			{
				MenuItem menuItem = value.ToMenuItem();
				MainMenu.AddMenuItem(menuItem);
				MenuController.BindMenuItem(MainMenu, ApplicationMenu, menuItem);
			}
		}
		menu.AddMenuItem(CrewSelectionSearchByAcronymItem);
	}

	private void OnLanguageChanged(object sender, LanguageChangedEventArgs e)
	{
	}
}
