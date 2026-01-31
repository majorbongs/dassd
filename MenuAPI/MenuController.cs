using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Gtacnr.Client;
using Gtacnr.Client.Libs;

namespace MenuAPI;

public class MenuController : Script
{
	public enum TextDirection
	{
		LTR,
		RTL
	}

	public enum MenuAlignmentOption
	{
		Left,
		Right
	}

	public const string _texture_dict = "commonmenu";

	public const string _header_texture = "interaction_bgd";

	private static List<string> menuTextureAssets = new List<string>
	{
		"commonmenu", "commonmenutu", "mpleaderboard", "mphud", "mpshopsale", "mpinventory", "mprankbadge", "mpcarhud", "mpcarhud2", "shared",
		"gtacnr_menu"
	};

	internal static int _scale = API.RequestScaleformMovie("INSTRUCTIONAL_BUTTONS");

	private static int ManualTimerForGC = API.GetGameTimer();

	private static MenuAlignmentOption _alignment = MenuAlignmentOption.Left;

	private static MenuController instance;

	private static bool wasAnyMenuOpenLastFrame = false;

	public static List<Menu> Menus { get; protected set; } = new List<Menu>();

	internal static HashSet<Menu> VisibleMenus { get; } = new HashSet<Menu>();

	private static float AspectRatio => API.GetScreenAspectRatio(false);

	public static float ScreenWidth => 1080f * AspectRatio;

	public static float ScreenHeight => 1080f;

	public static bool DisableMenuButtons { get; set; } = false;

	public static bool AreMenuButtonsEnabled
	{
		get
		{
			if (IsAnyMenuOpen() && !Game.IsPaused && Fading.IsFadedIn && !API.IsPlayerSwitchInProgress())
			{
				return !DisableMenuButtons;
			}
			return false;
		}
	}

	public static bool NavigateMenuUsingArrows { get; set; } = true;

	public static bool EnableManualGCs { get; set; } = true;

	public static bool DontOpenAnyMenu { get; set; } = false;

	public static bool PreventExitingMenu { get; set; } = false;

	public static bool DisableBackButton { get; set; } = false;

	public static bool SetDrawOrder { get; set; } = true;

	public static Control MenuToggleKey { get; set; } = (Control)(-1);

	public static bool EnableMenuToggleKeyOnController { get; set; } = false;

	public static Font DefaultHeaderFont { get; set; } = Font.HouseScript;

	public static Font DefaultTextFont { get; set; } = Font.Chalet;

	public static TextDirection DefaultTextDirection { get; set; } = TextDirection.LTR;

	public static Dictionary<MenuItem, Menu> MenuButtons { get; private set; } = new Dictionary<MenuItem, Menu>();

	public static Menu MainMenu { get; set; } = null;

	public static MenuAlignmentOption MenuAlignment
	{
		get
		{
			return _alignment;
		}
		set
		{
			if (AspectRatio < 1.8888888f)
			{
				_alignment = value;
				return;
			}
			_alignment = MenuAlignmentOption.Left;
			if (value == MenuAlignmentOption.Right)
			{
				Debug.WriteLine("[MenuAPI (" + API.GetCurrentResourceName() + ")] Warning: Right aligned menus are not supported for aspect ratios 17:9 or 21:9, left aligned will be used instead.");
			}
		}
	}

	public MenuController()
	{
		base.Update += ProcessMenus;
		base.Update += DrawInstructionalButtons;
		base.Update += ProcessMainButtons;
		base.Update += ProcessDirectionalButtons;
		base.Update += ProcessToggleMenuButton;
		base.Update += MenuButtonsDisableChecks;
		((BaseScript)this).Exports.Add("IsAnyMenuOpen", (Delegate)new Func<bool>(IsAnyMenuOpen));
		instance = this;
	}

	public static void BindMenuItem(Menu parentMenu, Menu childMenu, MenuItem menuItem)
	{
		AddSubmenu(parentMenu, childMenu);
		if (MenuButtons.ContainsKey(menuItem))
		{
			MenuButtons[menuItem] = childMenu;
		}
		else
		{
			MenuButtons.Add(menuItem, childMenu);
		}
	}

	public static void AddMenu(Menu menu)
	{
		if (!Menus.Contains(menu))
		{
			Menus.Add(menu);
			if (MainMenu == null)
			{
				MainMenu = menu;
			}
		}
	}

	public static void AddSubmenu(Menu parent, Menu child)
	{
		if (!Menus.Contains(child))
		{
			AddMenu(child);
		}
		if (!parent.ChildrenMenus.Contains(child))
		{
			parent.ChildrenMenus.Add(child);
		}
		child.ParentMenu = parent;
	}

	private static async Task LoadAssets()
	{
		menuTextureAssets.ForEach(delegate(string asset)
		{
			if (!API.HasStreamedTextureDictLoaded(asset))
			{
				API.RequestStreamedTextureDict(asset, false);
			}
		});
		while (menuTextureAssets.Any((string asset) => !API.HasStreamedTextureDictLoaded(asset)))
		{
			await Script.Yield();
		}
	}

	private static void UnloadAssets()
	{
		menuTextureAssets.ForEach(delegate(string asset)
		{
			if (API.HasStreamedTextureDictLoaded(asset))
			{
				API.SetStreamedTextureDictAsNoLongerNeeded(asset);
			}
		});
	}

	public static Menu GetCurrentMenu()
	{
		if (IsAnyMenuOpen())
		{
			return VisibleMenus.FirstOrDefault();
		}
		return null;
	}

	public static bool IsAnyMenuOpen()
	{
		return VisibleMenus.Count > 0;
	}

	private async Coroutine ProcessMainButtons()
	{
		if (!IsAnyMenuOpen())
		{
			return;
		}
		Menu currentMenu = GetCurrentMenu();
		if (currentMenu != null && !DontOpenAnyMenu)
		{
			if (PreventExitingMenu)
			{
				Game.DisableControlThisFrame(0, (Control)199);
				Game.DisableControlThisFrame(0, (Control)200);
			}
			if (currentMenu.Visible && AreMenuButtonsEnabled)
			{
				if (Game.IsDisabledControlJustReleased(0, (Control)201) || Game.IsControlJustReleased(0, (Control)201) || Game.IsDisabledControlJustReleased(0, (Control)106) || Game.IsControlJustReleased(0, (Control)106))
				{
					if (currentMenu.Size > 0)
					{
						currentMenu.SelectItem(currentMenu.CurrentIndex);
					}
				}
				else if (Game.IsDisabledControlJustReleased(0, (Control)177) && !DisableBackButton)
				{
					await Script.Yield();
					currentMenu.GoBack();
				}
				else if (Game.IsDisabledControlJustReleased(0, (Control)177) && PreventExitingMenu && !DisableBackButton)
				{
					if (currentMenu.ParentMenu != null)
					{
						currentMenu.GoBack();
					}
					await Script.Yield();
				}
			}
		}
		Game.DisableControlThisFrame(0, (Control)20);
	}

	private bool IsUpPressed()
	{
		if (!AreMenuButtonsEnabled)
		{
			return false;
		}
		if (!Game.PlayerPed.IsInVehicle() && Game.IsControlPressed(0, (Control)37) && (Game.IsControlPressed(0, (Control)16) || Game.IsControlPressed(0, (Control)17)))
		{
			return false;
		}
		if (Game.IsControlPressed(0, (Control)188) || Game.IsDisabledControlPressed(0, (Control)188) || Game.IsControlPressed(0, (Control)181) || Game.IsDisabledControlPressed(0, (Control)181))
		{
			return true;
		}
		return false;
	}

	private bool IsDownPressed()
	{
		if (!AreMenuButtonsEnabled)
		{
			return false;
		}
		if (!Game.PlayerPed.IsInVehicle() && Game.IsControlPressed(0, (Control)37) && (Game.IsControlPressed(0, (Control)16) || Game.IsControlPressed(0, (Control)17)))
		{
			return false;
		}
		if (Game.IsControlPressed(0, (Control)187) || Game.IsDisabledControlPressed(0, (Control)187) || Game.IsControlPressed(0, (Control)180) || Game.IsDisabledControlPressed(0, (Control)180))
		{
			return true;
		}
		return false;
	}

	private bool IsLeftPressed()
	{
		if (!Game.IsControlJustPressed(2, (Control)174) && !Game.IsDisabledControlJustPressed(2, (Control)174) && !Game.IsControlJustPressed(2, (Control)189) && !Game.IsDisabledControlJustPressed(2, (Control)189) && !Game.IsControlPressed(2, (Control)174) && !Game.IsDisabledControlPressed(2, (Control)174) && !Game.IsControlPressed(2, (Control)189))
		{
			return Game.IsDisabledControlPressed(2, (Control)189);
		}
		return true;
	}

	private bool IsRightPressed()
	{
		if (!Game.IsControlJustPressed(2, (Control)175) && !Game.IsDisabledControlJustPressed(2, (Control)175) && !Game.IsControlJustPressed(2, (Control)190) && !Game.IsDisabledControlJustPressed(2, (Control)190) && !Game.IsControlPressed(2, (Control)175) && !Game.IsDisabledControlPressed(2, (Control)175) && !Game.IsControlPressed(2, (Control)190))
		{
			return Game.IsDisabledControlPressed(2, (Control)190);
		}
		return true;
	}

	private async Coroutine ProcessToggleMenuButton()
	{
		if (!Game.IsPaused && !API.IsPauseMenuRestarting() && API.IsScreenFadedIn() && !API.IsPlayerSwitchInProgress() && !DisableMenuButtons)
		{
			if (IsAnyMenuOpen())
			{
				Game.DisableControlThisFrame(0, MenuToggleKey);
				if ((int)Game.CurrentInputMode == 0 && (Game.IsControlJustPressed(0, MenuToggleKey) || Game.IsDisabledControlJustPressed(0, MenuToggleKey)) && !PreventExitingMenu)
				{
					GetCurrentMenu()?.CloseMenu();
				}
			}
			else if ((int)Game.CurrentInputMode == 1)
			{
				if (!EnableMenuToggleKeyOnController)
				{
					return;
				}
				int tmpTimer = API.GetGameTimer();
				while ((Game.IsControlPressed(0, (Control)244) || Game.IsDisabledControlPressed(0, (Control)244)) && !Game.IsPaused && API.IsScreenFadedIn() && !API.IsPlayerSwitchInProgress() && !DontOpenAnyMenu)
				{
					if (API.GetGameTimer() - tmpTimer > 400)
					{
						if (MainMenu != null)
						{
							MainMenu.OpenMenu();
						}
						else if (Menus.Count > 0)
						{
							Menus[0].OpenMenu();
						}
						break;
					}
					await Script.Yield();
				}
			}
			else if ((Game.IsControlJustPressed(0, MenuToggleKey) || Game.IsDisabledControlJustPressed(0, MenuToggleKey)) && !Game.IsPaused && API.IsScreenFadedIn() && !API.IsPlayerSwitchInProgress() && !DontOpenAnyMenu && Menus.Count > 0)
			{
				if (MainMenu != null)
				{
					MainMenu.OpenMenu();
				}
				else
				{
					Menus[0].OpenMenu();
				}
			}
		}
		await Task.FromResult(0);
	}

	private async Coroutine ProcessDirectionalButtons()
	{
		if (!AreMenuButtonsEnabled)
		{
			return;
		}
		Menu currentMenu = GetCurrentMenu();
		if (currentMenu == null || DontOpenAnyMenu || currentMenu.Size <= 0 || !currentMenu.Visible || currentMenu.DisableDpadNavigation)
		{
			return;
		}
		if (IsUpPressed())
		{
			currentMenu.GoUp();
			int time = API.GetGameTimer();
			int times = 0;
			int delay = 200;
			while (IsUpPressed() && IsAnyMenuOpen() && GetCurrentMenu() != null)
			{
				currentMenu = GetCurrentMenu();
				if (API.GetGameTimer() - time > delay)
				{
					times++;
					if (times > 2)
					{
						delay = 150;
					}
					if (times > 5)
					{
						delay = 100;
					}
					if (times > 25)
					{
						delay = 50;
					}
					if (times > 60)
					{
						delay = 25;
					}
					currentMenu.GoUp();
					time = API.GetGameTimer();
				}
				await Script.Yield();
			}
		}
		else if (IsDownPressed())
		{
			currentMenu.GoDown();
			int delay = API.GetGameTimer();
			int times = 0;
			int time = 200;
			while (IsDownPressed() && GetCurrentMenu() != null)
			{
				currentMenu = GetCurrentMenu();
				if (API.GetGameTimer() - delay > time)
				{
					times++;
					if (times > 2)
					{
						time = 150;
					}
					if (times > 5)
					{
						time = 100;
					}
					if (times > 25)
					{
						time = 50;
					}
					if (times > 60)
					{
						time = 25;
					}
					currentMenu.GoDown();
					delay = API.GetGameTimer();
				}
				await Script.Yield();
			}
		}
		else if (IsLeftPressed())
		{
			if (!currentMenu.GetMenuItems()[currentMenu.CurrentIndex].Enabled)
			{
				return;
			}
			currentMenu.GoLeft();
			int time = API.GetGameTimer();
			int times = 0;
			int delay = 200;
			while (IsLeftPressed() && GetCurrentMenu() != null && AreMenuButtonsEnabled)
			{
				currentMenu = GetCurrentMenu();
				if (API.GetGameTimer() - time > delay)
				{
					times++;
					if (times > 2)
					{
						delay = 150;
					}
					if (times > 5)
					{
						delay = 100;
					}
					if (times > 25)
					{
						delay = 50;
					}
					if (times > 60)
					{
						delay = 25;
					}
					currentMenu.GoLeft();
					time = API.GetGameTimer();
				}
				await Script.Yield();
			}
		}
		else
		{
			if (!IsRightPressed() || !currentMenu.GetMenuItems()[currentMenu.CurrentIndex].Enabled)
			{
				return;
			}
			currentMenu.GoRight();
			int delay = API.GetGameTimer();
			int times = 0;
			int time = 200;
			while (IsRightPressed() && GetCurrentMenu() != null && AreMenuButtonsEnabled)
			{
				currentMenu = GetCurrentMenu();
				if (API.GetGameTimer() - delay > time)
				{
					times++;
					if (times > 2)
					{
						time = 150;
					}
					if (times > 5)
					{
						time = 100;
					}
					if (times > 25)
					{
						time = 50;
					}
					if (times > 60)
					{
						time = 25;
					}
					currentMenu.GoRight();
					delay = API.GetGameTimer();
				}
				await Script.Yield();
			}
		}
	}

	private async Coroutine MenuButtonsDisableChecks()
	{
		if (isInputVisible())
		{
			bool buttonsState = DisableMenuButtons;
			while (isInputVisible())
			{
				await Script.Yield();
				DisableMenuButtons = true;
			}
			int timer = API.GetGameTimer();
			while (API.GetGameTimer() - timer < 300)
			{
				await Script.Yield();
				DisableMenuButtons = true;
			}
			DisableMenuButtons = buttonsState;
		}
		static bool isInputVisible()
		{
			return API.UpdateOnscreenKeyboard() == 0;
		}
	}

	public static void CloseAllMenus()
	{
		foreach (Menu item in Menus.Where((Menu m) => m.Visible))
		{
			item.CloseMenu();
		}
	}

	private static void DisableControls()
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Invalid comparison between Unknown and I4
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		if (!IsAnyMenuOpen())
		{
			return;
		}
		Menu currentMenu = GetCurrentMenu();
		if (currentMenu == null)
		{
			return;
		}
		MenuItem currentMenuItem = currentMenu.GetCurrentMenuItem();
		if (currentMenuItem != null && (currentMenuItem is MenuSliderItem || currentMenuItem is MenuListItem || currentMenuItem is MenuDynamicListItem) && (int)Game.CurrentInputMode == 1)
		{
			Game.DisableControlThisFrame(0, (Control)37);
		}
		if (((Entity)Game.PlayerPed).IsDead && currentMenu != null && currentMenu.CloseWhenDead)
		{
			CloseAllMenus();
		}
		if ((int)Game.CurrentInputMode == 1)
		{
			Game.DisableControlThisFrame(0, (Control)20);
			if (Game.PlayerPed.IsInVehicle())
			{
				Game.DisableControlThisFrame(0, (Control)74);
				Game.DisableControlThisFrame(0, (Control)73);
				Game.DisableControlThisFrame(0, (Control)357);
			}
		}
		else
		{
			Game.DisableControlThisFrame(0, (Control)200);
			if (!Game.IsControlPressed(0, (Control)37))
			{
				Game.DisableControlThisFrame(24, (Control)16);
				Game.DisableControlThisFrame(24, (Control)17);
			}
		}
		Game.DisableControlThisFrame(0, (Control)333);
		Game.DisableControlThisFrame(0, (Control)332);
		Game.DisableControlThisFrame(0, (Control)81);
		Game.DisableControlThisFrame(0, (Control)85);
		Game.DisableControlThisFrame(0, (Control)82);
		Game.DisableControlThisFrame(0, (Control)27);
		Game.DisableControlThisFrame(0, (Control)177);
		Game.DisableControlThisFrame(0, (Control)173);
		Game.DisableControlThisFrame(0, (Control)174);
		Game.DisableControlThisFrame(0, (Control)175);
		Game.DisableControlThisFrame(0, (Control)24);
		Game.DisableControlThisFrame(0, (Control)257);
		Game.DisableControlThisFrame(0, (Control)263);
		Game.DisableControlThisFrame(0, (Control)264);
		Game.DisableControlThisFrame(0, (Control)142);
		Game.DisableControlThisFrame(0, (Control)141);
		Game.DisableControlThisFrame(0, (Control)140);
		Game.DisableControlThisFrame(0, (Control)69);
		Game.DisableControlThisFrame(0, (Control)70);
		Game.DisableControlThisFrame(0, (Control)114);
		Game.DisableControlThisFrame(0, (Control)92);
		Game.DisableControlThisFrame(0, (Control)25);
		Game.DisableControlThisFrame(0, (Control)68);
		Game.DisableControlThisFrame(0, (Control)22);
		if (Game.PlayerPed.IsInVehicle())
		{
			Game.DisableControlThisFrame(0, (Control)99);
			Game.DisableControlThisFrame(0, (Control)100);
			Game.DisableControlThisFrame(0, (Control)80);
		}
	}

	private static async Coroutine ProcessMenus()
	{
		bool flag = IsAnyMenuOpen();
		if (flag)
		{
			if (!wasAnyMenuOpenLastFrame && MenuAlignment == MenuAlignmentOption.Right)
			{
				BaseScript.TriggerEvent("gtacnr:hud:offsetX", new object[1] { 0f - 510f / ScreenWidth });
			}
		}
		else if (wasAnyMenuOpenLastFrame)
		{
			BaseScript.TriggerEvent("gtacnr:hud:offsetX", new object[1] { 0f });
		}
		wasAnyMenuOpenLastFrame = flag;
		if (Menus.Count > 0 && IsAnyMenuOpen() && API.IsScreenFadedIn() && !Game.IsPaused && !API.IsPlayerSwitchInProgress())
		{
			await LoadAssets();
			DisableControls();
			Menu currentMenu = GetCurrentMenu();
			if (currentMenu != null)
			{
				if (DontOpenAnyMenu)
				{
					if (currentMenu.Visible && !currentMenu.IgnoreDontOpenMenus)
					{
						currentMenu.CloseMenu();
					}
				}
				else if (currentMenu.Visible)
				{
					currentMenu.Draw();
				}
			}
			if (EnableManualGCs && API.GetGameTimer() - ManualTimerForGC > 60000)
			{
				GC.Collect();
				ManualTimerForGC = API.GetGameTimer();
			}
		}
		else
		{
			UnloadAssets();
		}
	}

	internal static async Coroutine DrawInstructionalButtons()
	{
		if (!Game.IsPaused && API.IsScreenFadedIn() && !API.IsPlayerSwitchInProgress() && !API.IsWarningMessageActive() && API.UpdateOnscreenKeyboard() != 0)
		{
			Menu menu = GetCurrentMenu();
			if (menu != null && menu.Visible && menu.EnableInstructionalButtons)
			{
				if (!API.HasScaleformMovieLoaded(_scale))
				{
					_scale = API.RequestScaleformMovie("INSTRUCTIONAL_BUTTONS");
				}
				while (!API.HasScaleformMovieLoaded(_scale))
				{
					await Script.Yield();
				}
				API.BeginScaleformMovieMethod(_scale, "CLEAR_ALL");
				API.EndScaleformMovieMethod();
				for (int i = 0; i < menu.InstructionalButtons.Count; i++)
				{
					string value = menu.InstructionalButtons.ElementAt(i).Value;
					Control key = menu.InstructionalButtons.ElementAt(i).Key;
					API.BeginScaleformMovieMethod(_scale, "SET_DATA_SLOT");
					API.ScaleformMovieMethodAddParamInt(i);
					API.PushScaleformMovieMethodParameterString(API.GetControlInstructionalButton(0, (int)key, 1));
					API.PushScaleformMovieMethodParameterString(value);
					API.EndScaleformMovieMethod();
				}
				if (menu.CustomInstructionalButtons.Count > 0)
				{
					for (int j = 0; j < menu.CustomInstructionalButtons.Count; j++)
					{
						Menu.InstructionalButton instructionalButton = menu.CustomInstructionalButtons[j];
						API.BeginScaleformMovieMethod(_scale, "SET_DATA_SLOT");
						API.ScaleformMovieMethodAddParamInt(j + menu.InstructionalButtons.Count);
						API.PushScaleformMovieMethodParameterString(instructionalButton.controlString);
						API.PushScaleformMovieMethodParameterString(instructionalButton.instructionText);
						API.EndScaleformMovieMethod();
					}
				}
				API.BeginScaleformMovieMethod(_scale, "DRAW_INSTRUCTIONAL_BUTTONS");
				API.ScaleformMovieMethodAddParamInt(0);
				API.EndScaleformMovieMethod();
				API.DrawScaleformMovieFullscreen(_scale, 255, 255, 255, 255, 0);
				return;
			}
		}
		DisposeInstructionalButtonsScaleform();
	}

	private static void DisposeInstructionalButtonsScaleform()
	{
		if (API.HasScaleformMovieLoaded(_scale))
		{
			API.SetScaleformMovieAsNoLongerNeeded(ref _scale);
		}
	}

	[EventHandler("onResourceStop")]
	private static void OnResourceStop(string name)
	{
		if (name == API.GetCurrentResourceName())
		{
			CloseAllMenus();
		}
	}
}
