using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.UI.Menus;
using Gtacnr.Localization;
using MenuAPI;

namespace Gtacnr.Client.Admin.Menus.EntitiesManagement;

public abstract class EntitiesManagementBase<T> : ICnRMenu where T : Entity
{
	protected readonly Menu MainMenu;

	protected List<T> entities = new List<T>();

	protected EntitiesManagementBase(Menu mainMenu)
	{
		MainMenu = mainMenu;
		MainMenu.CloseWhenDead = false;
		MainMenu.InstructionalButtons.Add((Control)166, LocalizationController.S(Entries.Main.BTN_REFRESH));
		MainMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)166, Menu.ControlPressCheckType.JUST_PRESSED, delegate
		{
			RefreshMenu();
		}, disableControl: true));
		MainMenu.InstructionalButtons.Add((Control)203, "Check first owner");
		MainMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)203, Menu.ControlPressCheckType.JUST_PRESSED, OnFetchFirstOwner, disableControl: true));
		MainMenu.OnMenuOpen += OnMenuOpen;
		MainMenu.OnMenuClose += OnMenuClose;
		MainMenu.OnItemSelect += OnItemSelect;
		MainMenu.OnIndexChange += OnIndexChange;
	}

	protected abstract List<T> GetAllEntities();

	protected string GetPlayerUsername(int id)
	{
		if (id == 0)
		{
			return "Server";
		}
		return LatentPlayers.Get(id)?.NameAndId ?? $"Disconnected ({id})";
	}

	protected abstract string GetEntityName(T entity);

	protected MenuItem CreateEntityMenuItem(T entity)
	{
		int playerServerId = API.GetPlayerServerId(API.NetworkGetEntityOwner(((PoolObject)(object)entity).Handle));
		string playerUsername = GetPlayerUsername(playerServerId);
		return new MenuItem(GetEntityName(entity) ?? "", "Current Owner: " + playerUsername + "~s~")
		{
			Label = $"({((PoolObject)(object)entity).Handle})",
			ItemData = entity
		};
	}

	private void OnMenuOpen(Menu menu)
	{
		EntitiesManagementScript.Instance.FirstOwnerCallback = FirstOwnerCallback;
		RefreshMenu();
	}

	private void OnMenuClose(Menu menu, MenuClosedEventArgs args)
	{
		EntitiesManagementScript.Instance.entityToDraw = null;
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		object itemData = menuItem.ItemData;
		T val = (T)((itemData is T) ? itemData : null);
		if (val != null)
		{
			Ped val2 = (Ped)((((object)val) is Ped) ? ((object)val) : null);
			if ((val2 != null && val2.IsPlayer) || !API.NetworkGetEntityIsNetworked(((PoolObject)(object)val).Handle) || !(await EntitiesManagementScript.Instance.RemoveEntity(API.NetworkGetNetworkIdFromEntity(((PoolObject)(object)val).Handle))))
			{
				return;
			}
		}
		menu.RemoveMenuItem(menuItem);
	}

	private void OnIndexChange(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex)
	{
		object itemData = newItem.ItemData;
		T val = (T)((itemData is T) ? itemData : null);
		if (val != null)
		{
			EntitiesManagementScript.Instance.entityToDraw = (Entity)(object)val;
		}
	}

	protected void RefreshMenu()
	{
		MainMenu.ClearMenuItems();
		entities = GetAllEntities();
		if (entities.Count != 0)
		{
			EntitiesManagementScript.Instance.entityToDraw = (Entity)(object)entities[0];
		}
		else
		{
			EntitiesManagementScript.Instance.entityToDraw = null;
		}
		foreach (T entity in entities)
		{
			MainMenu.AddMenuItem(CreateEntityMenuItem(entity));
		}
	}

	private void OnFetchFirstOwner(Menu menu, Control control)
	{
		MenuItem currentMenuItem = menu.GetCurrentMenuItem();
		if (currentMenuItem != null)
		{
			object itemData = currentMenuItem.ItemData;
			T val = (T)((itemData is T) ? itemData : null);
			if (val != null && API.NetworkGetEntityIsNetworked(((PoolObject)(object)val).Handle))
			{
				EntitiesManagementScript.Instance.FetchFirstOwnerId(API.NetworkGetNetworkIdFromEntity(((PoolObject)(object)val).Handle));
			}
		}
	}

	protected void FirstOwnerCallback(int entityNetworkId, int firstOwnerId)
	{
		MenuItem menuItem = MainMenu.GetMenuItems().FirstOrDefault(delegate(MenuItem item)
		{
			object itemData2 = item.ItemData;
			T val2 = (T)((itemData2 is T) ? itemData2 : null);
			return val2 != null && ((Entity)val2).NetworkId == entityNetworkId;
		});
		if (menuItem != null)
		{
			object itemData = menuItem.ItemData;
			T val = (T)((itemData is T) ? itemData : null);
			if (val != null)
			{
				int playerServerId = API.GetPlayerServerId(API.NetworkGetEntityOwner(((PoolObject)(object)val).Handle));
				string playerUsername = GetPlayerUsername(playerServerId);
				string playerUsername2 = GetPlayerUsername(firstOwnerId);
				menuItem.Description = "Current Owner: " + playerUsername + "~s~\nFirst Owner: " + playerUsername2;
			}
		}
	}

	public Menu GetMenu()
	{
		return MainMenu;
	}
}
