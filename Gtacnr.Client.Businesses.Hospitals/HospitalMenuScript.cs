using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Premium;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Businesses.Hospitals;

public class HospitalMenuScript : Script
{
	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private bool canOpenMenu;

	private bool isBusy;

	private bool? canHavePlasticSurgery;

	private bool isRefreshing;

	protected override void OnStarted()
	{
		ShoppingScript.AddExternalItemSelectHandler(OnMenuItemSelect);
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
		Users.XPChanged += OnXPChanged;
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		RefreshMenu();
	}

	private void OnXPChanged(object sender, Users.XPEventArgs e)
	{
		RefreshMenu();
	}

	[EventHandler("gtacnr:crimes:wantedLevelChanged")]
	private void OnWantedLevelChanged(int oldLevel, int newLevel)
	{
		RefreshMenu();
	}

	[EventHandler("gtacnr:membershipUpdated")]
	private void OnMembershipUpdated(int tier)
	{
		canHavePlasticSurgery = null;
	}

	private async void RefreshMenu()
	{
		if (isRefreshing)
		{
			return;
		}
		try
		{
			isRefreshing = true;
			string[] array = new string[7] { "vehicles", "heal", "cure", "surgery", "toCivilian", "toParamedic", "toPrivateMedic" };
			foreach (string key in array)
			{
				menuItems.Remove(key);
			}
			ShoppingScript.ClearExternalMenuItems(BusinessType.Hospital);
			long money = await Money.GetCachedBalanceOrFetch(AccountType.Cash);
			Hospital hospital;
			do
			{
				hospital = HospitalScript.CurrentHospital;
				await BaseScript.Delay(0);
			}
			while (hospital == null);
			while (!Gtacnr.Client.API.Crime.CachedWantedLevel.HasValue || Gtacnr.Client.API.Jobs.CachedJobEnum == JobsEnum.Invalid)
			{
				await BaseScript.Delay(10);
			}
			if (Gtacnr.Client.API.Jobs.CachedJob == "paramedic" && hospital.Dealership != null && DealershipScript.GetDealershipById(hospital.Dealership) != null)
			{
				menuItems["vehicles"] = new MenuItem("~b~Paramedic Vehicles", "Purchase ~y~vehicles ~s~for the ~b~paramedic ~s~job.")
				{
					RightIcon = MenuItem.Icon.GTACNR_VEHICLES
				};
				ShoppingScript.AddExternalMenuItem(BusinessType.Hospital, menuItems["vehicles"], before: true);
			}
			int num = Gtacnr.Utils.CalculateHealPrice();
			bool flag = API.GetEntityHealth(API.PlayerPedId()) < API.GetPedMaxHealth(API.PlayerPedId());
			menuItems["heal"] = new MenuItem("Heal", "Restore your ~r~health~s~.")
			{
				Label = (flag ? num.ToPriceTagString(money, naWhenZero: true) : "FULL"),
				Enabled = (flag && money >= num)
			};
			ShoppingScript.AddExternalMenuItem(BusinessType.Hospital, menuItems["heal"], before: true);
			int num2 = Gtacnr.Utils.CalculateCurePrice();
			bool flag2 = num2 > 0;
			menuItems["cure"] = new MenuItem("Cure", "Get your ~r~diseases ~s~cured.")
			{
				Label = (flag2 ? num2.ToPriceTagString(money, naWhenZero: true) : LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT)),
				Enabled = (flag2 && money >= num2)
			};
			ShoppingScript.AddExternalMenuItem(BusinessType.Hospital, menuItems["cure"], before: true);
			if (!hospital.IsCayo)
			{
				MembershipTier tier = MembershipScript.GetCurrentMembershipTier();
				string memberMsg = "\n~p~Free for Premium Members";
				if ((int)tier >= 1)
				{
					memberMsg = "\n~p~Free with your " + Gtacnr.Utils.GetDescription(tier);
				}
				if (!canHavePlasticSurgery.HasValue)
				{
					canHavePlasticSurgery = await TriggerServerEventAsync<bool>("gtacnr:canHavePlasticSurgery", new object[0]);
				}
				menuItems["surgery"] = new MenuItem("Plastic surgery")
				{
					Description = "Change your character's appearance and sex." + memberMsg,
					Label = (((int)tier >= 1) ? "~p~FREE" : "~p~1 Surgery Token"),
					Enabled = canHavePlasticSurgery.Value,
					RightIcon = ((!canHavePlasticSurgery.Value) ? MenuItem.Icon.LOCK : MenuItem.Icon.NONE)
				};
				if (!canHavePlasticSurgery.Value)
				{
					MenuItem menuItem = menuItems["surgery"];
					menuItem.Description = menuItem.Description + "\n~s~Purchase at ~b~" + ExternalLinks.Collection.Store;
				}
				ShoppingScript.AddExternalMenuItem(BusinessType.Hospital, menuItems["surgery"], before: true);
				if (Gtacnr.Client.API.Jobs.CachedJob == "paramedic")
				{
					AddToPrivateMedicItem();
					AddToCivilianItem();
				}
				else if (Gtacnr.Client.API.Jobs.CachedJob == "privateMedic")
				{
					AddToParamedicItem();
					AddToCivilianItem();
				}
				else
				{
					AddToParamedicItem();
					AddToPrivateMedicItem();
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			isRefreshing = false;
		}
		void AddToCivilianItem()
		{
			menuItems["toCivilian"] = Gtacnr.Data.Jobs.GetJobData("none").ToSwitchMenuItem();
			ShoppingScript.AddExternalMenuItem(BusinessType.Hospital, menuItems["toCivilian"]);
		}
		void AddToParamedicItem()
		{
			menuItems["toParamedic"] = Gtacnr.Data.Jobs.GetJobData("paramedic").ToSwitchMenuItem();
			ShoppingScript.AddExternalMenuItem(BusinessType.Hospital, menuItems["toParamedic"]);
		}
		void AddToPrivateMedicItem()
		{
			menuItems["toPrivateMedic"] = Gtacnr.Data.Jobs.GetJobData("privateMedic").ToSwitchMenuItem();
			ShoppingScript.AddExternalMenuItem(BusinessType.Hospital, menuItems["toPrivateMedic"]);
		}
	}

	private async void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (IsSelected("heal"))
		{
			MenuController.CloseAllMenus();
			ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:ems:buyHeal");
			switch (responseCode)
			{
			case ResponseCode.Success:
				Utils.ClearPedDamage(Game.PlayerPed);
				lock (AntiHealthLockScript.HealThreadLock)
				{
					AntiHealthLockScript.JustHealed();
					((Entity)Game.PlayerPed).HealthFloat = API.GetPedMaxHealth(API.PlayerPedId());
				}
				Utils.DisplayHelpText("You have been ~p~healed ~s~at the hospital.");
				RefreshMenu();
				break;
			case ResponseCode.InsufficientMoney:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY));
				break;
			case ResponseCode.TooFar:
				Utils.DisplayHelpText("~r~You are too far from the hospital!");
				break;
			default:
				Utils.DisplayError(responseCode, "", "OnMenuItemSelect");
				break;
			}
			return;
		}
		if (IsSelected("cure"))
		{
			MenuController.CloseAllMenus();
			ResponseCode responseCode2 = await TriggerServerEventAsync("gtacnr:ems:buyCure");
			switch (responseCode2)
			{
			case ResponseCode.Success:
				Utils.DisplayHelpText("You have been ~p~cured ~s~at the hospital.");
				RefreshMenu();
				break;
			case ResponseCode.InsufficientMoney:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY));
				break;
			case ResponseCode.TooFar:
				Utils.DisplayHelpText("~r~You are too far from the hospital!");
				break;
			default:
				Utils.DisplayError(responseCode2, "", "OnMenuItemSelect");
				break;
			}
			return;
		}
		if (IsSelected("surgery"))
		{
			try
			{
				if (!HospitalScript.CurrentHospital.IsPillbox)
				{
					Utils.DisplayHelpText("Please go to ~b~Pillbox Hill Medical Center ~s~(Downtown Los Santos) for plastic surgery.", playSound: false);
					Utils.PlayErrorSound();
				}
				else if (!canHavePlasticSurgery.Value)
				{
					Utils.PlayErrorSound();
				}
				else if (await Gtacnr.Client.API.Crime.GetWantedLevel() > 0)
				{
					Utils.DisplayHelpText("You are ~o~wanted ~s~by the police! Please, come back later.", playSound: false);
					Utils.PlayErrorSound();
				}
				else if ((int)MembershipScript.MembershipTier >= 1 || await Utils.ShowConfirm("Do you really want to enter the ~y~plastic surgery ~s~room? Since you are not a ~p~Premium Member~s~, this will cost a ~p~Surgery Token~s~. The token won't be taken until the surgery is complete and confirmed.", "Plastic Surgery", TimeSpan.FromSeconds(3.0)))
				{
					int num = await TriggerServerEventAsync<int>("gtacnr:startPlasticSurgery", new object[0]);
					if (num == 1)
					{
						MenuController.CloseAllMenus();
						BaseScript.TriggerEvent("gtacnr:characters:enterEditMode", new object[0]);
					}
					else
					{
						Utils.DisplayErrorMessage(147, num);
					}
				}
				return;
			}
			catch (Exception exception)
			{
				Print(exception);
				Utils.DisplayErrorMessage(148);
				return;
			}
		}
		if (IsSelected("vehicles"))
		{
			Hospital currentHospital = HospitalScript.CurrentHospital;
			if (currentHospital.Dealership != null)
			{
				Dealership dealershipById = DealershipScript.GetDealershipById(currentHospital.Dealership);
				if (dealershipById == null)
				{
					Utils.PlayErrorSound();
					return;
				}
				MenuController.CloseAllMenus();
				DealershipMenuScript.OpenMenu(dealershipById);
			}
		}
		else if (IsSelected("toCivilian"))
		{
			if (!isBusy)
			{
				await Gtacnr.Client.API.Jobs.TrySwitch("none", "default", null, BeforeSwitching, AfterSwitching);
			}
		}
		else if (IsSelected("toParamedic"))
		{
			if (!isBusy)
			{
				await Gtacnr.Client.API.Jobs.TrySwitch("paramedic", HospitalScript.CurrentHospital.Department, LocalizationController.S(Entries.Jobs.PARAMEDIC_TEAMING_WARNING), BeforeSwitching, AfterSwitching);
			}
		}
		else if (IsSelected("toPrivateMedic") && !isBusy)
		{
			await Gtacnr.Client.API.Jobs.TrySwitch("privateMedic", "default", LocalizationController.S(Entries.Jobs.PRIVATE_MEDIC_DESCRIPTION), BeforeSwitching, AfterSwitching);
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
		bool IsSelected(string key)
		{
			if (menuItems.ContainsKey(key))
			{
				return menuItem == menuItems[key];
			}
			return false;
		}
	}
}
