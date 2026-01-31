using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.API.UI.Menus;
using MenuAPI;

namespace Gtacnr.Client.Admin.Menus.EntitiesManagement;

public class EntitiesManagementScript : Script, ICnRAdminMenu, ICnRMenu
{
	private Menu MainMenu;

	private MenuItem SettingsMenuItem;

	private EntitiesManagementSettings SettingsMenu;

	private MenuItem PropsMenuItem;

	private EntitiesManagementProps PropsMenu;

	private MenuItem VehiclesMenuItem;

	private EntitiesManagementVehicles VehiclesMenu;

	private MenuItem PedsMenuItem;

	private EntitiesManagementPeds PedsMenu;

	private Entity _entityToDraw;

	public Action<int, int> FirstOwnerCallback;

	public Entity entityToDraw
	{
		get
		{
			return _entityToDraw;
		}
		set
		{
			if (_entityToDraw == null && value != null)
			{
				AttachTasks();
			}
			if (value == null)
			{
				DetachTasks();
			}
			_entityToDraw = value;
		}
	}

	public static EntitiesManagementScript Instance { get; private set; }

	public EntitiesManagementScript()
	{
		Instance = this;
	}

	public void CreateMenus()
	{
		if (MainMenu == null)
		{
			MainMenu = new Menu("Moderator Menu", "Entities Management")
			{
				CloseWhenDead = false
			};
			SettingsMenu = new EntitiesManagementSettings();
			SettingsMenuItem = new MenuItem("Settings")
			{
				Label = Utils.MENU_ARROW
			};
			MainMenu.AddMenuItem(SettingsMenuItem);
			MenuController.BindMenuItem(MainMenu, SettingsMenu.GetMenu(), SettingsMenuItem);
			PropsMenu = new EntitiesManagementProps();
			PropsMenuItem = new MenuItem("Props")
			{
				Label = Utils.MENU_ARROW
			};
			MainMenu.AddMenuItem(PropsMenuItem);
			MenuController.BindMenuItem(MainMenu, PropsMenu.GetMenu(), PropsMenuItem);
			VehiclesMenu = new EntitiesManagementVehicles();
			VehiclesMenuItem = new MenuItem("Vehicles")
			{
				Label = Utils.MENU_ARROW
			};
			MainMenu.AddMenuItem(VehiclesMenuItem);
			MenuController.BindMenuItem(MainMenu, VehiclesMenu.GetMenu(), VehiclesMenuItem);
			PedsMenu = new EntitiesManagementPeds();
			PedsMenuItem = new MenuItem("Peds")
			{
				Label = Utils.MENU_ARROW
			};
			MainMenu.AddMenuItem(PedsMenuItem);
			MenuController.BindMenuItem(MainMenu, PedsMenu.GetMenu(), PedsMenuItem);
		}
	}

	public Menu GetMenu()
	{
		return MainMenu;
	}

	private void AttachTasks()
	{
		base.Update -= DrawEntityBox;
		base.Update += DrawEntityBox;
	}

	private void DetachTasks()
	{
		base.Update -= DrawEntityBox;
	}

	private async Coroutine DrawEntityBox()
	{
		if (entityToDraw != null && ((PoolObject)entityToDraw).Exists())
		{
			Utils.DrawEntityBoundingBox(entityToDraw, 250, 150, 0, 100);
		}
	}

	public void FetchFirstOwnerId(int networkId)
	{
		BaseScript.TriggerServerEvent("gtacnr:admin:fetchFirstOwnerId", new object[1] { networkId });
	}

	[EventHandler("gtacnr:admin:getFirstOwnerId")]
	private void OnGetFirstOwnerId(int networkId, int ownerId)
	{
		FirstOwnerCallback(networkId, ownerId);
	}

	public bool ShouldDrawEntity(Entity e)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if ((SettingsMenu.OnlyOnScreenEntities && e.IsOnScreen) || !SettingsMenu.OnlyOnScreenEntities)
		{
			Vector3 position = e.Position;
			if (((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) < (float)SettingsMenu.EntityRange)
			{
				return e.NetworkId != 0;
			}
		}
		return false;
	}

	public async Task<bool> RemoveEntity(int networkId)
	{
		return await TriggerServerEventAsync<bool>("gtacnr:admin:destroyEntity", new object[1] { networkId });
	}
}
