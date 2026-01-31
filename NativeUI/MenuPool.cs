using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using NativeUI.PauseMenu;

namespace NativeUI;

public class MenuPool
{
	public bool BannerInheritance = true;

	public bool OffsetInheritance = true;

	private readonly List<UIMenu> _menuList = new List<UIMenu>();

	private readonly List<TabView> _pauseMenus = new List<TabView>();

	public bool MouseEdgeEnabled
	{
		set
		{
			_menuList.ForEach(delegate(UIMenu m)
			{
				m.MouseEdgeEnabled = value;
			});
		}
	}

	public bool ControlDisablingEnabled
	{
		set
		{
			_menuList.ForEach(delegate(UIMenu m)
			{
				m.ControlDisablingEnabled = value;
			});
		}
	}

	public bool ResetCursorOnOpen
	{
		set
		{
			_menuList.ForEach(delegate(UIMenu m)
			{
				m.ResetCursorOnOpen = value;
			});
		}
	}

	public bool FormatDescriptions
	{
		set
		{
			_menuList.ForEach(delegate(UIMenu m)
			{
				m.FormatDescriptions = value;
			});
		}
	}

	public string AUDIO_LIBRARY
	{
		set
		{
			_menuList.ForEach(delegate(UIMenu m)
			{
				m.AUDIO_LIBRARY = value;
			});
		}
	}

	public string AUDIO_UPDOWN
	{
		set
		{
			_menuList.ForEach(delegate(UIMenu m)
			{
				m.AUDIO_UPDOWN = value;
			});
		}
	}

	public string AUDIO_SELECT
	{
		set
		{
			_menuList.ForEach(delegate(UIMenu m)
			{
				m.AUDIO_SELECT = value;
			});
		}
	}

	public string AUDIO_BACK
	{
		set
		{
			_menuList.ForEach(delegate(UIMenu m)
			{
				m.AUDIO_BACK = value;
			});
		}
	}

	public string AUDIO_ERROR
	{
		set
		{
			_menuList.ForEach(delegate(UIMenu m)
			{
				m.AUDIO_ERROR = value;
			});
		}
	}

	public int WidthOffset
	{
		set
		{
			_menuList.ForEach(delegate(UIMenu m)
			{
				m.SetMenuWidthOffset(value);
			});
		}
	}

	public string CounterPretext
	{
		set
		{
			_menuList.ForEach(delegate(UIMenu m)
			{
				m.CounterPretext = value;
			});
		}
	}

	public bool DisableInstructionalButtons
	{
		set
		{
			_menuList.ForEach(delegate(UIMenu m)
			{
				m.DisableInstructionalButtons(value);
			});
		}
	}

	public void Add(UIMenu menu)
	{
		_menuList.Add(menu);
	}

	public void AddPauseMenu(TabView menu)
	{
		_pauseMenus.Add(menu);
	}

	public UIMenu AddSubMenu(UIMenu menu, string text)
	{
		PointF offset = PointF.Empty;
		if (OffsetInheritance)
		{
			offset = menu.Offset;
		}
		return AddSubMenu(menu, text, "", offset);
	}

	public UIMenu AddSubMenu(UIMenu menu, string text, PointF offset)
	{
		return AddSubMenu(menu, text, "", Point.Empty);
	}

	public UIMenu AddSubMenu(UIMenu menu, string text, string description)
	{
		PointF offset = PointF.Empty;
		if (OffsetInheritance)
		{
			offset = menu.Offset;
		}
		return AddSubMenu(menu, text, description, offset);
	}

	public UIMenu AddSubMenu(UIMenu menu, string text, string description, PointF offset)
	{
		UIMenuItem uIMenuItem = new UIMenuItem(text, description);
		menu.AddItem(uIMenuItem);
		UIMenu uIMenu = new UIMenu(((Text)menu.Title).Caption, text, offset);
		if (BannerInheritance && menu.BannerTexture != null)
		{
			uIMenu.SetBannerType(menu.BannerTexture);
		}
		else if (BannerInheritance && menu.BannerRectangle != null)
		{
			uIMenu.SetBannerType(menu.BannerRectangle);
		}
		else if (BannerInheritance && menu.BannerSprite != null)
		{
			uIMenu.SetBannerType(menu.BannerSprite);
		}
		Add(uIMenu);
		menu.BindMenuToItem(uIMenu, uIMenuItem);
		return uIMenu;
	}

	public void RefreshIndex()
	{
		foreach (UIMenu menu in _menuList)
		{
			menu.RefreshIndex();
		}
	}

	public List<UIMenu> ToList()
	{
		return _menuList;
	}

	public void ProcessControl()
	{
		int count = _menuList.Count;
		for (int i = 0; i < count; i++)
		{
			if (_menuList[i].Visible)
			{
				_menuList[i].ProcessControl();
			}
		}
	}

	public void ProcessKey(Keys key)
	{
		int count = _menuList.Count;
		for (int i = 0; i < count; i++)
		{
			if (_menuList[i].Visible)
			{
				_menuList[i].ProcessKey(key);
			}
		}
	}

	public void ProcessMouse()
	{
		int count = _menuList.Count;
		for (int i = 0; i < count; i++)
		{
			if (_menuList[i].Visible)
			{
				_menuList[i].ProcessMouse();
			}
		}
	}

	public void Draw()
	{
		int count = _menuList.Count;
		for (int i = 0; i < count; i++)
		{
			if (_menuList[i].Visible)
			{
				_menuList[i].Draw();
			}
		}
	}

	public bool IsAnyMenuOpen()
	{
		if (!_menuList.Any((UIMenu menu) => menu.Visible))
		{
			return _pauseMenus.Any((TabView x) => x.Visible);
		}
		return true;
	}

	public void ProcessMenus()
	{
		ProcessControl();
		if (_pauseMenus.Count > 0)
		{
			_pauseMenus.ForEach(delegate(TabView x)
			{
				x.ProcessControls();
				x.Draw();
			});
		}
		ProcessMouse();
		Draw();
	}

	public void CloseAllMenus()
	{
		foreach (UIMenu item in _menuList.Where((UIMenu menu) => menu.Visible))
		{
			item.Visible = false;
		}
	}

	public void SetBannerType(Sprite bannerType)
	{
		_menuList.ForEach(delegate(UIMenu m)
		{
			m.SetBannerType(bannerType);
		});
	}

	public void SetBannerType(UIResRectangle bannerType)
	{
		_menuList.ForEach(delegate(UIMenu m)
		{
			m.SetBannerType(bannerType);
		});
	}

	public void SetBannerType(string bannerPath)
	{
		_menuList.ForEach(delegate(UIMenu m)
		{
			m.SetBannerType(bannerPath);
		});
	}

	public void SetKey(UIMenu.MenuControls menuControl, Control control)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		_menuList.ForEach(delegate(UIMenu m)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			m.SetKey(menuControl, control);
		});
	}

	public void SetKey(UIMenu.MenuControls menuControl, Control control, int controllerIndex)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		_menuList.ForEach(delegate(UIMenu m)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			m.SetKey(menuControl, control, controllerIndex);
		});
	}

	public void SetKey(UIMenu.MenuControls menuControl, Keys control)
	{
		_menuList.ForEach(delegate(UIMenu m)
		{
			m.SetKey(menuControl, control);
		});
	}

	public void ResetKey(UIMenu.MenuControls menuControl)
	{
		_menuList.ForEach(delegate(UIMenu m)
		{
			m.ResetKey(menuControl);
		});
	}
}
