using System.Threading.Tasks;

namespace MenuAPI;

public class MenuDynamicListItem : MenuItem
{
	public delegate string ChangeItemCallback(MenuDynamicListItem item, bool left);

	public bool HideArrowsWhenNotSelected { get; set; }

	public string CurrentItem { get; set; }

	public ChangeItemCallback Callback { get; set; }

	public MenuDynamicListItem(string text, string initialValue, ChangeItemCallback callback)
		: this(text, initialValue, callback, null)
	{
	}

	public MenuDynamicListItem(string text, string initialValue, ChangeItemCallback callback, string description)
		: base(text, description)
	{
		CurrentItem = initialValue;
		Callback = callback;
	}

	internal override async Task Draw(int indexOffset)
	{
		if (HideArrowsWhenNotSelected && !base.Selected)
		{
			base.Label = CurrentItem ?? "~r~N/A";
		}
		else
		{
			base.Label = "~s~‹ " + (CurrentItem ?? "~r~N/A~s~") + " ~s~›";
		}
		await base.Draw(indexOffset);
	}
}
