using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses;
using Gtacnr.Data;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Jobs.DrugDealer;

public class DrugDealerJobScript : Script
{
	private bool isBusy;

	private MenuItem civMenuItem;

	private MenuItem ddlMenuItem;

	protected override void OnStarted()
	{
		ShoppingScript.AddExternalItemSelectHandler(OnItemSelect);
	}

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		Refresh();
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnbChangedEvent;
		Users.XPChanged += OnXPChanged;
	}

	private void OnbChangedEvent(object sender, JobArgs e)
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
			ShoppingScript.RemoveExternalMenuItem(BusinessType.DrugSupplier, civMenuItem);
			ShoppingScript.RemoveExternalMenuItem(BusinessType.WeedDispensary, civMenuItem);
		}
		if (ddlMenuItem != null)
		{
			ShoppingScript.RemoveExternalMenuItem(BusinessType.DrugSupplier, ddlMenuItem);
			ShoppingScript.RemoveExternalMenuItem(BusinessType.WeedDispensary, ddlMenuItem);
		}
		civMenuItem = Gtacnr.Data.Jobs.GetJobData("none").ToSwitchMenuItem();
		ddlMenuItem = Gtacnr.Data.Jobs.GetJobData("drugDealer").ToSwitchMenuItem();
		if (Gtacnr.Client.API.Jobs.CachedJob == "drugDealer")
		{
			ShoppingScript.AddExternalMenuItem(BusinessType.DrugSupplier, civMenuItem);
			ShoppingScript.AddExternalMenuItem(BusinessType.WeedDispensary, civMenuItem);
		}
		else
		{
			ShoppingScript.AddExternalMenuItem(BusinessType.DrugSupplier, ddlMenuItem);
			ShoppingScript.AddExternalMenuItem(BusinessType.WeedDispensary, ddlMenuItem);
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
		else if (menuItem == ddlMenuItem && !isBusy)
		{
			await Gtacnr.Client.API.Jobs.TrySwitch("drugDealer", "default", null, BeforeSwitching, AfterSwitching);
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
