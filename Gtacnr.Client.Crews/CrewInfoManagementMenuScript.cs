using System;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Crews;

public sealed class CrewInfoManagementMenuScript : Script
{
	private static MenuItem AcronymStyleDataMenuItem = new MenuItem("Acronym Style", "Customize the style for displaying crew acronyms");

	private static Menu AcronymStyleDataMenu = new Menu("Customize Acronym");

	private static MenuItem AcronymSeparatorsMenuItem = new MenuItem("Acronym Separators", "Select the separator for displaying crew acronyms");

	private static MenuItem AcronymStylesMenuItem = new MenuItem("Acronym Styles", "Select the style for displaying crew acronyms");

	private static Menu AcronymSeparatorsMenu = new Menu("Acronym Separators");

	private static Menu AcronymStylesMenu = new Menu("Acronym Styles");

	private static bool isBusy = false;

	public static Menu MainMenu { get; private set; } = new Menu("Crew Info Management", "Manage your crew's information");

	public CrewInfoManagementMenuScript()
	{
		MenuController.AddMenu(MainMenu);
		MainMenu.AddMenuItem(AcronymStyleDataMenuItem);
		MenuController.AddMenu(AcronymStyleDataMenu);
		MenuController.BindMenuItem(MainMenu, AcronymStyleDataMenu, AcronymStyleDataMenuItem);
		AcronymStyleDataMenu.AddMenuItem(AcronymSeparatorsMenuItem);
		MenuController.BindMenuItem(AcronymStyleDataMenu, AcronymSeparatorsMenu, AcronymSeparatorsMenuItem);
		AcronymSeparatorsMenu.OnMenuOpen += OnAcronymSeparatorsMenuOpen;
		AcronymSeparatorsMenu.OnItemSelect += OnAcronymSeparatorItemSelect;
		AcronymStyleDataMenu.AddMenuItem(AcronymStylesMenuItem);
		MenuController.BindMenuItem(AcronymStyleDataMenu, AcronymStylesMenu, AcronymStylesMenuItem);
		AcronymStylesMenu.OnMenuOpen += OnAcronymStylesMenuOpen;
		AcronymStylesMenu.OnItemSelect += OnAcronymStylesItemSelect;
		LocalizationController.LanguageChanged += OnLanguageChanged;
	}

	private async void OnAcronymStylesItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		AcronymStyle style = (AcronymStyle)menuItem.ItemData;
		if (isBusy)
		{
			return;
		}
		try
		{
			isBusy = true;
			CrewsModifyAcronymStyleResponse crewsModifyAcronymStyleResponse = (CrewsModifyAcronymStyleResponse)(await TriggerServerEventAsync<int>("gtacnr:crews:updateAcronymStyle", new object[1] { (int)style }));
			if (crewsModifyAcronymStyleResponse == CrewsModifyAcronymStyleResponse.Success)
			{
				CrewScript.CrewData.AcronymStyle.Style = style;
				OnAcronymStylesMenuOpen(menu);
			}
			else
			{
				Utils.DisplayErrorMessage(0, -1, crewsModifyAcronymStyleResponse.ToString());
				Utils.PlayErrorSound();
			}
		}
		finally
		{
			isBusy = false;
		}
	}

	private async void OnAcronymSeparatorItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		AcronymStyleSeparator separator = (AcronymStyleSeparator)menuItem.ItemData;
		if (isBusy)
		{
			return;
		}
		try
		{
			isBusy = true;
			CrewsModifyAcronymStyleResponse crewsModifyAcronymStyleResponse = (CrewsModifyAcronymStyleResponse)(await TriggerServerEventAsync<int>("gtacnr:crews:updateAcronymSeparator", new object[1] { (int)separator }));
			if (crewsModifyAcronymStyleResponse == CrewsModifyAcronymStyleResponse.Success)
			{
				CrewScript.CrewData.AcronymStyle.Separator = separator;
				OnAcronymSeparatorsMenuOpen(menu);
			}
			else
			{
				Utils.DisplayErrorMessage(0, -1, crewsModifyAcronymStyleResponse.ToString());
				Utils.PlayErrorSound();
			}
		}
		finally
		{
			isBusy = false;
		}
	}

	private void OnAcronymStylesMenuOpen(Menu menu)
	{
		AcronymStylesMenu.ClearMenuItems();
		AcronymStyle[] obj = (AcronymStyle[])Enum.GetValues(typeof(AcronymStyle));
		AcronymStyle style = CrewScript.CrewData.AcronymStyle.Style;
		AcronymStyle[] array = obj;
		for (int i = 0; i < array.Length; i++)
		{
			AcronymStyle acronymStyle = array[i];
			MenuItem item = new MenuItem(acronymStyle.ToString())
			{
				Enabled = (acronymStyle != style),
				ItemData = acronymStyle
			};
			AcronymStylesMenu.AddMenuItem(item);
		}
	}

	private void OnAcronymSeparatorsMenuOpen(Menu menu)
	{
		AcronymSeparatorsMenu.ClearMenuItems();
		AcronymStyleSeparator[] obj = (AcronymStyleSeparator[])Enum.GetValues(typeof(AcronymStyleSeparator));
		AcronymStyleSeparator separator = CrewScript.CrewData.AcronymStyle.Separator;
		AcronymStyleSeparator[] array = obj;
		for (int i = 0; i < array.Length; i++)
		{
			AcronymStyleSeparator acronymStyleSeparator = array[i];
			MenuItem item = new MenuItem(acronymStyleSeparator.ToString())
			{
				Enabled = (acronymStyleSeparator != separator),
				ItemData = acronymStyleSeparator
			};
			AcronymSeparatorsMenu.AddMenuItem(item);
		}
	}

	private void OnLanguageChanged(object sender, LanguageChangedEventArgs e)
	{
	}
}
