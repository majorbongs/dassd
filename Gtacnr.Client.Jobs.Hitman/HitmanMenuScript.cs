using Gtacnr.Localization;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Hitman;

public class HitmanMenuScript : Script
{
	public static readonly string MainMenuItemDescription = LocalizationController.S(Entries.Imenu.IMENU_JOB_DESCR_HITMAN);

	public static Menu Menu { get; private set; } = new Menu(LocalizationController.S(Entries.Businesses.MENU_HITMAN_TITLE), LocalizationController.S(Entries.Main.MENU_CHOOSE_OPTION));

	public HitmanMenuScript()
	{
		AddSubmenuItem(DispatchScript.HitmanDispatch.CallsMenu, new MenuItem(LocalizationController.S(Entries.Businesses.MENU_HITMAN_CONTRACTS_SUBTITLE), LocalizationController.S(Entries.Businesses.MENU_HITMAN_CONTRACTS_DESCRIPTION))
		{
			LeftIcon = MenuItem.Icon.GTACNR_JOB_CALLS
		});
	}

	private static void AddSubmenuItem(Menu subMenu, MenuItem menuItem)
	{
		Menu.AddMenuItem(menuItem);
		MenuController.AddSubmenu(Menu, subMenu);
		MenuController.BindMenuItem(Menu, subMenu, menuItem);
	}
}
