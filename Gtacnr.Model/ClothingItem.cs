using Gtacnr.Client;
using Gtacnr.Client.API;
using Gtacnr.Client.Premium;
using Gtacnr.Localization;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Model;

public class ClothingItem : InventoryItemBase
{
	public ClothingItemType Type { get; set; }

	public StaffLevel RequiredStaffLevel { get; set; }

	public string Department { get; set; }

	public bool AvailableAcrossAllJobs { get; set; }

	public bool Disabled { get; set; }

	public ClothingItemData Male { get; set; }

	public ClothingItemData Female { get; set; }

	public override string ToString()
	{
		return base.Name ?? "(Undefined)";
	}

	public bool HasSex(Sex sex)
	{
		if (sex != Sex.Male)
		{
			return Female != null;
		}
		return Male != null;
	}

	public ClothingItemData GetData(Sex sex)
	{
		if (sex != Sex.Male)
		{
			return Female;
		}
		return Male;
	}

	public ClothingItem()
	{
		base.Category = ItemCategory.Clothing;
	}

	public MenuItem ToMenuItem()
	{
		string text = (Disabled ? "~r~" : "");
		string description = base.Description ?? "";
		MenuItem menuItem = new MenuItem(text + base.Name, description);
		if (base.Name.Length > 40)
		{
			menuItem.Text = text + base.Name.Substring(0, 37) + "...";
			menuItem.Description = base.Name;
		}
		if (Disabled)
		{
			if (!string.IsNullOrWhiteSpace(menuItem.Description))
			{
				menuItem.Description += "\n";
			}
			menuItem.Description += "~y~This item has been disabled. Please Open A ticket to replace it, If you already have then ignore this~s~";
		}
		menuItem.ItemData = this;
		int levelByXP = Utils.GetLevelByXP(Users.CachedXP);
		if (base.RequiredLevel > levelByXP)
		{
			menuItem.Enabled = false;
			menuItem.RightIcon = MenuItem.Icon.LOCK;
			if (!string.IsNullOrWhiteSpace(menuItem.Description))
			{
				menuItem.Description += "\n";
			}
			menuItem.Description = menuItem.Description + "~b~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCK_LEVEL, base.RequiredLevel);
		}
		MembershipTier membershipTier = MembershipScript.MembershipTier;
		if ((int)base.RequiredMembership > 0)
		{
			if (!string.IsNullOrWhiteSpace(menuItem.Description))
			{
				menuItem.Description += "\n";
			}
			menuItem.Text = "~p~" + base.Name;
			if ((int)membershipTier < (int)base.RequiredMembership)
			{
				menuItem.Enabled = false;
				menuItem.RightIcon = MenuItem.Icon.LOCK;
				menuItem.Description = menuItem.Description + "~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_REQUIRES_MEMBERSHIP, Utils.GetDescription(base.RequiredMembership), ExternalLinks.Collection.Store);
			}
			else if (membershipTier == base.RequiredMembership)
			{
				MenuItem menuItem2 = menuItem;
				menuItem2.Description = menuItem2.Description + "~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP, Utils.GetDescription(membershipTier)) + "~s~\n" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP_REMINDER);
			}
			else
			{
				MenuItem menuItem2 = menuItem;
				menuItem2.Description = menuItem2.Description + "~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP, Utils.GetDescription(membershipTier)) + " " + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP_REQUIRES_LOWER_TIER, Utils.GetDescription(base.RequiredMembership)) + "~s~\n" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP_REMINDER);
			}
		}
		if (base.Rarity > ItemRarity.Common)
		{
			if (!string.IsNullOrWhiteSpace(menuItem.Description))
			{
				menuItem.Description += "\n";
			}
			menuItem.Description += base.Rarity.ToMenuItemDescription();
		}
		return menuItem;
	}
}
