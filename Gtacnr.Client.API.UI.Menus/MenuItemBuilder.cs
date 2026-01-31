using MenuAPI;

namespace Gtacnr.Client.API.UI.Menus;

public class MenuItemBuilder
{
	private string? Id { get; set; }

	private string? Text { get; set; }

	private string? Description { get; set; }

	private string? Label { get; set; }

	private bool Enabled { get; set; }

	private object? ItemData { get; set; }

	private MenuItem.Icon LeftIcon { get; set; }

	private MenuItem.Icon RightIcon { get; set; }

	public MenuItemBuilder WithId(string id)
	{
		Id = id;
		return this;
	}

	public MenuItemBuilder WithText(string text)
	{
		Text = text;
		return this;
	}

	public MenuItemBuilder WithDescription(string description)
	{
		Description = description;
		return this;
	}

	public MenuItemBuilder WithLabel(string label)
	{
		Label = label;
		return this;
	}

	public MenuItemBuilder WithEnabled(bool isEnabled)
	{
		Enabled = isEnabled;
		return this;
	}

	public MenuItemBuilder WithItemData(object itemData)
	{
		ItemData = itemData;
		return this;
	}

	public MenuItemBuilder WithLeftIcon(MenuItem.Icon leftIcon)
	{
		LeftIcon = leftIcon;
		return this;
	}

	public MenuItemBuilder WithRightIcon(MenuItem.Icon rightIcon)
	{
		RightIcon = rightIcon;
		return this;
	}

	public MenuItem Build()
	{
		return new MenuItem(Text, Description)
		{
			Id = Id,
			Label = Label,
			Enabled = Enabled,
			ItemData = ItemData,
			LeftIcon = LeftIcon,
			RightIcon = RightIcon
		};
	}
}
