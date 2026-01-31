using System.Collections.Generic;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.Crews;

public sealed class CrewLogsMenuScript : Script
{
	private static MenuItem PreviousPageItem = new MenuItem("Prev Page", "Load previous page of applications");

	private static MenuItem NextPageItem = new MenuItem("Next Page", "Load the next page of applications");

	private static int CurrentPage = 0;

	public static Menu MainMenu { get; private set; } = new Menu("Crew Logs");

	public CrewLogsMenuScript()
	{
		MenuController.AddMenu(MainMenu);
		MainMenu.OnMenuOpen += OnMainMenuOpen;
		MainMenu.OnItemSelect += OnMainItemSelect;
	}

	private async void OnMainItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == NextPageItem)
		{
			int nextPage = CurrentPage + 1;
			string text = await TriggerServerEventAsync<string>("gtacnr:crews:getAllLogs", new object[1] { nextPage });
			if (string.IsNullOrEmpty(text))
			{
				Utils.PlayErrorSound();
				return;
			}
			List<CrewLog> list = text.Unjson<List<CrewLog>>();
			if (list.Count == 0)
			{
				Utils.PlayErrorSound();
				return;
			}
			CurrentPage = nextPage;
			MainMenu.ClearMenuItems();
			foreach (CrewLog item2 in list)
			{
				MenuItem item = item2.ToMenuItem();
				MainMenu.AddMenuItem(item);
			}
			if (CurrentPage > 0)
			{
				MainMenu.AddMenuItem(PreviousPageItem);
			}
			MainMenu.AddMenuItem(NextPageItem);
			MainMenu.MenuSubtitle = $"Page {CurrentPage + 1}";
		}
		else if (menuItem == PreviousPageItem)
		{
			if (CurrentPage <= 0)
			{
				Utils.PlayErrorSound();
				return;
			}
			CurrentPage--;
			MainMenu.MenuSubtitle = $"Page {CurrentPage + 1}";
			MainMenu.OpenMenu();
		}
	}

	private async void OnMainMenuOpen(Menu menu)
	{
		menu.ClearMenuItems();
		menu.AddLoadingMenuItem();
		menu.MenuSubtitle = $"Page {CurrentPage + 1}";
		string text = await TriggerServerEventAsync<string>("gtacnr:crews:getAllLogs", new object[1] { CurrentPage });
		if (string.IsNullOrWhiteSpace(text))
		{
			menu.ClearMenuItems();
			menu.AddMenuItem(new MenuItem("Error", "Failed to load logs."));
			return;
		}
		List<CrewLog> list = text.Unjson<List<CrewLog>>();
		menu.ClearMenuItems();
		if (list.Count == 0)
		{
			menu.AddMenuItem(new MenuItem("No Logs"));
			return;
		}
		foreach (CrewLog item2 in list)
		{
			MenuItem item = item2.ToMenuItem();
			MainMenu.AddMenuItem(item);
		}
		if (CurrentPage > 1)
		{
			MainMenu.AddMenuItem(PreviousPageItem);
		}
		MainMenu.AddMenuItem(NextPageItem);
	}
}
