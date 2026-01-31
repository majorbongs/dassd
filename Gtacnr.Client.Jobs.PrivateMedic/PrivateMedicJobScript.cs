using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses;
using Gtacnr.Data;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Jobs.PrivateMedic;

public class PrivateMedicJobScript : Script
{
	private bool isBusy;

	private MenuItem civMenuItem;

	private MenuItem mdMenuItem;

	protected override void OnStarted()
	{
		ShoppingScript.AddExternalItemSelectHandler(OnItemSelect);
	}

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		Refresh();
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
		Users.XPChanged += OnXPChanged;
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		Refresh();
	}

	private void OnXPChanged(object sender, Users.XPEventArgs e)
	{
		Refresh();
	}

	private void Refresh()
	{
		if (civMenuItem != null)
		{
			ShoppingScript.RemoveExternalMenuItem(BusinessType.University, civMenuItem);
		}
		if (mdMenuItem != null)
		{
			ShoppingScript.RemoveExternalMenuItem(BusinessType.University, mdMenuItem);
		}
		civMenuItem = Gtacnr.Data.Jobs.GetJobData("none").ToSwitchMenuItem();
		mdMenuItem = Gtacnr.Data.Jobs.GetJobData("privateMedic").ToSwitchMenuItem();
		if (Gtacnr.Client.API.Jobs.CachedJob == "privateMedic")
		{
			ShoppingScript.AddExternalMenuItem(BusinessType.University, civMenuItem);
		}
		else
		{
			ShoppingScript.AddExternalMenuItem(BusinessType.University, mdMenuItem);
		}
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == civMenuItem)
		{
			if (!isBusy)
			{
				await Gtacnr.Client.API.Jobs.TrySwitch("none", "default", null, BeforeSwitching, AfterSwitching);
			}
		}
		else if (menuItem == mdMenuItem && !isBusy)
		{
			await Gtacnr.Client.API.Jobs.TrySwitch("privateMedic", "default", null, BeforeSwitching, AfterSwitching);
		}
		async Task AfterSwitching()
		{
			await Utils.FadeIn(500);
			MenuController.CloseAllMenus();
			isBusy = false;
		}
		async Task BeforeSwitching()
		{
			isBusy = true;
			MenuController.CloseAllMenus();
			await Utils.FadeOut(500);
		}
	}
}
