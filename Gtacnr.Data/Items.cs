using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Model;

namespace Gtacnr.Data;

public static class Items
{
	private static readonly Dictionary<string, InventoryItem> _itemDefinitions;

	private static readonly Dictionary<string, WeaponDefinition> _weaponDefinitions;

	private static readonly Dictionary<string, WeaponComponentDefinition> _weaponComponentDefinitions;

	private static readonly Dictionary<string, AmmoDefinition> _ammoDefinitions;

	private static readonly Dictionary<string, ClothingItem> _clothingDefinitions;

	private static readonly Dictionary<string, Service> _serviceDefinitions;

	private static readonly Dictionary<uint, WeaponDefinition> _weaponDefinitionsByHash;

	private static readonly Dictionary<int, ClothingItem> _clothingDefinitionsByHash;

	public const string ITEMS_RESOURCE = "gtacnr_items";

	static Items()
	{
		_itemDefinitions = InitializeItemDefinitions();
		_weaponDefinitions = new Dictionary<string, WeaponDefinition>();
		_weaponComponentDefinitions = InitializeWeaponComponentDefinitions();
		_ammoDefinitions = InitializeAmmoDefinitions();
		_clothingDefinitions = new Dictionary<string, ClothingItem>();
		_serviceDefinitions = InitializeServiceDefinitions();
		_weaponDefinitionsByHash = new Dictionary<uint, WeaponDefinition>();
		_clothingDefinitionsByHash = new Dictionary<int, ClothingItem>();
		InitializeWeaponDefinitions();
		InitializeClothingDefinitions();
	}

	private static void InitializeWeaponDefinitions()
	{
		try
		{
			foreach (WeaponDefinition item in Utils.LoadJson<List<WeaponDefinition>>("gtacnr_items", "data/weapons/weapons.json"))
			{
				if (item != null && !string.IsNullOrWhiteSpace(item.Id) && !_weaponDefinitions.ContainsKey(item.Id))
				{
					item.Hash = API.GetHashKey(item.Id);
					item.Name = Utils.ResolveLocalization(item.Name);
					item.Description = Utils.ResolveLocalization(item.Description);
					if (item.Disclaimer != null)
					{
						WeaponDefinition weaponDefinition = item;
						weaponDefinition.Description = weaponDefinition.Description + "\n~r~" + Utils.ResolveLocalization("@{inventory_disclaimer}") + ": ~s~" + Utils.ResolveLocalization(item.Disclaimer);
					}
					if (item.ShouldAddDefaultMultipliers)
					{
						item.EconomyMultipliers.InsertRange(0, new string[2] { "global", "weapons" });
					}
					_weaponDefinitions[item.Id] = item;
					_weaponDefinitionsByHash[(uint)item.Hash] = item;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("An exception has occurred while parsing weapons! Please fix data/weapons/weapons.json");
			Debug.WriteLine(ex.ToString());
		}
	}

	private static Dictionary<string, InventoryItem> InitializeItemDefinitions()
	{
		Dictionary<string, InventoryItem> dictionary = new Dictionary<string, InventoryItem>();
		string text = "data/items/files.json";
		try
		{
			foreach (string item in Utils.LoadJson<List<string>>("gtacnr_items", "data/items/files.json"))
			{
				text = item;
				foreach (InventoryItem item2 in API.LoadResourceFile("gtacnr_items", "data/items/" + item).Unjson<List<InventoryItem>>())
				{
					if (item2 != null && !string.IsNullOrWhiteSpace(item2.Id) && !dictionary.ContainsKey(item2.Id))
					{
						item2.Name = Utils.ResolveLocalization(item2.Name);
						item2.Description = Utils.ResolveLocalization(item2.Description);
						if (item2.Disclaimer != null)
						{
							InventoryItem inventoryItem = item2;
							inventoryItem.Description = inventoryItem.Description + "\n~r~" + Utils.ResolveLocalization("@{inventory_disclaimer}") + ": ~s~" + Utils.ResolveLocalization(item2.Disclaimer);
						}
						dictionary[item2.Id] = item2;
						if (item2.ShouldAddDefaultMultipliers)
						{
							item2.EconomyMultipliers.InsertRange(0, new string[2]
							{
								"global",
								item2.Category.ToString().ToLower()
							});
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("An exception has occurred while parsing items! Please fix " + text);
			Debug.WriteLine(ex.ToString());
		}
		return dictionary;
	}

	private static Dictionary<string, WeaponComponentDefinition> InitializeWeaponComponentDefinitions()
	{
		Dictionary<string, WeaponComponentDefinition> dictionary = new Dictionary<string, WeaponComponentDefinition>();
		try
		{
			foreach (WeaponComponentDefinition item in API.LoadResourceFile("gtacnr_items", "data/weapons/attachments.json").Unjson<List<WeaponComponentDefinition>>())
			{
				if (item != null && !string.IsNullOrWhiteSpace(item.Id) && !dictionary.ContainsKey(item.Id))
				{
					item.Hash = API.GetHashKey(item.Id);
					item.Name = Utils.ResolveLocalization(item.Name);
					item.Description = Utils.ResolveLocalization(item.Description);
					if (item.Disclaimer != null)
					{
						WeaponComponentDefinition weaponComponentDefinition = item;
						weaponComponentDefinition.Description = weaponComponentDefinition.Description + "\n~r~" + Utils.ResolveLocalization("@{inventory_disclaimer}") + ": ~s~" + Utils.ResolveLocalization(item.Disclaimer);
					}
					if (item.ShouldAddDefaultMultipliers)
					{
						item.EconomyMultipliers.InsertRange(0, new string[2] { "global", "attachments" });
					}
					dictionary[item.Id] = item;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("An exception has occurred while parsing items! Please fix data/weapons/attachments.json");
			Debug.WriteLine(ex.ToString());
		}
		return dictionary;
	}

	private static Dictionary<string, AmmoDefinition> InitializeAmmoDefinitions()
	{
		Dictionary<string, AmmoDefinition> dictionary = new Dictionary<string, AmmoDefinition>();
		try
		{
			foreach (AmmoDefinition item in API.LoadResourceFile("gtacnr_items", "data/weapons/ammo.json").Unjson<List<AmmoDefinition>>())
			{
				if (item != null && !string.IsNullOrWhiteSpace(item.Id) && !dictionary.ContainsKey(item.Id))
				{
					item.Hash = API.GetHashKey(item.Id);
					item.Name = Utils.ResolveLocalization(item.Name);
					item.Description = Utils.ResolveLocalization(item.Description);
					if (item.Disclaimer != null)
					{
						AmmoDefinition ammoDefinition = item;
						ammoDefinition.Description = ammoDefinition.Description + "\n~r~" + Utils.ResolveLocalization("@{inventory_disclaimer}") + ": ~s~" + Utils.ResolveLocalization(item.Disclaimer);
					}
					if (item.ShouldAddDefaultMultipliers)
					{
						item.EconomyMultipliers.InsertRange(0, new string[2] { "global", "ammo" });
					}
					dictionary[item.Id] = item;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("An exception has occurred while parsing items! Please fix data/weapons/ammo.json");
			Debug.WriteLine(ex.ToString());
		}
		return dictionary;
	}

	private static void InitializeClothingDefinitions()
	{
		string text = "data/clothes/files.json";
		try
		{
			_clothingDefinitions.Clear();
			_clothingDefinitionsByHash.Clear();
			foreach (string item in API.LoadResourceFile("gtacnr_items", "data/clothes/files.json").Unjson<List<string>>())
			{
				text = item;
				foreach (ClothingItem item2 in API.LoadResourceFile("gtacnr_items", "data/clothes/" + item).Unjson<List<ClothingItem>>())
				{
					if (item2 != null && !string.IsNullOrWhiteSpace(item2.Id) && !_clothingDefinitions.ContainsKey(item2.Id))
					{
						item2.Name = Utils.ResolveGTALabel(item2.Name);
						item2.Name = Utils.ResolveLocalization(item2.Name);
						item2.Description = Utils.ResolveLocalization(item2.Description);
						if (item2.Disclaimer != null)
						{
							ClothingItem clothingItem = item2;
							clothingItem.Description = clothingItem.Description + "\n~r~" + Utils.ResolveLocalization("@{inventory_disclaimer}") + ": ~s~" + Utils.ResolveLocalization(item2.Disclaimer);
						}
						if (item2.ShouldAddDefaultMultipliers)
						{
							item2.EconomyMultipliers.InsertRange(0, new string[2] { "global", "clothes" });
						}
						_clothingDefinitions[item2.Id] = item2;
						_clothingDefinitionsByHash[Utils.GenerateHash(item2.Id)] = item2;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("An exception has occurred while parsing items! Please fix " + text);
			Debug.WriteLine(ex.ToString());
		}
	}

	private static Dictionary<string, Service> InitializeServiceDefinitions()
	{
		Dictionary<string, Service> dictionary = new Dictionary<string, Service>();
		string text = "data/services/files.json";
		try
		{
			foreach (string item in API.LoadResourceFile("gtacnr_items", "data/services/files.json").Unjson<List<string>>())
			{
				text = item;
				foreach (Service item2 in API.LoadResourceFile("gtacnr_items", "data/services/" + item).Unjson<List<Service>>())
				{
					if (item2 != null && !string.IsNullOrWhiteSpace(item2.Id) && !dictionary.ContainsKey(item2.Id))
					{
						item2.Name = Utils.ResolveLocalization(item2.Name);
						item2.Description = Utils.ResolveLocalization(item2.Description);
						if (item2.Disclaimer != null)
						{
							Service service = item2;
							service.Description = service.Description + "\n" + Utils.ResolveLocalization("@{inventory_disclaimer}") + ": " + Utils.ResolveLocalization(item2.Disclaimer);
						}
						if (item2.ShouldAddDefaultMultipliers)
						{
							item2.EconomyMultipliers.InsertRange(0, new string[2] { "global", "services" });
						}
						dictionary[item2.Id] = item2;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("An exception has occurred while parsing items! Please fix " + text);
			Debug.WriteLine(ex.ToString());
		}
		return dictionary;
	}

	public static bool IsItemDefined(string itemId)
	{
		return _itemDefinitions.ContainsKey(itemId);
	}

	public static InventoryItem? GetItemDefinition(string itemId)
	{
		if (_itemDefinitions.ContainsKey(itemId))
		{
			return _itemDefinitions[itemId];
		}
		return null;
	}

	public static IEnumerable<InventoryItem> GetAllItemDefinitions()
	{
		return _itemDefinitions.Values.AsEnumerable();
	}

	public static bool IsWeaponDefined(string weaponId)
	{
		return _weaponDefinitions.ContainsKey(weaponId);
	}

	public static WeaponDefinition? GetWeaponDefinition(string weaponId)
	{
		if (_weaponDefinitions.ContainsKey(weaponId))
		{
			return _weaponDefinitions[weaponId];
		}
		return null;
	}

	public static WeaponDefinition? GetWeaponDefinitionByHash(uint weaponHash)
	{
		if (_weaponDefinitionsByHash.ContainsKey(weaponHash))
		{
			return _weaponDefinitionsByHash[weaponHash];
		}
		return null;
	}

	public static IEnumerable<WeaponDefinition> GetAllWeaponDefinitions()
	{
		return _weaponDefinitions.Values.AsEnumerable();
	}

	public static IEnumerable<uint> GetAllWeaponHashes()
	{
		return _weaponDefinitionsByHash.Keys.AsEnumerable();
	}

	public static bool IsWeaponComponentDefined(string componentId)
	{
		return _weaponComponentDefinitions.ContainsKey(componentId);
	}

	public static WeaponComponentDefinition? GetWeaponComponentDefinition(string componentId)
	{
		if (_weaponComponentDefinitions.ContainsKey(componentId))
		{
			return _weaponComponentDefinitions[componentId];
		}
		return null;
	}

	public static IEnumerable<WeaponComponentDefinition> GetAllWeaponComponentDefinitions()
	{
		return _weaponComponentDefinitions.Values.AsEnumerable();
	}

	public static bool IsAmmoDefined(string ammoId)
	{
		return _ammoDefinitions.ContainsKey(ammoId);
	}

	public static AmmoDefinition? GetAmmoDefinition(string ammoId)
	{
		if (_ammoDefinitions.ContainsKey(ammoId))
		{
			return _ammoDefinitions[ammoId];
		}
		return null;
	}

	public static IEnumerable<AmmoDefinition> GetAllAmmoDefinitions()
	{
		return _ammoDefinitions.Values.AsEnumerable();
	}

	public static bool IsClothingItemDefined(string clothingItemId)
	{
		return _clothingDefinitions.ContainsKey(clothingItemId);
	}

	public static ClothingItem? GetClothingItemDefinition(string clothingItemId)
	{
		if (_clothingDefinitions.ContainsKey(clothingItemId))
		{
			return _clothingDefinitions[clothingItemId];
		}
		return null;
	}

	public static ClothingItem? GetClothingItemDefinitionByHash(int clothingItemIdHash)
	{
		if (_clothingDefinitionsByHash.ContainsKey(clothingItemIdHash))
		{
			return _clothingDefinitionsByHash[clothingItemIdHash];
		}
		return null;
	}

	public static IEnumerable<ClothingItem> GetAllClothingItemDefinitions()
	{
		return _clothingDefinitions.Values.AsEnumerable();
	}

	public static bool IsServiceDefined(string serviceId)
	{
		return _serviceDefinitions.ContainsKey(serviceId);
	}

	public static Service? GetServiceDefinition(string serviceId)
	{
		if (_serviceDefinitions.ContainsKey(serviceId))
		{
			return _serviceDefinitions[serviceId];
		}
		return null;
	}

	public static IEnumerable<Service> GetAllServiceDefinitions()
	{
		return _serviceDefinitions.Values.AsEnumerable();
	}

	public static bool IsItemBaseDefined(string itemId)
	{
		if (!_itemDefinitions.ContainsKey(itemId) && !_weaponDefinitions.ContainsKey(itemId) && !_weaponComponentDefinitions.ContainsKey(itemId) && !_ammoDefinitions.ContainsKey(itemId))
		{
			return _clothingDefinitions.ContainsKey(itemId);
		}
		return true;
	}

	public static InventoryItemBase? GetItemBaseDefinition(string itemId)
	{
		if (_itemDefinitions.ContainsKey(itemId))
		{
			return _itemDefinitions[itemId];
		}
		if (_weaponDefinitions.ContainsKey(itemId))
		{
			return _weaponDefinitions[itemId];
		}
		if (_weaponComponentDefinitions.ContainsKey(itemId))
		{
			return _weaponComponentDefinitions[itemId];
		}
		if (_ammoDefinitions.ContainsKey(itemId))
		{
			return _ammoDefinitions[itemId];
		}
		if (_clothingDefinitions.ContainsKey(itemId))
		{
			return _clothingDefinitions[itemId];
		}
		return null;
	}

	public static IEnumerable<InventoryItemBase> GetAllInventoryItemBaseDefinitions()
	{
		return GetAllItemDefinitions().Cast<InventoryItemBase>().Concat(GetAllWeaponDefinitions()).Concat(GetAllWeaponComponentDefinitions())
			.Concat(GetAllAmmoDefinitions())
			.Concat(GetAllClothingItemDefinitions());
	}
}
