using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Client.Libs;
using MenuAPI;

namespace Gtacnr.Client.API.UI.Menus;

public class MenuBuilder
{
	private string? Title { get; set; }

	private string? Subtitle { get; set; }

	private string? CounterPreText { get; set; }

	private Font HeaderFont { get; set; }

	private float HeaderFontSize { get; set; }

	private KeyValuePair<string, string> HeaderTexture { get; set; }

	private bool PlaySelectSound { get; set; }

	private List<MenuItemBuilder> MenuItems { get; set; } = new List<MenuItemBuilder>();

	private Dictionary<Control, string> InstructionalButtons { get; set; } = new Dictionary<Control, string>();

	private List<Menu.ButtonPressHandler> ButtonPressHandlers { get; set; } = new List<Menu.ButtonPressHandler>();

	public MenuBuilder WithTitle(string title)
	{
		Title = title;
		return this;
	}

	public MenuBuilder WithSubtitle(string subtitle)
	{
		Subtitle = subtitle;
		return this;
	}

	public MenuBuilder WithCounterPreText(string counterPreText)
	{
		CounterPreText = counterPreText;
		return this;
	}

	public MenuBuilder WithHeaderFont(Font headerFont)
	{
		HeaderFont = headerFont;
		return this;
	}

	public MenuBuilder WithHeaderFontSize(float headerFontSize)
	{
		HeaderFontSize = headerFontSize;
		return this;
	}

	public MenuBuilder WithHeaderTexture(KeyValuePair<string, string> texture)
	{
		HeaderTexture = texture;
		return this;
	}

	public MenuBuilder AddMenuItem(MenuItemBuilder menuItemBuilder)
	{
		MenuItems.Add(menuItemBuilder);
		return this;
	}

	public MenuBuilder AddInstructionalButton(Control control, string label)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		InstructionalButtons[control] = label;
		return this;
	}

	public MenuBuilder AddButtonPressHandler(Menu.ButtonPressHandler handler)
	{
		ButtonPressHandlers.Add(handler);
		return this;
	}

	public Menu Build()
	{
		Menu menu = new Menu(Title);
		MenuController.AddMenu(menu);
		ApplyTo(menu);
		return menu;
	}

	public void ApplyTo(Menu menu)
	{
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		menu.MenuTitle = Title;
		menu.MenuSubtitle = Subtitle;
		menu.CounterPreText = CounterPreText;
		menu.HeaderFont = HeaderFont;
		menu.HeaderFontSize = HeaderFontSize;
		menu.HeaderTexture = HeaderTexture;
		menu.PlaySelectSound = PlaySelectSound;
		if (MenuItems.Count > 0)
		{
			menu.ClearMenuItems();
			foreach (MenuItemBuilder menuItem2 in MenuItems)
			{
				MenuItem menuItem = menuItem2.Build();
				menuItem.ParentMenu = menu;
				menu.AddMenuItem(menuItem);
			}
		}
		if (InstructionalButtons.Count > 0)
		{
			menu.InstructionalButtons.Clear();
			foreach (KeyValuePair<Control, string> instructionalButton in InstructionalButtons)
			{
				menu.InstructionalButtons.Add(instructionalButton.Key, instructionalButton.Value);
			}
		}
		if (ButtonPressHandlers.Count > 0)
		{
			menu.ButtonPressHandlers.Clear();
			menu.ButtonPressHandlers.AddRange(ButtonPressHandlers);
		}
	}
}
