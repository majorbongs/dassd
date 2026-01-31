using System.Linq;
using CitizenFX.Core;
using Gtacnr.Data;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.Items;

public class ScratchCardScript : Script
{
	public static ScratchCardScript instance;

	private ScratchCard currentScratchCard;

	private Menu scratchCardMenu;

	public static Menu Menu => instance.scratchCardMenu;

	public ScratchCardScript()
	{
		instance = this;
	}

	protected override void OnStarted()
	{
		scratchCardMenu = new Menu("Scratch Card");
		scratchCardMenu.OnItemSelect += OnMenuItemSelect;
		scratchCardMenu.OnMenuClose += OnMenuClose;
		scratchCardMenu.PlaySelectSound = false;
		scratchCardMenu.InstructionalButtons.Clear();
		scratchCardMenu.InstructionalButtons.Add((Control)201, "Scratch");
		scratchCardMenu.InstructionalButtons.Add((Control)202, "Discard");
	}

	private void OnMenuClose(Menu menu, MenuClosedEventArgs e)
	{
		if (menu == scratchCardMenu && currentScratchCard != null)
		{
			BaseScript.TriggerServerEvent("gtacnr:gambling:cancelScratchCard", new object[0]);
		}
	}

	private void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menu != scratchCardMenu)
		{
			return;
		}
		if ((bool)menuItem.ItemData || currentScratchCard == null)
		{
			Utils.PlayErrorSound();
			return;
		}
		bool flag = currentScratchCard.WinningNumbers.Contains(currentScratchCard.Numbers[itemIndex]);
		menuItem.ItemData = true;
		string text = "~s~";
		if (flag)
		{
			text = "~y~";
		}
		menuItem.Text = text + currentScratchCard.Numbers[itemIndex];
		if (menu.GetMenuItems().All((MenuItem i) => (bool)i.ItemData))
		{
			if (currentScratchCard.CalculatePrize() > 0)
			{
				Game.PlaySound("ROBBERY_MONEY_TOTAL", "HUD_FRONTEND_CUSTOM_SOUNDSET");
				PlayWinAnim();
			}
			else
			{
				Game.PlaySound("LOSER", "HUD_AWARDS");
			}
			menu.GetMenuItems().ForEach(delegate(MenuItem i)
			{
				i.Description = GetMenuDescription(completed: true);
			});
			BaseScript.TriggerServerEvent("gtacnr:gambling:endScratchCard", new object[0]);
			currentScratchCard = null;
		}
		else
		{
			if (flag)
			{
				Game.PlaySound("LOCAL_PLYR_CASH_COUNTER_INCREASE", "DLC_HEISTS_GENERAL_FRONTEND_SOUNDS");
			}
			else
			{
				Game.PlaySound("3_2_1", "HUD_MINI_GAME_SOUNDSET");
			}
			Utils.ShakeGamepad(100);
			PlayScratchAnim();
		}
	}

	private string GetMenuDescription(bool completed = false)
	{
		string text = "Winning Numbers~n~ " + string.Join(", ", currentScratchCard.WinningNumbers.Select((int n) => $"~y~{n}~s~")) + "~n~" + $"Numbers from 1 to {currentScratchCard.NumMax}~n~Prizes";
		int num = 0;
		foreach (int prize in currentScratchCard.Prizes)
		{
			num++;
			if (prize != 0)
			{
				text += $"~n~~s~{num} matches: ~g~{prize.ToCurrencyString()}";
			}
		}
		if (completed)
		{
			int num2 = currentScratchCard.CalculatePrize();
			if (num2 > 0)
			{
				return text + "~n~~y~Congratulations! ~s~You won ~y~" + num2.ToCurrencyString() + ".";
			}
			return text + "~n~~r~No win, sorry! Try again!";
		}
		return text + "~n~~g~Good Luck!";
	}

	[EventHandler("gtacnr:gambling:startScratchCard")]
	private void StartScratchCard(string scratchCardJson)
	{
		currentScratchCard = scratchCardJson.Unjson<ScratchCard>();
		MenuController.CloseAllMenus();
		scratchCardMenu.ClearMenuItems();
		scratchCardMenu.MenuSubtitle = currentScratchCard.Name;
		string menuDescription = GetMenuDescription();
		for (int i = 0; i < currentScratchCard.Numbers.Count; i++)
		{
			scratchCardMenu.AddMenuItem(new MenuItem("~c~Scratch", menuDescription)
			{
				ItemData = false
			});
		}
		scratchCardMenu.OpenMenu();
	}

	[EventHandler("gtacnr:inventories:usingItem")]
	private void OnUsingItem(string itemId, float amount)
	{
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (itemDefinition.ExtraData.ContainsKey("IsScratchCard") && (bool)itemDefinition.ExtraData["IsScratchCard"])
		{
			MenuController.CloseAllMenus();
			scratchCardMenu.ClearMenuItems();
			scratchCardMenu.MenuSubtitle = "";
			scratchCardMenu.AddLoadingMenuItem();
			scratchCardMenu.OpenMenu();
		}
	}

	private async void PlayScratchAnim()
	{
		Weapon current = Game.PlayerPed.Weapons.Current;
		bool flag = Game.PlayerPed.IsInVehicle();
		if (!((int)Weapon.op_Implicit(current) != -1569615261 || flag))
		{
			int num = 1000;
			string animDict = "anim@amb@board_room@supervising@";
			string animName = "dissaproval_01_lo_amy_skater_01";
			Game.PlayerPed.Task.PlayAnimation(animDict, animName, 4f, num, (AnimationFlags)51);
			await BaseScript.Delay(num);
			Game.PlayerPed.Task.ClearAnimation(animDict, animName);
		}
	}

	private async void PlayWinAnim()
	{
		Weapon current = Game.PlayerPed.Weapons.Current;
		bool flag = Game.PlayerPed.IsInVehicle();
		if (!((int)Weapon.op_Implicit(current) != -1569615261 || flag))
		{
			int num = 3000;
			string animDict = "anim_casino_a@amb@casino@games@arcadecabinet@maleleft";
			string animName = "win_big";
			Game.PlayerPed.Task.PlayAnimation(animDict, animName, 4f, num, (AnimationFlags)51);
			await BaseScript.Delay(num);
			Game.PlayerPed.Task.ClearAnimation(animDict, animName);
		}
	}
}
