using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Inventory;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Weapons;
using Gtacnr.Client.Zones;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.IMenu;

public class ArmoryMenuScript : Script
{
	private Menu textureMenu;

	private WeaponDefinition selectedWeapon;

	private Dictionary<string, string> selectedTextures = new Dictionary<string, string>();

	private WeaponTextureInfo selectedTextureInfo;

	public static Menu Menu { get; private set; }

	private Dictionary<string, Menu> menus { get; } = new Dictionary<string, Menu>();

	private Dictionary<string, MenuItem> menuItems { get; } = new Dictionary<string, MenuItem>();

	public ArmoryMenuScript()
	{
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	protected override async void OnStarted()
	{
		while (MainMenuScript.MainMenu == null)
		{
			await BaseScript.Delay(0);
		}
		Menu = new Menu(LocalizationController.S(Entries.Businesses.MENU_ARMORY_TITLE), LocalizationController.S(Entries.Businesses.MENU_ARMORY_MANAGE_SUBTITLE));
		Menu.InstructionalButtons.Add((Control)166, LocalizationController.S(Entries.Main.BTN_REFRESH));
		Menu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)166, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnMenuRefresh, disableControl: true));
		MenuController.AddSubmenu(MainMenuScript.MainMenu, Menu);
		MenuController.BindMenuItem(MainMenuScript.MainMenu, Menu, MainMenuScript.MainMenuItems["armory"]);
		while (!ArmoryScript.Initialized)
		{
			await BaseScript.Delay(0);
		}
		RefreshMenu();
	}

	private async void RefreshMenu(bool forceReload = false)
	{
		try
		{
			Menu.ClearMenuItems();
			menus.Clear();
			menuItems.Clear();
			if (forceReload)
			{
				Menu.AddLoadingMenuItem();
				await ArmoryScript.ReloadLoadout();
				Menu.ClearMenuItems();
			}
			Loadout currentLoadout = ArmoryScript.CurrentLoadout;
			int num = 0;
			foreach (LoadoutWeapon weaponDatum in currentLoadout.WeaponData)
			{
				WeaponDefinition weaponDefinition = Gtacnr.Data.Items.GetWeaponDefinition(weaponDatum.ItemId);
				if (weaponDefinition == null)
				{
					continue;
				}
				string key = $"cat_{weaponDefinition.Category}";
				if (!menus.ContainsKey(key))
				{
					string text = Gtacnr.Utils.ResolveLocalization(Gtacnr.Utils.GetDescription(weaponDefinition.Category));
					MenuItem menuItem = new MenuItem(text)
					{
						Label = "›",
						ItemData = weaponDefinition
					};
					Menu.AddMenuItem(menuItem);
					menus[key] = new Menu(LocalizationController.S(Entries.Businesses.MENU_ARMORY_TITLE), text)
					{
						PlaySelectSound = false
					};
					MenuController.AddSubmenu(Menu, menus[key]);
					MenuController.BindMenuItem(Menu, menus[key], menuItem);
					menus[key].OnIndexChange += OnMenuIndexChange;
					menus[key].ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)203, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnEquipUnequip, disableControl: true));
					Control control = (Control)(Utils.IsUsingKeyboard() ? 289 : 72);
					menus[key].ButtonPressHandlers.Add(new Menu.ButtonPressHandler(control, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnCustomTexture, disableControl: true));
				}
				MenuItem menuItem2 = new MenuItem(weaponDefinition.Name)
				{
					ItemData = weaponDefinition
				};
				menus[key].AddMenuItem(menuItem2);
				RefreshWeaponMenuItem(menus[key], menuItem2);
				string key2 = "weap_" + weaponDatum.ItemId;
				menus[key2] = new Menu(LocalizationController.S(Entries.Businesses.MENU_ARMORY_TITLE), weaponDefinition.Name);
				MenuController.AddSubmenu(menus[key], menus[key2]);
				MenuController.BindMenuItem(menus[key], menus[key2], menuItem2);
				menus[key2].OnIndexChange += OnMenuIndexChange;
				menus[key2].ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)203, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnEquipUnequip, disableControl: true));
				if (weaponDatum.Attachments != null && weaponDatum.Attachments.Count > 0)
				{
					foreach (LoadoutAttachment attachment in weaponDatum.Attachments)
					{
						WeaponComponentDefinition weaponComponentDefinition = Gtacnr.Data.Items.GetWeaponComponentDefinition(attachment.ItemId);
						if (weaponComponentDefinition != null)
						{
							MenuItem menuItem3 = new MenuItem(weaponComponentDefinition.Name, weaponComponentDefinition.Description)
							{
								ItemData = Tuple.Create(weaponDefinition, weaponComponentDefinition),
								Label = ((weaponComponentDefinition.Type == WeaponComponentType.Livery) ? "~y~SKIN" : ""),
								PlaySelectSound = false
							};
							menus[key2].AddMenuItem(menuItem3);
							RefreshAttachmentMenuItem(menus[key2], menuItem3);
						}
					}
				}
				else
				{
					menus[key2].AddMenuItem(new MenuItem("No attachments :(", "You don't have any attachment on this weapon."));
				}
				num++;
			}
			Menu.SortMenuItems((MenuItem i1, MenuItem i2) => (i1.ItemData as WeaponDefinition).Category - (i2.ItemData as WeaponDefinition).Category);
			MenuItem menuItem4 = (menuItems["ammo"] = new MenuItem("~b~" + LocalizationController.S(Entries.Businesses.MENU_ARMORY_AMMO_SUBTITLE), "View the types of ammo you currently have.")
			{
				Label = "›"
			});
			MenuItem menuItem6 = menuItem4;
			Menu.AddMenuItem(menuItem6);
			Menu menu = (menus["ammo"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_ARMORY_TITLE), LocalizationController.S(Entries.Businesses.MENU_ARMORY_AMMO_SUBTITLE)));
			Menu menu3 = menu;
			foreach (LoadoutAmmo ammoDatum in currentLoadout.AmmoData)
			{
				if (ammoDatum.Amount > 0)
				{
					AmmoDefinition ammoDefinition = Gtacnr.Data.Items.GetAmmoDefinition(ammoDatum.ItemId);
					if (ammoDefinition != null)
					{
						MenuItem item = new MenuItem(ammoDefinition.Name)
						{
							Description = ammoDefinition.Description + (ammoDefinition.IsIllegal ? "\n~r~ILLEGAL" : ""),
							ItemData = ammoDefinition,
							Label = $"{ammoDatum.Amount}"
						};
						menu3.AddMenuItem(item);
					}
				}
			}
			if (currentLoadout.AmmoData.Count == 0)
			{
				menu3.AddMenuItem(new MenuItem("No ammo :(", "You don't have any ammunition."));
			}
			MenuController.AddSubmenu(Menu, menu3);
			MenuController.BindMenuItem(Menu, menu3, menuItem6);
			Menu.CounterPreText = $"{num} weapons";
		}
		catch (Exception ex)
		{
			Print(ex);
			Menu.ClearMenuItems();
			Menu.AddErrorMenuItem(ex);
		}
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		RefreshMenu(forceReload: true);
	}

	private void RefreshWeaponMenuItem(Menu menu, MenuItem menuItem)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		WeaponDefinition weaponDefinition = menuItem.ItemData as WeaponDefinition;
		WeaponHash val = (WeaponHash)weaponDefinition.Hash;
		bool flag = ArmoryScript.IsWeaponEquipped(val);
		menu.InstructionalButtons.Clear();
		menu.InstructionalButtons.Add((Control)201, "Attachments");
		menu.InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		menu.InstructionalButtons.Add((Control)203, flag ? "Unequip" : "Equip");
		Control key = (Control)(Utils.IsUsingKeyboard() ? 289 : 72);
		menu.InstructionalButtons.Add(key, "Custom Texture");
		menuItem.RightIcon = (flag ? MenuItem.Icon.GUN : MenuItem.Icon.NONE);
		if (flag && !Game.PlayerPed.IsInVehicle() && !CuffedScript.IsCuffed && !SurrenderScript.IsSurrendered && SafezoneScript.Current == null)
		{
			Game.PlayerPed.Weapons.Select(val);
		}
		menuItem.Description = weaponDefinition.Description + (weaponDefinition.IsIllegal ? "\n~r~ILLEGAL" : "");
		if (weaponDefinition.Rarity > ItemRarity.Common)
		{
			if (!string.IsNullOrWhiteSpace(menuItem.Description))
			{
				menuItem.Description += "\n";
			}
			menuItem.Description += weaponDefinition.Rarity.ToMenuItemDescription();
		}
	}

	private void RefreshAttachmentMenuItem(Menu menu, MenuItem menuItem)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		Tuple<WeaponDefinition, WeaponComponentDefinition> tuple = menuItem.ItemData as Tuple<WeaponDefinition, WeaponComponentDefinition>;
		WeaponHash weaponHash = (WeaponHash)tuple.Item1.Hash;
		WeaponComponentHash attachmentHash = (WeaponComponentHash)tuple.Item2.Hash;
		bool flag = ArmoryScript.IsAttachmentEquipped(weaponHash, attachmentHash);
		menu.InstructionalButtons.Clear();
		menu.InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		if (tuple.Item2.Type != WeaponComponentType.Magazine)
		{
			menu.InstructionalButtons.Add((Control)203, flag ? "Unequip" : "Equip");
		}
		menuItem.RightIcon = (flag ? MenuItem.Icon.GUN : MenuItem.Icon.NONE);
	}

	private void OnMenuIndexChange(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex)
	{
		if (newItem.ItemData is WeaponDefinition)
		{
			RefreshWeaponMenuItem(menu, newItem);
		}
		else if (newItem.ItemData is Tuple<WeaponDefinition, WeaponComponentDefinition>)
		{
			RefreshAttachmentMenuItem(menu, newItem);
		}
	}

	private void OnEquipUnequip(Menu menu, Control control)
	{
		if (SafezoneScript.Current != null)
		{
			Utils.PlayErrorSound();
			return;
		}
		MenuItem currentMenuItem = menu.GetCurrentMenuItem();
		if (currentMenuItem.ItemData is WeaponDefinition weaponDefinition)
		{
			WeaponDefinition weaponDefinition2 = weaponDefinition;
			bool flag = ArmoryScript.IsWeaponEquipped((WeaponHash)weaponDefinition2.Hash);
			if ((flag && ArmoryScript.UnequipWeapon((WeaponHash)weaponDefinition2.Hash)) || (!flag && ArmoryScript.EquipWeapon((WeaponHash)weaponDefinition2.Hash)))
			{
				Utils.PlaySelectSound();
				RefreshWeaponMenuItem(menu, currentMenuItem);
			}
			else
			{
				Utils.PlayErrorSound();
			}
		}
		else
		{
			if (!(currentMenuItem.ItemData is Tuple<WeaponDefinition, WeaponComponentDefinition> { Item1: var item, Item2: var item2 }))
			{
				return;
			}
			if (item2.Type == WeaponComponentType.Magazine)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_ARMORY_MAGAZINE_REMOVE));
				Utils.PlayErrorSound();
				return;
			}
			bool flag2 = ArmoryScript.IsAttachmentEquipped((WeaponHash)item.Hash, (WeaponComponentHash)item2.Hash);
			if ((flag2 && ArmoryScript.UnequipAttachment((WeaponHash)item.Hash, (WeaponComponentHash)item2.Hash)) || (!flag2 && ArmoryScript.EquipAttachment((WeaponHash)item.Hash, (WeaponComponentHash)item2.Hash)))
			{
				Utils.PlaySelectSound();
				RefreshAttachmentMenuItem(menu, currentMenuItem);
			}
			else
			{
				Utils.PlayErrorSound();
			}
		}
	}

	private async void OnCustomTexture(Menu menu, Control control)
	{
		object itemData = menu.GetCurrentMenuItem().ItemData;
		if (!(itemData is WeaponDefinition weapInfo))
		{
			return;
		}
		if (weapInfo.TextureInfo == null)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_IMPLEMENTED_THIS_ITEM));
			return;
		}
		Dictionary<string, string> dictionary = await AskForTextures(weapInfo.TextureInfo);
		if (dictionary != null)
		{
			CustomWeaponTexturesScript.UpdateTextures(weapInfo.Id, dictionary);
		}
	}

	private async Task<Dictionary<string, string>> AskForTextures(WeaponTextureInfo textureInfo)
	{
		if (textureInfo.Txd == null)
		{
			return null;
		}
		if (textureMenu == null)
		{
			textureMenu = new Menu("Armory", "Custom Texture");
			textureMenu.OnItemSelect += OnTextureMenuItemSelect;
			MenuController.AddSubmenu(Menu, textureMenu);
		}
		textureMenu.ClearMenuItems();
		selectedTextures.Clear();
		selectedTextureInfo = textureInfo;
		if (!string.IsNullOrEmpty(textureInfo.DiffuseMap))
		{
			textureMenu.AddMenuItem(new MenuItem("Diffuse Map (Color)", "Use CBŚP's free tool: ~b~cbsp-cnr.eu/base64")
			{
				ItemData = textureInfo.DiffuseMap
			});
		}
		if (!string.IsNullOrEmpty(textureInfo.NormalMap))
		{
			textureMenu.AddMenuItem(new MenuItem("Normal Map", "Use CBŚP's free tool: ~b~cbsp-cnr.eu/base64")
			{
				ItemData = textureInfo.NormalMap
			});
		}
		if (!string.IsNullOrEmpty(textureInfo.SpecularMap))
		{
			textureMenu.AddMenuItem(new MenuItem("Specular Map", "Use CBŚP's free tool: ~b~cbsp-cnr.eu/base64")
			{
				ItemData = textureInfo.SpecularMap
			});
		}
		foreach (string otherTexture in textureInfo.OtherTextures)
		{
			textureMenu.AddMenuItem(new MenuItem(otherTexture ?? "", "Use CBŚP's free tool: ~b~cbsp-cnr.eu/base64")
			{
				ItemData = otherTexture
			});
		}
		MenuController.CloseAllMenus();
		textureMenu.OpenMenu();
		while (textureMenu.Visible)
		{
			await BaseScript.Delay(0);
		}
		return selectedTextures;
	}

	private async void OnTextureMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		string base64Texture = (await Utils.GetUserInput(menuItem.Text, "Enter base64 image", "", int.MaxValue))?.Trim();
		if (!string.IsNullOrEmpty(base64Texture))
		{
			string textureName = menuItem.ItemData as string;
			await selectedTextureInfo.ReplaceTexture(textureName, base64Texture);
			selectedTextures[textureName] = base64Texture;
		}
	}

	private void OnMenuRefresh(Menu menu, Control control)
	{
		RefreshMenu(forceReload: true);
	}

	private async void OnWeaponGive(Menu menu, Control control)
	{
		float num = 3f;
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		List<Player> list = new List<Player>();
		selectedWeapon = menu.GetCurrentMenuItem().ItemData as WeaponDefinition;
		foreach (Player player2 in ((BaseScript)this).Players)
		{
			if (!((Entity)(object)player2.Character == (Entity)null) && player2.Handle != Game.Player.Handle)
			{
				Vector3 position2 = ((Entity)player2.Character).Position;
				if (((Vector3)(ref position2)).DistanceToSquared(position) < num * num)
				{
					list.Add(player2);
				}
			}
		}
		if (list.Count == 0)
		{
			Utils.PlayErrorSound();
			Utils.DisplayHelpText("There are no ~r~nearby players ~s~to give your weapon to.", playSound: false);
			return;
		}
		if (list.Count == 1)
		{
			await TryGiveWeaponToPlayer(list.First().ServerId);
			Menu.Visible = true;
			RefreshMenu();
			return;
		}
		Menu playersMenu = new Menu(LocalizationController.S(Entries.Businesses.MENU_ARMORY_TITLE), LocalizationController.S(Entries.Businesses.MENU_ARMORY_GIVEWEAPON_SUBTITLE));
		foreach (Player player in list)
		{
			string text = await Authentication.GetAccountName(player);
			MenuItem item = new MenuItem(text, $"Give your ~y~{selectedWeapon} ~s~to <C>{text} ({player.ServerId})</C>~s~.")
			{
				ItemData = player.ServerId
			};
			playersMenu.AddMenuItem(item);
		}
		MenuController.CloseAllMenus();
		MenuController.AddMenu(menu);
		playersMenu.Visible = true;
		playersMenu.OnItemSelect += async delegate(Menu menu2, MenuItem menuItem, int itemIndex)
		{
			if (menuItem.ItemData is int num2 && API.GetPlayerFromServerId(num2) != -1)
			{
				await TryGiveWeaponToPlayer(num2);
			}
			playersMenu.Visible = false;
			Menu.Visible = true;
			RefreshMenu();
		};
	}

	private async Task TryGiveWeaponToPlayer(int targetId)
	{
		string targetName = await Authentication.GetAccountName(targetId);
		Ped val = new Ped(API.GetPlayerPed(API.GetPlayerFromServerId(targetId)));
		if (((Entity)val).IsDead)
		{
			Utils.DisplayHelpText("~r~You can't give a weapon to a dead player.");
			return;
		}
		Vector3 position = ((Entity)val).Position;
		if (((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) > 9f)
		{
			Utils.DisplayHelpText("~r~The player is too far, you can't give them a weapon.");
			return;
		}
		switch (await TriggerServerEventAsync<int>("gtacnr:armory:giveWeapon", new object[2] { targetId, selectedWeapon.Id }))
		{
		case 1:
			API.RemoveWeaponFromPed(API.PlayerPedId(), (uint)API.GetHashKey(selectedWeapon.Id));
			Utils.DisplayHelpText($"You gave {targetName} ({targetId}) a ~y~{selectedWeapon}~s~.");
			break;
		case 5:
			Utils.DisplayHelpText($"{targetName} ({targetId}) already has a ~y~{selectedWeapon}~s~.");
			break;
		default:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
			break;
		}
	}

	[EventHandler("gtacnr:armory:weaponGiven")]
	private async void OnWeaponGivenToMe(int token, int giverId, string weaponId)
	{
		WeaponHash weaponHash = (WeaponHash)API.GetHashKey(weaponId);
		if (ArmoryScript.HasWeapon(weaponHash))
		{
			Respond(response: false);
			return;
		}
		string arg = await Authentication.GetAccountName(giverId);
		WeaponDefinition weaponDefinition = Gtacnr.Data.Items.GetWeaponDefinition(weaponId);
		API.GiveWeaponToPed(API.PlayerPedId(), (uint)(int)weaponHash, 0, false, true);
		Utils.DisplayHelpText($"Received a ~y~{weaponDefinition} ~s~from <C>{arg} ({giverId})</C>.");
		Respond(response: true);
		void Respond(bool response)
		{
			BaseScript.TriggerServerEvent("gtacnr:armory:weaponGiven:response", new object[2] { token, response });
		}
	}

	private void OnWeaponDiscard(Menu menu, Control control)
	{
		Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_IMPLEMENTED), playSound: false);
		Utils.PlayErrorSound();
	}
}
