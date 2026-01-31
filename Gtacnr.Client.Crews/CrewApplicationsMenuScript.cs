using System.Collections.Generic;
using Gtacnr.Model;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Crews;

public sealed class CrewApplicationsMenuScript : Script
{
	private static MenuItem PreviousPageItem = new MenuItem("Prev Page", "Load previous page of applications");

	private static MenuItem NextPageItem = new MenuItem("Next Page", "Load the next page of applications");

	private static int CurrentPage = 0;

	private static CrewApplication SelectedApplication;

	private static Menu ActionsMenu = new Menu("Actions", "Select an action");

	private static MenuItem ResponseTextItem = new MenuItem("Response Text", "N/A");

	private static MenuItem ApproveApplicationItem = new MenuItem("Approve Application", "Approve the selected application");

	private static MenuItem RejectApplicationItem = new MenuItem("Reject Application", "Reject the selected application");

	public static Menu MainMenu { get; private set; } = new Menu("Crew Applications", "Manage your crew applications");

	public CrewApplicationsMenuScript()
	{
		MenuController.AddMenu(MainMenu);
		MainMenu.OnMenuOpen += OnMainMenuOpen;
		MainMenu.OnItemSelect += OnMainItemSelect;
		MenuController.AddMenu(ActionsMenu);
		ActionsMenu.AddMenuItem(ResponseTextItem);
		ActionsMenu.AddMenuItem(ApproveApplicationItem);
		ActionsMenu.AddMenuItem(RejectApplicationItem);
		ActionsMenu.OnItemSelect += OnActionsItemSelect;
		ActionsMenu.OnMenuOpen += OnActionsMenuOpen;
	}

	private void OnActionsMenuOpen(Menu menu)
	{
		ResponseTextItem.Description = "N/A";
	}

	private async void OnActionsItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == ApproveApplicationItem || menuItem == RejectApplicationItem)
		{
			string description = ResponseTextItem.Description;
			if (description == "N/A")
			{
				Utils.PlayErrorSound();
				Utils.DisplayHelpText("You have not set a response text. Please set one before responding to the application.");
				return;
			}
			if (description.Length < 5)
			{
				Utils.PlayErrorSound();
				Utils.DisplayHelpText("Response text is too short.");
				return;
			}
			CrewApplicationResponse crewApplicationResponse = ((menuItem == ApproveApplicationItem) ? CrewApplicationResponse.Accepted : CrewApplicationResponse.Rejected);
			CrewRespondToApplicationResponse crewRespondToApplicationResponse = (CrewRespondToApplicationResponse)(await TriggerServerEventAsync<int>("gtacnr:crews:appResponse", new object[3]
			{
				SelectedApplication.Id.ToString(),
				description,
				(int)crewApplicationResponse
			}));
			if (crewRespondToApplicationResponse == CrewRespondToApplicationResponse.Success)
			{
				menu.GoBack();
				Utils.PlaySelectSound();
			}
			else
			{
				Utils.DisplayErrorMessage(0, -1, crewRespondToApplicationResponse.ToString());
				Utils.PlayErrorSound();
			}
		}
		else if (menuItem == ResponseTextItem)
		{
			string text = await Utils.GetUserInput("Response Text", "Enter your response text (optional):", "", 200);
			if (text != null)
			{
				SelectedApplication.ResponseText = text;
				ResponseTextItem.Description = (string.IsNullOrWhiteSpace(text) ? "N/A" : text);
				Utils.PlaySelectSound();
			}
		}
	}

	private async void OnMainItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem.ItemData is CrewApplication selectedApplication)
		{
			SelectedApplication = selectedApplication;
		}
		else if (menuItem == NextPageItem)
		{
			int nextPage = CurrentPage + 1;
			string text = await TriggerServerEventAsync<string>("gtacnr:crews:getAllApplications", new object[1] { nextPage });
			if (string.IsNullOrEmpty(text))
			{
				Utils.PlayErrorSound();
				return;
			}
			List<CrewApplication> list = text.Unjson<List<CrewApplication>>();
			if (list.Count == 0)
			{
				Utils.PlayErrorSound();
				return;
			}
			CurrentPage = nextPage;
			MainMenu.ClearMenuItems();
			foreach (CrewApplication item in list)
			{
				MenuItem menuItem2 = item.ToMenuItem();
				MainMenu.AddMenuItem(menuItem2);
				MenuController.BindMenuItem(MainMenu, ActionsMenu, menuItem2);
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
		string text = await TriggerServerEventAsync<string>("gtacnr:crews:getAllApplications", new object[1] { CurrentPage });
		if (string.IsNullOrWhiteSpace(text))
		{
			menu.ClearMenuItems();
			menu.AddMenuItem(new MenuItem("Error", "Failed to load crew applications."));
			return;
		}
		List<CrewApplication> list = text.Unjson<List<CrewApplication>>();
		menu.ClearMenuItems();
		if (list.Count == 0)
		{
			menu.AddMenuItem(new MenuItem("No Applications", "You have no pending crew applications."));
			return;
		}
		foreach (CrewApplication item in list)
		{
			MenuItem menuItem = item.ToMenuItem();
			MainMenu.AddMenuItem(menuItem);
			MenuController.BindMenuItem(MainMenu, ActionsMenu, menuItem);
		}
		if (CurrentPage > 1)
		{
			MainMenu.AddMenuItem(PreviousPageItem);
		}
		MainMenu.AddMenuItem(NextPageItem);
	}
}
