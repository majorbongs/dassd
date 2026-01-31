using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Libs;
using MenuAPI;

namespace Gtacnr.Client.HUD;

public class CustomHUDOffsetScript : Script
{
	private Menu offsetMenu;

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private bool autoOpened;

	private bool anythingChanged;

	private static CustomHUDOffsetScript instance;

	public CustomHUDOffsetScript()
	{
		instance = this;
	}

	[EventHandler("gtacnr:spawned")]
	private void OnSpawned(Vector4 coords)
	{
		if (Preferences.MenuOffsetSet.Get())
		{
			BetterDrawText.CustomOffsetX = Preferences.MenuOffsetX.Get();
			BetterDrawText.CustomOffsetY = Preferences.MenuOffsetY.Get();
			BetterDrawText.CustomOffsetW = Preferences.MenuOffsetW.Get();
			MenuItem.RowSpacing = Preferences.MenuOffsetH.Get();
		}
		else
		{
			autoOpened = true;
			StartSetMenuOffsets();
		}
	}

	public static void OpenMenu(Menu parentMenu)
	{
		instance.StartSetMenuOffsets(parentMenu);
	}

	private void StartSetMenuOffsets(Menu parentMenu = null)
	{
		if (parentMenu != null)
		{
			MenuController.CloseAllMenus();
		}
		offsetMenu = new Menu("Menu Offsets", "Adjust your menus");
		offsetMenu.OnMenuClose += OnMenuClose;
		offsetMenu.OnSliderItemSelect += OnMenuSliderItemSelect;
		offsetMenu.OnSliderPositionChange += OnMenuSliderPositionChange;
		if (parentMenu != null)
		{
			offsetMenu.ParentMenu = parentMenu;
		}
		Menu menu = offsetMenu;
		MenuItem item = (menuItems["x"] = new MenuSliderItem("Horizontal", 0, 100, (BetterDrawText.CustomOffsetX * 1000f + 50f).ToInt())
		{
			Description = "Move the slider to adjust the horizontal offset, or select to input it manually."
		});
		menu.AddMenuItem(item);
		Menu menu2 = offsetMenu;
		item = (menuItems["y"] = new MenuSliderItem("Vertical", 0, 100, (BetterDrawText.CustomOffsetY * 1000f + 50f).ToInt())
		{
			Description = "Move the slider to adjust the vertical offset, or select to input it manually."
		});
		menu2.AddMenuItem(item);
		Menu menu3 = offsetMenu;
		item = (menuItems["w"] = new MenuSliderItem("Width", 0, 100, (BetterDrawText.CustomOffsetW * 1000f + 50f).ToInt())
		{
			Description = "Move the slider to adjust the menu width, or select to input it manually."
		});
		menu3.AddMenuItem(item);
		Menu menu4 = offsetMenu;
		item = (menuItems["h"] = new MenuSliderItem("Height", 0, 100, MenuItem.RowSpacing.ConvertRange(19f, 57f, 0f, 100f).ToInt())
		{
			Description = "Move the slider to adjust the menu row height, or select to input it manually."
		});
		menu4.AddMenuItem(item);
		offsetMenu.AddMenuItem(Utils.GetSpacerMenuItem("\u02c5 Test \u02c5"));
		offsetMenu.AddMenuItem(new MenuItem("This is a test")
		{
			Label = "~b~PREVIEW",
			LeftIcon = MenuItem.Icon.GTACNR_ACCOUNT,
			Description = "A very long description that should make your text span multiple lines so that you can check if it looks OK, and you are probably not reading this because it's so long and you didn't ask me to tell you all this, but here I am writing this. I could have used a placeholder like ''Lorem Ipsum dolor sit amet'' or the Bee Movie script, but I decided not to be that lazy.",
			PlaySelectSound = false
		});
		offsetMenu.OpenMenu();
	}

	private void OnMenuClose(Menu menu, MenuClosedEventArgs e)
	{
		if (menu == offsetMenu)
		{
			if (autoOpened)
			{
				Utils.DisplayHelpText("If you need to adjust ~y~menu text offsets ~s~again, you can do so in ~y~M ~s~> ~y~Options ~s~> ~y~Display~s~.");
			}
			if (anythingChanged)
			{
				BaseScript.TriggerServerEvent("gtacnr:onSetMenuOffsets", new object[5]
				{
					API.GetScreenAspectRatio(false),
					BetterDrawText.CustomOffsetX,
					BetterDrawText.CustomOffsetY,
					BetterDrawText.CustomOffsetW,
					MenuItem.RowSpacing
				});
				Preferences.MenuOffsetSet.Set(value: true);
				Preferences.MenuOffsetX.Set(BetterDrawText.CustomOffsetX);
				Preferences.MenuOffsetY.Set(BetterDrawText.CustomOffsetY);
				Preferences.MenuOffsetW.Set(BetterDrawText.CustomOffsetW);
				Preferences.MenuOffsetH.Set(MenuItem.RowSpacing);
				Utils.PlayContinueSound();
				Utils.SendNotification("Your ~b~changes ~s~have been ~g~saved~s~.");
			}
		}
	}

	private async void OnMenuSliderItemSelect(Menu menu, MenuSliderItem sliderItem, int sliderPosition, int itemIndex)
	{
		if (!float.TryParse(await Utils.GetUserInput("Manual offset", "Enter a value between -1000 and 1000", "0", 6), out var result) || result < -1000f || result > 1000f)
		{
			Utils.PlayErrorSound();
			return;
		}
		anythingChanged = true;
		Utils.PlaySelectSound();
		int num = result.ConvertRange(-1000f, 1000f, 0f, 100f).ToInt();
		float num2 = (float)(num - 50) / 1000f;
		sliderItem.Position = num;
		if (sliderItem == menuItems["x"])
		{
			BetterDrawText.CustomOffsetX = num2;
		}
		else if (sliderItem == menuItems["y"])
		{
			BetterDrawText.CustomOffsetY = num2;
		}
		else if (sliderItem == menuItems["w"])
		{
			BetterDrawText.CustomOffsetW = num2;
		}
		else if (sliderItem == menuItems["h"])
		{
			num2 = Gtacnr.Utils.ConvertRange(num, 0f, 100f, 19f, 57f);
			MenuItem.RowSpacing = num2;
		}
	}

	private void OnMenuSliderPositionChange(Menu menu, MenuSliderItem sliderItem, int oldPosition, int newPosition, int itemIndex)
	{
		float num = (float)(newPosition - 50) / 1000f;
		if (sliderItem == menuItems["x"])
		{
			BetterDrawText.CustomOffsetX = num;
		}
		else if (sliderItem == menuItems["y"])
		{
			BetterDrawText.CustomOffsetY = num;
		}
		else if (sliderItem == menuItems["w"])
		{
			BetterDrawText.CustomOffsetW = num;
		}
		else if (sliderItem == menuItems["h"])
		{
			num = Gtacnr.Utils.ConvertRange(newPosition, 0f, 100f, 19f, 57f);
			MenuItem.RowSpacing = num;
		}
		anythingChanged = true;
	}
}
