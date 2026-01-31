using System;
using System.Collections.Generic;
using System.Drawing;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

namespace NativeUI.PauseMenu;

public class TabView
{
	public bool DisplayHeader = true;

	protected readonly SizeF Resolution = ScreenTools.ResolutionMaintainRatio;

	internal bool _loaded;

	internal static readonly string _browseTextLocalized = Game.GetGXTEntry("HUD_INPUT1C");

	public int Index;

	private bool _visible;

	private Scaleform _sc;

	private Scaleform _header;

	public string Title { get; set; }

	public string SubTitle { get; set; }

	public string SideStringTop { get; set; }

	public string SideStringMiddle { get; set; }

	public string SideStringBottom { get; set; }

	public Tuple<string, string> HeaderPicture { internal get; set; }

	public Sprite Photo { get; set; }

	public string Name { get; set; }

	public string Money { get; set; }

	public string MoneySubtitle { get; set; }

	public List<TabItem> Tabs { get; set; }

	public int FocusLevel { get; set; }

	public bool TemporarilyHidden { get; set; }

	public bool CanLeave { get; set; }

	public bool HideTabs { get; set; }

	public bool Visible
	{
		get
		{
			return _visible;
		}
		set
		{
			if (value)
			{
				API.SetPauseMenuActive(true);
				Effects.Start((ScreenEffect)3, 800, false);
				API.TransitionToBlurred(700f);
			}
			else
			{
				API.SetPauseMenuActive(false);
				Effects.Start((ScreenEffect)3, 500, false);
				API.TransitionFromBlurred(400f);
			}
			_visible = value;
		}
	}

	public event EventHandler OnMenuClose;

	public TabView(string title)
	{
		Title = title;
		SubTitle = "";
		SideStringTop = "";
		SideStringMiddle = "";
		SideStringBottom = "";
		Tabs = new List<TabItem>();
		Index = 0;
		Name = Game.Player.Name;
		TemporarilyHidden = false;
		CanLeave = true;
	}

	public TabView(string title, string subtitle)
	{
		Title = title;
		SubTitle = subtitle;
		SideStringTop = "";
		SideStringMiddle = "";
		SideStringBottom = "";
		Tabs = new List<TabItem>();
		Index = 0;
		Name = Game.Player.Name;
		TemporarilyHidden = false;
		CanLeave = true;
	}

	public void AddTab(TabItem item)
	{
		Tabs.Add(item);
		item.Parent = this;
	}

	public void ShowInstructionalButtons()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		if (_sc == null)
		{
			_sc = new Scaleform("instructional_buttons");
		}
		_sc.CallFunction("CLEAR_ALL", new object[0]);
		_sc.CallFunction("TOGGLE_MOUSE_BUTTONS", new object[1] { 0 });
		_sc.CallFunction("CREATE_CONTAINER", new object[0]);
		_sc.CallFunction("SET_DATA_SLOT", new object[3]
		{
			0,
			API.GetControlInstructionalButton(2, 176, 0),
			UIMenu._selectTextLocalized
		});
		_sc.CallFunction("SET_DATA_SLOT", new object[3]
		{
			1,
			API.GetControlInstructionalButton(2, 177, 0),
			UIMenu._backTextLocalized
		});
		_sc.CallFunction("SET_DATA_SLOT", new object[3]
		{
			2,
			API.GetControlInstructionalButton(2, 206, 0),
			""
		});
		_sc.CallFunction("SET_DATA_SLOT", new object[3]
		{
			3,
			API.GetControlInstructionalButton(2, 205, 0),
			_browseTextLocalized
		});
	}

	public async void ShowHeader()
	{
		if (_header == null)
		{
			_header = new Scaleform("pause_menu_header");
		}
		while (!_header.IsLoaded)
		{
			await BaseScript.Delay(0);
		}
		if (string.IsNullOrEmpty(SubTitle) || string.IsNullOrWhiteSpace(SubTitle))
		{
			_header.CallFunction("SET_HEADER_TITLE", new object[1] { Title });
		}
		else
		{
			_header.CallFunction("SET_HEADER_TITLE", new object[3] { Title, false, SubTitle });
			_header.CallFunction("SHIFT_CORONA_DESC", new object[1] { true });
		}
		if (HeaderPicture != null)
		{
			_header.CallFunction("SET_CHAR_IMG", new object[3] { HeaderPicture.Item1, HeaderPicture.Item2, true });
		}
		else
		{
			int mugshot = API.RegisterPedheadshot(API.PlayerPedId());
			while (!API.IsPedheadshotReady(mugshot))
			{
				await BaseScript.Delay(1);
			}
			string pedheadshotTxdString = API.GetPedheadshotTxdString(mugshot);
			HeaderPicture = new Tuple<string, string>(pedheadshotTxdString, pedheadshotTxdString);
			API.ReleasePedheadshotImgUpload(mugshot);
			_header.CallFunction("SET_CHAR_IMG", new object[3] { HeaderPicture.Item1, HeaderPicture.Item2, true });
		}
		_header.CallFunction("SET_HEADING_DETAILS", new object[4] { SideStringTop, SideStringMiddle, SideStringBottom, false });
		_header.CallFunction("BUILD_MENU", new object[0]);
		_header.CallFunction("adjustHeaderPositions", new object[0]);
		_header.CallFunction("SHOW_HEADING_DETAILS", new object[1] { true });
		_header.CallFunction("SHOW_MENU", new object[1] { true });
		_loaded = true;
	}

	public void DrawInstructionalButton(int slot, Control control, string text)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected I4, but got Unknown
		_sc.CallFunction("SET_DATA_SLOT", new object[3]
		{
			slot,
			API.GetControlInstructionalButton(2, (int)control, 0),
			text
		});
	}

	public void ProcessControls()
	{
		if (!Visible || TemporarilyHidden)
		{
			return;
		}
		API.DisableAllControlActions(0);
		if (Game.IsControlJustPressed(2, (Control)174) && FocusLevel == 0)
		{
			Tabs[Index].Active = false;
			Tabs[Index].Focused = false;
			Tabs[Index].Visible = false;
			Index = (1000 - 1000 % Tabs.Count + Index - 1) % Tabs.Count;
			Tabs[Index].Active = true;
			Tabs[Index].Focused = false;
			Tabs[Index].Visible = true;
			Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
		}
		else if (Game.IsControlJustPressed(2, (Control)175) && FocusLevel == 0)
		{
			Tabs[Index].Active = false;
			Tabs[Index].Focused = false;
			Tabs[Index].Visible = false;
			Index = (1000 - 1000 % Tabs.Count + Index + 1) % Tabs.Count;
			Tabs[Index].Active = true;
			Tabs[Index].Focused = false;
			Tabs[Index].Visible = true;
			Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
		}
		else if (Game.IsControlJustPressed(2, (Control)201) && FocusLevel == 0)
		{
			if (Tabs[Index].CanBeFocused)
			{
				Tabs[Index].Focused = true;
				Tabs[Index].JustOpened = true;
				FocusLevel = 1;
			}
			else
			{
				Tabs[Index].JustOpened = true;
				Tabs[Index].OnActivated();
			}
			Game.PlaySound("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
		}
		else if (Game.IsControlJustPressed(2, (Control)177))
		{
			if (FocusLevel == 1)
			{
				Tabs[Index].Focused = false;
				FocusLevel = 0;
				Game.PlaySound("BACK", "HUD_FRONTEND_DEFAULT_SOUNDSET");
			}
			else if (FocusLevel == 0 && CanLeave)
			{
				Visible = false;
				Game.PlaySound("BACK", "HUD_FRONTEND_DEFAULT_SOUNDSET");
				this.OnMenuClose?.Invoke(this, EventArgs.Empty);
				_loaded = false;
				_header.CallFunction("REMOVE_MENU", new object[1] { true });
				_header.Dispose();
				_header = null;
			}
		}
		if (!HideTabs)
		{
			if (Game.IsControlJustPressed(0, (Control)205))
			{
				Tabs[Index].Active = false;
				Tabs[Index].Focused = false;
				Tabs[Index].Visible = false;
				Index = (1000 - 1000 % Tabs.Count + Index - 1) % Tabs.Count;
				Tabs[Index].Active = true;
				Tabs[Index].Focused = false;
				Tabs[Index].Visible = true;
				FocusLevel = 0;
				Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
			}
			else if (Game.IsControlJustPressed(0, (Control)206))
			{
				Tabs[Index].Active = false;
				Tabs[Index].Focused = false;
				Tabs[Index].Visible = false;
				Index = (1000 - 1000 % Tabs.Count + Index + 1) % Tabs.Count;
				Tabs[Index].Active = true;
				Tabs[Index].Focused = false;
				Tabs[Index].Visible = true;
				FocusLevel = 0;
				Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
			}
		}
		if (Tabs.Count > 0)
		{
			Tabs[Index].ProcessControls();
		}
	}

	public void RefreshIndex()
	{
		foreach (TabItem tab in Tabs)
		{
			tab.Focused = false;
			tab.Active = false;
			tab.Visible = false;
		}
		Index = (1000 - 1000 % Tabs.Count) % Tabs.Count;
		Tabs[Index].Active = true;
		Tabs[Index].Focused = false;
		Tabs[Index].Visible = true;
		FocusLevel = 0;
	}

	public void Draw()
	{
		if (!Visible || TemporarilyHidden)
		{
			return;
		}
		ShowInstructionalButtons();
		API.HideHudAndRadarThisFrame();
		API.ShowCursorThisFrame();
		PointF left = new PointF(300f, (SubTitle != null && SubTitle != "") ? 205 : 195);
		if (!HideTabs)
		{
			for (int i = 0; i < Tabs.Count; i++)
			{
				float num = (float)((int)(Resolution.Width - 2f * left.X - 5f) / Tabs.Count) - 1.95f;
				Game.EnableControlThisFrame(0, (Control)239);
				Game.EnableControlThisFrame(0, (Control)240);
				bool flag = ScreenTools.IsMouseInBounds(left.AddPoints(new PointF((num + 5f) * (float)i, 0f)), new SizeF(num, 40f));
				Color baseColor = (Tabs[i].Active ? Colors.White : (flag ? Color.FromArgb(100, 50, 50, 50) : Colors.Black));
				((Rectangle)new UIResRectangle(left.AddPoints(new PointF((num + 5f) * (float)i, 0f)), new SizeF(num, 40f), Color.FromArgb(Tabs[i].Active ? 255 : 200, baseColor))).Draw();
				if (Tabs[i].Active)
				{
					((Rectangle)new UIResRectangle(left.SubtractPoints(new PointF(0f - (num + 5f) * (float)i, 10f)), new SizeF(num, 10f), Colors.DodgerBlue)).Draw();
				}
				((Text)new UIResText(Tabs[i].Title.ToUpper(), left.AddPoints(new PointF(num / 2f + (num + 5f) * (float)i, 5f)), 0.3f, Tabs[i].Active ? Colors.Black : Colors.White, (Font)0, (Alignment)0)).Draw();
				if (flag && Game.IsControlJustPressed(0, (Control)237) && !Tabs[i].Active)
				{
					Tabs[Index].Active = false;
					Tabs[Index].Focused = false;
					Tabs[Index].Visible = false;
					Index = (1000 - 1000 % Tabs.Count + i) % Tabs.Count;
					Tabs[Index].Active = true;
					Tabs[Index].Focused = true;
					Tabs[Index].Visible = true;
					Tabs[Index].JustOpened = true;
					if (Tabs[Index].CanBeFocused)
					{
						FocusLevel = 1;
					}
					else
					{
						FocusLevel = 0;
					}
					Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
				}
			}
		}
		Tabs[Index].Draw();
		_sc.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", new object[1] { -1 });
		_sc.Render2D();
		if (DisplayHeader)
		{
			if (!_loaded)
			{
				ShowHeader();
			}
			API.DrawScaleformMovie(_header.Handle, 0.501f, 0.162f, 0.6782f, 0.145f, 255, 255, 255, 255, 0);
		}
	}
}
