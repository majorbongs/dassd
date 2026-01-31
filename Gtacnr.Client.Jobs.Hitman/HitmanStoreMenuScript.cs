using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses;
using Gtacnr.Data;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Hitman;

public class HitmanStoreMenuScript : Script
{
	private bool isBusy;

	private MenuItem civMenuItem;

	private MenuItem hitmanMenuItem;

	private MenuItem newContractMenuItem = new MenuItem("Order a hit contract");

	protected override void OnStarted()
	{
		ShoppingScript.AddExternalItemSelectHandler(OnItemSelect);
		ShoppingScript.AddExternalMenuItem(BusinessType.Hitman, newContractMenuItem);
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
			ShoppingScript.RemoveExternalMenuItem(BusinessType.Hitman, civMenuItem);
		}
		if (hitmanMenuItem != null)
		{
			ShoppingScript.RemoveExternalMenuItem(BusinessType.Hitman, hitmanMenuItem);
		}
		civMenuItem = Gtacnr.Data.Jobs.GetJobData("none").ToSwitchMenuItem();
		hitmanMenuItem = Gtacnr.Data.Jobs.GetJobData("hitman").ToSwitchMenuItem();
		if (Gtacnr.Client.API.Jobs.CachedJob == "hitman")
		{
			ShoppingScript.AddExternalMenuItem(BusinessType.Hitman, civMenuItem);
		}
		else
		{
			ShoppingScript.AddExternalMenuItem(BusinessType.Hitman, hitmanMenuItem);
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
		else if (menuItem == hitmanMenuItem)
		{
			if (!isBusy)
			{
				await Gtacnr.Client.API.Jobs.TrySwitch("hitman", "default", null, BeforeSwitching, AfterSwitching);
			}
		}
		else if (menuItem == newContractMenuItem)
		{
			MenuController.CloseAllMenus();
			HitmanContractMenuScript.ShowMenu(menu, onSite: true);
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
