using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses;
using Gtacnr.Data;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Jobs.ArmsDealer;

public class ArmsDealerJobScript : Script
{
	private bool isBusy;

	private MenuItem civMenuItem;

	private MenuItem adMenuItem;

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
			ShoppingScript.RemoveExternalMenuItem(BusinessType.IllegalGunStore, civMenuItem);
		}
		if (adMenuItem != null)
		{
			ShoppingScript.RemoveExternalMenuItem(BusinessType.IllegalGunStore, adMenuItem);
		}
		civMenuItem = Gtacnr.Data.Jobs.GetJobData("none").ToSwitchMenuItem();
		adMenuItem = Gtacnr.Data.Jobs.GetJobData("armsDealer").ToSwitchMenuItem();
		if (Gtacnr.Client.API.Jobs.CachedJob == "armsDealer")
		{
			ShoppingScript.AddExternalMenuItem(BusinessType.IllegalGunStore, civMenuItem);
		}
		else
		{
			ShoppingScript.AddExternalMenuItem(BusinessType.IllegalGunStore, adMenuItem);
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
		else if (menuItem == adMenuItem && !isBusy)
		{
			await Gtacnr.Client.API.Jobs.TrySwitch("armsDealer", "default", null, BeforeSwitching, AfterSwitching);
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
