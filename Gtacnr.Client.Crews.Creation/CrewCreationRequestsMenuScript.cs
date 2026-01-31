using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.Crews.Creation;

public class CrewCreationRequestsMenuScript : Script
{
	private static MenuItem PreviousPageItem = new MenuItem("Prev Page");

	private static MenuItem NextPageItem = new MenuItem("Next Page");

	private static int CurrentPage = 0;

	public static Menu MainMenu { get; private set; } = new Menu("Creation Requests");

	public CrewCreationRequestsMenuScript()
	{
		MenuController.AddMenu(MainMenu);
		MainMenu.OnMenuOpen += OnMainMenuOpen;
		MainMenu.OnItemSelect += OnMainItemSelect;
		MainMenu.InstructionalButtons.Add((Control)22, "Copy Id");
		MainMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)22, Menu.ControlPressCheckType.JUST_PRESSED, OnCopyRequestId, disableControl: true));
	}

	private async void OnMainItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == NextPageItem)
		{
			int nextPage = CurrentPage + 1;
			string text = await TriggerServerEventAsync<string>("gtacnr:crews:creation:getAllRequests", new object[1] { nextPage });
			if (string.IsNullOrEmpty(text))
			{
				Utils.PlayErrorSound();
				return;
			}
			List<CrewCreationRequest> list = text.Unjson<List<CrewCreationRequest>>();
			if (list.Count == 0)
			{
				Utils.PlayErrorSound();
				return;
			}
			CurrentPage = nextPage;
			MainMenu.ClearMenuItems();
			foreach (CrewCreationRequest item2 in list)
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
		string text = await TriggerServerEventAsync<string>("gtacnr:crews:creation:getAllRequests", new object[1] { CurrentPage });
		if (string.IsNullOrWhiteSpace(text))
		{
			menu.ClearMenuItems();
			menu.AddMenuItem(new MenuItem("Error", "Failed to load crew creation requests."));
			return;
		}
		List<CrewCreationRequest> list = text.Unjson<List<CrewCreationRequest>>();
		menu.ClearMenuItems();
		if (list.Count == 0)
		{
			menu.AddMenuItem(new MenuItem("No Requests", "You have no pending crew creation requests."));
			return;
		}
		foreach (CrewCreationRequest item2 in list)
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

	private async void OnCopyRequestId(Menu menu, Control control)
	{
		if (menu.GetCurrentMenuItem().ItemData is CrewCreationRequest crewCreationRequest)
		{
			await Utils.GetUserInput("Crew creation id", "Post this id in support ticket for more information about your request", "", 2048, "text", crewCreationRequest.Id.ToString());
		}
	}
}
