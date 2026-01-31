using Gtacnr.Client.API.UI.Menus;
using MenuAPI;

namespace Gtacnr.Client.Admin.Menus.EntitiesManagement;

public class EntitiesManagementSettings : ICnRMenu
{
	private readonly Menu MainMenu;

	private readonly MenuSliderItem entityRangeItem;

	private readonly MenuCheckboxItem onlyOnScreenEntitiesItem;

	public int EntityRange { get; private set; } = 2000;

	public bool OnlyOnScreenEntities { get; private set; } = true;

	public EntitiesManagementSettings()
	{
		MainMenu = new Menu("Settings", "Entities Management Settings")
		{
			CloseWhenDead = false
		};
		entityRangeItem = new MenuSliderItem("Entity Range", 1, 10, EntityRange / 1000)
		{
			Description = "Adjust the entity range."
		};
		onlyOnScreenEntitiesItem = new MenuCheckboxItem("Only On-Screen Entities", "Toggle to manage only on-screen entities.", OnlyOnScreenEntities);
		MainMenu.OnCheckboxChange += OnCheckboxChange;
		MainMenu.OnSliderPositionChange += OnSliderPositionChange;
		MainMenu.AddMenuItem(entityRangeItem);
		MainMenu.AddMenuItem(onlyOnScreenEntitiesItem);
	}

	private void OnSliderPositionChange(Menu menu, MenuSliderItem sliderItem, int oldPosition, int newPosition, int itemIndex)
	{
		if (sliderItem == entityRangeItem)
		{
			EntityRange = newPosition * 1000;
		}
	}

	private void OnCheckboxChange(Menu menu, MenuCheckboxItem menuItem, int itemIndex, bool newCheckedState)
	{
		if (menuItem == onlyOnScreenEntitiesItem)
		{
			OnlyOnScreenEntities = newCheckedState;
		}
	}

	public Menu GetMenu()
	{
		return MainMenu;
	}
}
