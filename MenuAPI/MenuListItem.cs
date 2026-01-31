using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MenuAPI;

public class MenuListItem : MenuItem
{
	public enum ColorPanelType
	{
		Hair,
		Makeup
	}

	public ColorPanelType ColorPanelColorType;

	public int ListIndex { get; set; }

	public List<string> ListItems { get; set; } = new List<string>();

	public bool HideArrowsWhenNotSelected { get; set; } = true;

	public bool ShowOpacityPanel { get; set; }

	public bool ShowColorPanel { get; set; }

	public bool DisableDrawList { get; set; }

	public int ItemsCount => ListItems.Count;

	public string GetCurrentSelection()
	{
		if (ItemsCount > 0 && ListIndex >= 0 && ListIndex < ItemsCount)
		{
			return ListItems[ListIndex];
		}
		return null;
	}

	public MenuListItem(string text, IEnumerable<string> items)
		: this(text, items, 0, null)
	{
	}

	public MenuListItem(string text, IEnumerable<string> items, int index)
		: this(text, items, index, null)
	{
	}

	public MenuListItem(string text, IEnumerable<string> items, int index, string description)
		: base(text, description)
	{
		ListItems = items.ToList();
		ListIndex = index;
	}

	internal override async Task Draw(int indexOffset)
	{
		if (ItemsCount < 1)
		{
			ListItems.Add("N/A");
		}
		while (ListIndex < 0)
		{
			ListIndex += ItemsCount;
		}
		while (ListIndex >= ItemsCount)
		{
			ListIndex -= ItemsCount;
		}
		if (!DisableDrawList)
		{
			if (HideArrowsWhenNotSelected && !base.Selected)
			{
				base.Label = GetCurrentSelection() ?? "N/A";
			}
			else
			{
				base.Label = "‹ " + (GetCurrentSelection() ?? "N/A") + " ›";
			}
		}
		await base.Draw(indexOffset);
	}
}
