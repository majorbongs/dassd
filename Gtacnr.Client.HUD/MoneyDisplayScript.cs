using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.HUD;

public class MoneyDisplayScript : Script
{
	private DateTime lastPressTime = DateTime.MinValue;

	private DateTime lastCashUpdateTime = DateTime.MinValue;

	private DateTime lastBankUpdateTime = DateTime.MinValue;

	private long? actualOldCash;

	private long? actualOldBank;

	private bool isCashShown;

	private bool isBankShown;

	private bool isCashChangeShown;

	private bool isBankChangeShown;

	public static bool ForceMoneyDisplay { get; set; }

	protected override void OnStarted()
	{
		bool flag = Preferences.ThousandsSeparator.Get();
		BaseScript.TriggerEvent("gtacnr:hud:toggleThousandsSeparator", new object[1] { flag });
	}

	[Update]
	private async Coroutine RefreshTask()
	{
		if ((API.IsControlJustPressed(2, 20) || API.IsDisabledControlJustPressed(2, 20)) && !MenuController.IsAnyMenuOpen())
		{
			lastPressTime = DateTime.UtcNow;
		}
		bool flag = Gtacnr.Utils.CheckTimePassed(lastPressTime, 4250.0);
		if (!flag || ForceMoneyDisplay)
		{
			ShowCash();
			ShowBank();
		}
		else if (!ForceMoneyDisplay)
		{
			HideCash(!flag);
			HideBank(!flag);
			HideCashChange(!flag);
			HideBankChange(!flag);
		}
		if (!ForceMoneyDisplay)
		{
			if (Gtacnr.Utils.CheckTimePassed(lastCashUpdateTime, 4500.0))
			{
				actualOldCash = null;
			}
			if (Gtacnr.Utils.CheckTimePassed(lastBankUpdateTime, 4500.0))
			{
				actualOldBank = null;
			}
		}
	}

	public void Display()
	{
		lastPressTime = DateTime.UtcNow;
	}

	[EventHandler("gtacnr:money:moneyUpdated")]
	private void OnMoneyUpdated(string account, long oldBalance, long newBalance)
	{
		if (account == AccountType.Cash)
		{
			if (!actualOldCash.HasValue)
			{
				actualOldCash = oldBalance;
			}
			SetCashChange(newBalance - actualOldCash.Value);
			SetCash(newBalance);
			ShowCashChange();
			Display();
			lastCashUpdateTime = DateTime.UtcNow;
		}
		else if (account == AccountType.Bank)
		{
			if (!actualOldBank.HasValue)
			{
				actualOldBank = oldBalance;
			}
			SetBankChange(newBalance - actualOldBank.Value);
			SetBank(newBalance);
			ShowBankChange();
			Display();
			lastBankUpdateTime = DateTime.UtcNow;
		}
		long num = newBalance - oldBalance;
		Print(AccountType.GetName(account) + ": " + ((num >= 0) ? "^2+" : "^1-") + Math.Abs(num).ToCurrencyString() + "^0");
	}

	[EventHandler("gtacnr:careDebtChanged")]
	private async void OnCareDebtChanged(long totalDebt, long debtChange)
	{
		BaseScript.TriggerEvent("gtacnr:hud:setDebt", new object[1] { totalDebt });
		BaseScript.TriggerEvent("gtacnr:hud:setDebtChange", new object[1] { debtChange });
		BaseScript.TriggerEvent("gtacnr:hud:setDebtPrefix", new object[1] { "care" });
		BaseScript.TriggerEvent("gtacnr:hud:toggleDebt", new object[1] { true });
		BaseScript.TriggerEvent("gtacnr:hud:toggleDebtChange", new object[1] { true });
		await BaseScript.Delay(8000);
		BaseScript.TriggerEvent("gtacnr:hud:toggleDebt", new object[1] { false });
		BaseScript.TriggerEvent("gtacnr:hud:toggleDebtChange", new object[1] { false });
	}

	[EventHandler("gtacnr:bailDebtChanged")]
	private async void BailDebt(long totalDebt, long debtChange)
	{
		BaseScript.TriggerEvent("gtacnr:hud:setDebt", new object[1] { totalDebt });
		BaseScript.TriggerEvent("gtacnr:hud:setDebtChange", new object[1] { debtChange });
		BaseScript.TriggerEvent("gtacnr:hud:setDebtPrefix", new object[1] { "bail" });
		BaseScript.TriggerEvent("gtacnr:hud:toggleDebt", new object[1] { true });
		BaseScript.TriggerEvent("gtacnr:hud:toggleDebtChange", new object[1] { true });
		await BaseScript.Delay(8000);
		BaseScript.TriggerEvent("gtacnr:hud:toggleDebt", new object[1] { false });
		BaseScript.TriggerEvent("gtacnr:hud:toggleDebtChange", new object[1] { false });
	}

	[EventHandler("gtacnr:govDebtChanged")]
	private async void GovDebt(long totalDebt, long debtChange)
	{
		BaseScript.TriggerEvent("gtacnr:hud:setDebt", new object[1] { totalDebt });
		BaseScript.TriggerEvent("gtacnr:hud:setDebtChange", new object[1] { debtChange });
		BaseScript.TriggerEvent("gtacnr:hud:setDebtPrefix", new object[1] { "gov" });
		BaseScript.TriggerEvent("gtacnr:hud:toggleDebt", new object[1] { true });
		BaseScript.TriggerEvent("gtacnr:hud:toggleDebtChange", new object[1] { true });
		await BaseScript.Delay(8000);
		BaseScript.TriggerEvent("gtacnr:hud:toggleDebt", new object[1] { false });
		BaseScript.TriggerEvent("gtacnr:hud:toggleDebtChange", new object[1] { false });
	}

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		await LoadBalance();
		Display();
	}

	[EventHandler("gtacnr:died")]
	private void OnDied()
	{
		ForceMoneyDisplay = false;
	}

	[EventHandler("onClientResourceStart")]
	private async void OnClientResourceStart(string resourceName)
	{
		if (resourceName == "gtacnr_ui" && SpawnScript.HasSpawned)
		{
			await LoadBalance();
			Display();
		}
	}

	private async Task LoadBalance()
	{
		long cash = await Money.GetBalance(AccountType.Cash);
		long num = await Money.GetBalance(AccountType.Bank);
		actualOldCash = cash;
		actualOldBank = num;
		SetCash(cash);
		SetBank(num);
	}

	private void ShowCash(bool fade = true)
	{
		if (!isCashShown)
		{
			isCashShown = true;
			BaseScript.TriggerEvent("gtacnr:hud:toggleCash", new object[2]
			{
				true,
				!fade
			});
		}
	}

	private void HideCash(bool fade = true)
	{
		if (isCashShown)
		{
			isCashShown = false;
			BaseScript.TriggerEvent("gtacnr:hud:toggleCash", new object[2]
			{
				false,
				!fade
			});
		}
	}

	private void ShowBank(bool fade = true)
	{
		if (!isBankShown)
		{
			isBankShown = true;
			BaseScript.TriggerEvent("gtacnr:hud:toggleBank", new object[2]
			{
				true,
				!fade
			});
		}
	}

	private void HideBank(bool fade = true)
	{
		if (isBankShown)
		{
			isBankShown = false;
			BaseScript.TriggerEvent("gtacnr:hud:toggleBank", new object[2]
			{
				false,
				!fade
			});
		}
	}

	private void ShowCashChange(bool fade = true)
	{
		if (!isCashChangeShown)
		{
			isCashChangeShown = true;
			BaseScript.TriggerEvent("gtacnr:hud:toggleCashChange", new object[2]
			{
				true,
				!fade
			});
		}
	}

	private void HideCashChange(bool fade = true)
	{
		if (isCashChangeShown)
		{
			isCashChangeShown = false;
			BaseScript.TriggerEvent("gtacnr:hud:toggleCashChange", new object[2]
			{
				false,
				!fade
			});
		}
	}

	private void ShowBankChange(bool fade = true)
	{
		if (!isBankChangeShown)
		{
			isBankChangeShown = true;
			BaseScript.TriggerEvent("gtacnr:hud:toggleBankChange", new object[2]
			{
				true,
				!fade
			});
		}
	}

	private void HideBankChange(bool fade = true)
	{
		if (isBankChangeShown)
		{
			isBankChangeShown = false;
			BaseScript.TriggerEvent("gtacnr:hud:toggleBankChange", new object[2]
			{
				false,
				!fade
			});
		}
	}

	private void SetCash(long amount)
	{
		if (amount < 0)
		{
			amount = 0L;
		}
		BaseScript.TriggerEvent("gtacnr:hud:setCash", new object[1] { amount });
	}

	private void SetBank(long amount)
	{
		if (amount < 0)
		{
			amount = 0L;
		}
		BaseScript.TriggerEvent("gtacnr:hud:setBank", new object[1] { amount });
	}

	private void SetCashChange(long amount)
	{
		BaseScript.TriggerEvent("gtacnr:hud:setCashChange", new object[1] { amount });
	}

	private void SetBankChange(long amount)
	{
		BaseScript.TriggerEvent("gtacnr:hud:setBankChange", new object[1] { amount });
	}
}
