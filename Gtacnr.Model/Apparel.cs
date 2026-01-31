using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client;
using Gtacnr.Client.Events.Holidays.AprilsFools;
using Gtacnr.Data;
using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class Apparel
{
	private readonly List<string> _data = new List<string>();

	private static int aprilFoolsMask = Utils.GetRandomInt(149, 154);

	public IReadOnlyCollection<string> Items => _data;

	public event EventHandler<ApparelChangedEventArgs> Changed;

	private void TriggerApparelChanged(ApparelChangedEventArgs args)
	{
		try
		{
			this.Changed?.Invoke(this, args);
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.ToString());
		}
	}

	public Apparel()
	{
	}

	public Apparel(IEnumerable<string> itemIds)
		: this()
	{
		Set(itemIds);
	}

	public Apparel(IEnumerable<ClothingItem> items)
		: this()
	{
		Set(items);
	}

	public Apparel(string itemId)
		: this()
	{
		Add(itemId);
	}

	public Apparel(ClothingItem item)
		: this()
	{
		Add(item);
	}

	public Apparel(Apparel apparel)
		: this()
	{
		foreach (string item in apparel.Items)
		{
			Add(item);
		}
	}

	public bool IsEquipped(string itemId)
	{
		return _data.Contains(itemId);
	}

	public void Set(IEnumerable<string> itemIds)
	{
		_data.Clear();
		if (itemIds != null)
		{
			foreach (string itemId in itemIds)
			{
				if (Gtacnr.Data.Items.GetClothingItemDefinition(itemId) != null)
				{
					Replace(itemId);
				}
			}
		}
		TriggerApparelChanged(new ApparelChangedEventArgs());
	}

	public void Set(IEnumerable<ClothingItem> itemIds)
	{
		Set(itemIds.Select((ClothingItem item) => item.Id));
	}

	public void Set(Apparel apparel)
	{
		Set(apparel.Items);
	}

	public bool Add(string itemId)
	{
		ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(itemId);
		if (clothingItemDefinition == null)
		{
			return false;
		}
		if (clothingItemDefinition.Disabled)
		{
			return false;
		}
		if (_data.Contains(itemId))
		{
			return false;
		}
		_data.Add(itemId);
		TriggerApparelChanged(new ApparelChangedEventArgs());
		return true;
	}

	public bool Add(ClothingItem item)
	{
		return Add(item?.Id);
	}

	public bool Replace(string itemId)
	{
		ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(itemId);
		if (clothingItemDefinition == null)
		{
			return false;
		}
		if (clothingItemDefinition.Disabled)
		{
			return false;
		}
		if (_data.Contains(itemId))
		{
			_data.Remove(itemId);
		}
		List<string> conflictingItems = new List<string>();
		foreach (string datum in _data)
		{
			string existingItemId = datum;
			ClothingItem clothingItemDefinition2 = Gtacnr.Data.Items.GetClothingItemDefinition(existingItemId);
			if (clothingItemDefinition2.Type != ClothingItemType.Tattoos && clothingItemDefinition2.Type != ClothingItemType.Staff && ((clothingItemDefinition2.Type != ClothingItemType.Outfits && clothingItemDefinition2.Type != ClothingItemType.Uniforms) || clothingItemDefinition.Type == ClothingItemType.Outfits || clothingItemDefinition.Type == ClothingItemType.Uniforms))
			{
				if (clothingItemDefinition2.HasSex(Sex.Male) && clothingItemDefinition.HasSex(Sex.Male))
				{
					RemoveConflicts(clothingItemDefinition2.Male, clothingItemDefinition.Male);
				}
				if (clothingItemDefinition2.HasSex(Sex.Female) && clothingItemDefinition.HasSex(Sex.Female))
				{
					RemoveConflicts(clothingItemDefinition2.Female, clothingItemDefinition.Female);
				}
			}
			void RemoveConflicts(ClothingItemData existingData, ClothingItemData newData)
			{
				if ((existingData.Components.Any((ComponentVariation c) => newData.Components.Select((ComponentVariation x) => x.Index).Except(new int[2] { 3, 8 }).Contains(c.Index)) || existingData.Props.Any((Accessory c) => newData.Props.Select((Accessory x) => x.Index).Contains(c.Index)) || existingData.HeadOverlays.Any((HeadOverlay c) => newData.HeadOverlays.Select((HeadOverlay x) => x.Index).Contains(c.Index))) && !conflictingItems.Contains(existingItemId))
				{
					conflictingItems.Add(existingItemId);
				}
			}
		}
		foreach (string item in conflictingItems)
		{
			_data.Remove(item);
		}
		_data.Add(itemId);
		TriggerApparelChanged(new ApparelChangedEventArgs());
		return true;
	}

	public bool Replace(ClothingItem item)
	{
		return Replace(item?.Id);
	}

	public bool Remove(string itemId)
	{
		if (Gtacnr.Data.Items.GetClothingItemDefinition(itemId) == null)
		{
			return false;
		}
		if (!_data.Remove(itemId))
		{
			return false;
		}
		TriggerApparelChanged(new ApparelChangedEventArgs());
		return true;
	}

	public bool Remove(ClothingItem item)
	{
		return Remove(item?.Id);
	}

	public bool Remove(ClothingItemType itemType)
	{
		string text = _data.FirstOrDefault((string i) => Gtacnr.Data.Items.GetClothingItemDefinition(i).Type == itemType);
		if ((text == null || !_data.Remove(text)) && (from i in _data
			select Gtacnr.Data.Items.GetClothingItemDefinition(i) into def
			where def.Type.In(ClothingItemType.Outfits, ClothingItemType.Uniforms, ClothingItemType.Staff)
			select def).Any() && Add(GetCleanupItemByCategory(itemType)))
		{
			return true;
		}
		TriggerApparelChanged(new ApparelChangedEventArgs());
		return true;
	}

	public bool Remove(HashSet<string> ids)
	{
		HashSet<string> hashSet = new HashSet<string>(_data);
		hashSet.IntersectWith(ids);
		if (hashSet.Count <= 0)
		{
			return false;
		}
		foreach (string item in hashSet)
		{
			_data.Remove(item);
		}
		TriggerApparelChanged(new ApparelChangedEventArgs());
		return true;
	}

	public void Clear()
	{
		_data.Clear();
		TriggerApparelChanged(new ApparelChangedEventArgs());
	}

	public override string ToString()
	{
		return string.Join(", ", Items ?? new string[0]).TrimEnd().TrimEnd(',');
	}

	public static Apparel GetDefault()
	{
		return new Apparel(new string[1] { "default_outfit" });
	}

	public static Apparel GetUnderwear()
	{
		return new Apparel(new string[1] { "underwear_outfit" });
	}

	public static Apparel GetDefault(string job, Sex sex = Sex.Male)
	{
		Job jobData = Jobs.GetJobData(job);
		if (jobData == null || !jobData.SeparateOutfit || jobData.DefaultOutfits == null || !jobData.DefaultOutfits.ContainsKey(sex))
		{
			return GetDefault();
		}
		if (!jobData.DefaultOutfits[sex].ContainsKey("default"))
		{
			return GetDefault();
		}
		return new Apparel(jobData.DefaultOutfits[sex]["default"]);
	}

	public static string GetCleanupItemByCategory(ClothingItemType itemType)
	{
		return itemType switch
		{
			ClothingItemType.Hats => "hats_none", 
			ClothingItemType.Armor => "armor_none", 
			ClothingItemType.Tops => "tops_none", 
			ClothingItemType.Pants => "pants_none", 
			ClothingItemType.Shoes => "shoes_none", 
			ClothingItemType.Masks => "masks_none", 
			ClothingItemType.Glasses => "glasses_none", 
			_ => "", 
		};
	}

	public void ApplyOnPed(Ped ped)
	{
		if ((Entity)(object)ped == (Entity)null || !ped.Exists() || !Gtacnr.Client.Utils.IsFreemodePed(ped))
		{
			return;
		}
		API.SetPedComponentVariation(((PoolObject)ped).Handle, 2, 0, 0, 0);
		Sex freemodePedSex = Gtacnr.Client.Utils.GetFreemodePedSex(ped);
		foreach (string datum in _data)
		{
			ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(datum);
			if (clothingItemDefinition == null)
			{
				Debug.WriteLine($"Warning: unable to apply clothing item `{datum}` to ped ID {((PoolObject)ped).Handle}.");
				continue;
			}
			if (clothingItemDefinition.Disabled)
			{
				Debug.WriteLine($"Warning: ignoring disabled clothing item `{datum}` when applying to ped ID {((PoolObject)ped).Handle}.");
				continue;
			}
			ClothingItemData data = clothingItemDefinition.GetData(freemodePedSex);
			if (data == null)
			{
				continue;
			}
			foreach (ComponentVariation component in data.Components)
			{
				int num = component.Drawable;
				if (num < 0)
				{
					num = 0;
				}
				API.SetPedComponentVariation(((PoolObject)ped).Handle, component.Index, num, component.Texture, component.Palette);
				if (component.Index == 2 && Gtacnr.Client.Utils.HairOverlays.ContainsKey(component.Drawable))
				{
					API.SetPedFacialDecoration(((PoolObject)ped).Handle, (uint)API.GetHashKey(Gtacnr.Client.Utils.HairOverlays[component.Drawable].Key), (uint)API.GetHashKey(Gtacnr.Client.Utils.HairOverlays[component.Drawable].Value));
				}
			}
			foreach (Accessory prop in data.Props)
			{
				if (prop.Drawable == -1)
				{
					API.ClearPedProp(((PoolObject)ped).Handle, prop.Index);
				}
				else
				{
					API.SetPedPropIndex(((PoolObject)ped).Handle, prop.Index, prop.Drawable, prop.Texture, true);
				}
			}
			foreach (HeadOverlay headOverlay in data.HeadOverlays)
			{
				int num2 = ((headOverlay.Index != 8) ? 1 : 2);
				API.SetPedHeadOverlay(((PoolObject)ped).Handle, headOverlay.Index, headOverlay.Overlay, headOverlay.Opacity);
				API.SetPedHeadOverlayColor(((PoolObject)ped).Handle, headOverlay.Index, num2, headOverlay.Color, 0);
			}
			foreach (Decoration decoration in data.Decorations)
			{
				uint num3 = (uint)Utils.GenerateHash(decoration.Collection);
				uint num4 = (uint)Utils.GenerateHash(decoration.Name);
				API.SetPedDecoration(((PoolObject)ped).Handle, num3, num4);
			}
		}
		if (AprilsFoolsScript.IsAprilsFools && (Entity)(object)ped == (Entity)(object)Game.PlayerPed)
		{
			API.SetPedComponentVariation(((PoolObject)ped).Handle, 1, aprilFoolsMask, 0, 0);
		}
	}

	public AppliedApparelData GetAppliedData(Ped ped)
	{
		Dictionary<int, Tuple<int, int, int>> dictionary = new Dictionary<int, Tuple<int, int, int>>();
		Dictionary<int, Tuple<int, int>> dictionary2 = new Dictionary<int, Tuple<int, int>>();
		dictionary[2] = Tuple.Create(0, 0, 0);
		Sex freemodePedSex = Gtacnr.Client.Utils.GetFreemodePedSex(ped);
		foreach (string datum in _data)
		{
			ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(datum);
			if (clothingItemDefinition == null || clothingItemDefinition.Disabled)
			{
				continue;
			}
			ClothingItemData data = clothingItemDefinition.GetData(freemodePedSex);
			if (data == null)
			{
				continue;
			}
			foreach (ComponentVariation component in data.Components)
			{
				int num = component.Drawable;
				if (num < 0)
				{
					num = 0;
				}
				dictionary[component.Index] = Tuple.Create(num, component.Texture, component.Palette);
			}
			foreach (Accessory prop in data.Props)
			{
				if (prop.Drawable == -1)
				{
					dictionary2[prop.Index] = Tuple.Create(prop.Drawable, 0);
				}
				else
				{
					dictionary2[prop.Index] = Tuple.Create(prop.Drawable, prop.Texture);
				}
			}
			foreach (HeadOverlay headOverlay in data.HeadOverlays)
			{
				_ = headOverlay;
			}
			foreach (Decoration decoration in data.Decorations)
			{
				_ = decoration;
			}
		}
		return new AppliedApparelData
		{
			ComponentVariations = dictionary,
			PropVariations = dictionary2
		};
	}

	public void ApplyOnPlayer()
	{
		ApplyOnPed(Game.PlayerPed);
	}

	public static void ClearFromPed(Ped ped, bool clearMakeup = true, bool clearDecorations = false, bool clearDamage = false, bool washAndDryClothes = true)
	{
		API.ClearAllPedProps(((PoolObject)ped).Handle);
		GetUnderwear().ApplyOnPed(ped);
		if (clearMakeup)
		{
			API.SetPedHeadOverlay(((PoolObject)ped).Handle, 4, 0, 0f);
		}
		if (clearDecorations)
		{
			if (clearDamage)
			{
				API.ClearPedDecorations(((PoolObject)ped).Handle);
			}
			else
			{
				API.ClearPedDecorationsLeaveScars(((PoolObject)ped).Handle);
			}
		}
		if (clearDamage)
		{
			Gtacnr.Client.Utils.ClearPedDamage(ped, blood: true, wetness: false, dirt: false);
		}
		if (washAndDryClothes)
		{
			API.ClearPedEnvDirt(((PoolObject)ped).Handle);
			API.ClearPedWetness(((PoolObject)ped).Handle);
		}
	}

	public void ClearFromPlayer()
	{
		ClearFromPed(Game.PlayerPed);
	}
}
