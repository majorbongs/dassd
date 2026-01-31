using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

namespace NativeUI;

public class UIMenu
{
	public enum MenuControls
	{
		Up,
		Down,
		Left,
		Right,
		Select,
		Back
	}

	private readonly Container _mainMenu;

	private readonly Sprite _background;

	private readonly UIResRectangle _descriptionBar;

	private readonly Sprite _descriptionRectangle;

	private readonly UIResText _descriptionText;

	private readonly UIResText _counterText;

	private int _activeItem = 1000;

	private bool _visible;

	private bool _buttonsEnabled = true;

	private bool _justOpened = true;

	private bool _itemsDirty;

	private const int MaxItemsOnScreen = 9;

	private int _minItem;

	private int _maxItem = 9;

	private readonly Dictionary<MenuControls, Tuple<List<Keys>, List<Tuple<Control, int>>>> _keyDictionary = new Dictionary<MenuControls, Tuple<List<Keys>, List<Tuple<Control, int>>>>();

	private readonly List<InstructionalButton> _instructionalButtons = new List<InstructionalButton>();

	private readonly Sprite _upAndDownSprite;

	private readonly UIResRectangle _extraRectangleUp;

	private readonly UIResRectangle _extraRectangleDown;

	private readonly Scaleform _instructionalButtonsScaleform;

	private readonly Scaleform _menuGlare;

	private readonly int _extraYOffset;

	private static readonly MenuControls[] _menuControls = Enum.GetValues(typeof(MenuControls)).Cast<MenuControls>().ToArray();

	private float PanelOffset;

	private bool ReDraw = true;

	private bool Glare;

	internal static readonly string _selectTextLocalized = Game.GetGXTEntry("HUD_INPUT2");

	internal static readonly string _backTextLocalized = Game.GetGXTEntry("HUD_INPUT3");

	protected readonly SizeF Resolution = ScreenTools.ResolutionMaintainRatio;

	protected internal int[] _pressingTimer = new int[4];

	protected internal int[] _pressingTicks = new int[4];

	public string AUDIO_LIBRARY = "HUD_FRONTEND_DEFAULT_SOUNDSET";

	public string AUDIO_UPDOWN = "NAV_UP_DOWN";

	public string AUDIO_LEFTRIGHT = "NAV_LEFT_RIGHT";

	public string AUDIO_SELECT = "SELECT";

	public string AUDIO_BACK = "BACK";

	public string AUDIO_ERROR = "ERROR";

	public List<UIMenuItem> MenuItems = new List<UIMenuItem>();

	public bool MouseEdgeEnabled = true;

	public bool ControlDisablingEnabled = true;

	public bool ResetCursorOnOpen = true;

	[Obsolete("The description is now formated automatically by the game.")]
	public bool FormatDescriptions = true;

	public bool MouseControlsEnabled = true;

	public bool ScaleWithSafezone = true;

	public List<UIMenuHeritageWindow> Windows = new List<UIMenuHeritageWindow>();

	private PointF Safe { get; set; }

	private SizeF BackgroundSize { get; set; }

	private SizeF DrawWidth { get; set; }

	public PointF Offset { get; }

	public Sprite BannerSprite { get; private set; }

	public UIResRectangle BannerRectangle { get; private set; }

	public string BannerTexture { get; private set; }

	public bool CanUserGoBack { get; set; } = true;

	public bool Visible
	{
		get
		{
			return _visible;
		}
		set
		{
			_visible = value;
			_justOpened = value;
			_itemsDirty = value;
			UpdateScaleform();
			if (ParentMenu == null && value && ResetCursorOnOpen)
			{
				API.SetCursorLocation(0.5f, 0.5f);
				Hud.CursorSprite = (CursorSprite)1;
				if (value)
				{
					MenuOpenEv();
				}
				else
				{
					MenuCloseEv();
				}
			}
		}
	}

	public int CurrentSelection
	{
		get
		{
			if (MenuItems.Count != 0)
			{
				return _activeItem % MenuItems.Count;
			}
			return 0;
		}
		set
		{
			if (MenuItems.Count == 0)
			{
				_activeItem = 0;
			}
			MenuItems[_activeItem % MenuItems.Count].Selected = false;
			_activeItem = 1000000 - 1000000 % MenuItems.Count + value;
			if (CurrentSelection > _maxItem)
			{
				_maxItem = CurrentSelection;
				_minItem = CurrentSelection - 9;
			}
			else if (CurrentSelection < _minItem)
			{
				_maxItem = 9 + CurrentSelection;
				_minItem = CurrentSelection;
			}
		}
	}

	public static bool IsUsingController => !API.IsInputDisabled(2);

	public int Size => MenuItems.Count;

	public UIResText Title { get; }

	public UIResText Subtitle { get; }

	public string CounterPretext { get; set; }

	public UIMenu ParentMenu { get; set; }

	public UIMenuItem ParentItem { get; set; }

	public Dictionary<UIMenuItem, UIMenu> Children { get; }

	public int WidthOffset { get; private set; }

	public event IndexChangedEvent OnIndexChange;

	public event ListChangedEvent OnListChange;

	public event ListSelectedEvent OnListSelect;

	public event SliderChangedEvent OnSliderChange;

	public event ProgressSliderChangedEvent OnProgressSliderChange;

	public event OnProgressChanged OnProgressChange;

	public event OnProgressSelected OnProgressSelect;

	public event CheckboxChangeEvent OnCheckboxChange;

	public event ItemSelectEvent OnItemSelect;

	public event MenuOpenEvent OnMenuOpen;

	public event MenuCloseEvent OnMenuClose;

	public event MenuChangeEvent OnMenuChange;

	public UIMenu(string title, string subtitle, bool glare = false)
		: this(title, subtitle, new PointF(0f, 0f), "commonmenu", "interaction_bgd", glare)
	{
	}

	public UIMenu(string title, string subtitle, PointF offset, bool glare = false)
		: this(title, subtitle, offset, "commonmenu", "interaction_bgd", glare)
	{
	}

	public UIMenu(string title, string subtitle, PointF offset, string customBanner)
		: this(title, subtitle, offset, "commonmenu", "interaction_bgd")
	{
		BannerTexture = customBanner;
	}

	public UIMenu(string title, string subtitle, PointF offset, string spriteLibrary, string spriteName, bool glare = false)
	{
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Expected O, but got Unknown
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Expected O, but got Unknown
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Expected O, but got Unknown
		Offset = offset;
		Children = new Dictionary<UIMenuItem, UIMenu>();
		WidthOffset = 0;
		Glare = glare;
		_instructionalButtonsScaleform = new Scaleform("instructional_buttons");
		_menuGlare = new Scaleform("mp_menu_glare");
		UpdateScaleform();
		_mainMenu = new Container(new PointF(0f, 0f), new SizeF(700f, 500f), Color.FromArgb(0, 0, 0, 0));
		BannerSprite = new Sprite(spriteLibrary, spriteName, new PointF(0f + Offset.X, 0f + Offset.Y), new SizeF(431f, 100f));
		_mainMenu.Items.Add((IElement)(object)(Title = new UIResText(title, new PointF(215f + Offset.X, 13f + Offset.Y), 1.15f, Colors.White, (Font)1, (Alignment)0)));
		if (!string.IsNullOrWhiteSpace(subtitle))
		{
			_mainMenu.Items.Add((IElement)(object)new UIResRectangle(new PointF(0f + offset.X, 100f + Offset.Y), new SizeF(431f, 37f), Colors.Black));
			_mainMenu.Items.Add((IElement)(object)(Subtitle = new UIResText(subtitle, new PointF(8f + Offset.X, 103f + Offset.Y), 0.35f, Colors.WhiteSmoke, (Font)0, (Alignment)1)));
			if (subtitle.StartsWith("~"))
			{
				CounterPretext = subtitle.Substring(0, 3);
			}
			_counterText = new UIResText("", new PointF(425f + Offset.X, 103f + Offset.Y), 0.35f, Colors.WhiteSmoke, (Font)0, (Alignment)2);
			_extraYOffset = 30;
		}
		_upAndDownSprite = new Sprite("commonmenu", "shop_arrows_upanddown", new PointF(190f + Offset.X, 517f + Offset.Y - 37f + (float)_extraYOffset), new SizeF(50f, 50f));
		_extraRectangleUp = new UIResRectangle(new PointF(0f + Offset.X, 524f + Offset.Y - 37f + (float)_extraYOffset), new SizeF(431f, 18f), Color.FromArgb(200, 0, 0, 0));
		_extraRectangleDown = new UIResRectangle(new PointF(0f + Offset.X, 542f + Offset.Y - 37f + (float)_extraYOffset), new SizeF(431f, 18f), Color.FromArgb(200, 0, 0, 0));
		_descriptionBar = new UIResRectangle(new PointF(Offset.X, 123f), new SizeF(431f, 4f), Colors.Black);
		_descriptionRectangle = new Sprite("commonmenu", "gradient_bgd", new PointF(Offset.X, 127f), new SizeF(431f, 30f));
		_descriptionText = new UIResText("Description", new PointF(Offset.X + 5f, 125f), 0.35f, Color.FromArgb(255, 255, 255, 255), (Font)0, (Alignment)1);
		_background = new Sprite("commonmenu", "gradient_bgd", new PointF(Offset.X, 144f + Offset.Y - 37f + (float)_extraYOffset), new SizeF(290f, 25f));
		SetKey(MenuControls.Up, (Control)172);
		SetKey(MenuControls.Up, (Control)241);
		SetKey(MenuControls.Down, (Control)173);
		SetKey(MenuControls.Down, (Control)242);
		SetKey(MenuControls.Left, (Control)174);
		SetKey(MenuControls.Right, (Control)175);
		SetKey(MenuControls.Select, (Control)201);
		SetKey(MenuControls.Back, (Control)177);
		SetKey(MenuControls.Back, (Control)199);
	}

	[Obsolete("Use Controls.Toggle instead.", true)]
	public static void DisEnableControls(bool toggle)
	{
		Controls.Toggle(toggle);
	}

	[Obsolete("Use ScreenTools.ResolutionMaintainRatio instead.", true)]
	public static SizeF GetScreenResolutionMaintainRatio()
	{
		return ScreenTools.ResolutionMaintainRatio;
	}

	[Obsolete("Use ScreenTools.ResolutionMaintainRatio instead.", true)]
	public static SizeF GetScreenResiolutionMantainRatio()
	{
		return ScreenTools.ResolutionMaintainRatio;
	}

	[Obsolete("Use ScreenTools.IsMouseInBounds instead.", true)]
	public static bool IsMouseInBounds(Point topLeft, Size boxSize)
	{
		return ScreenTools.IsMouseInBounds(topLeft, boxSize);
	}

	[Obsolete("Use ScreenTools.SafezoneBounds instead.", true)]
	public static Point GetSafezoneBounds()
	{
		return ScreenTools.SafezoneBounds;
	}

	public void SetMenuWidthOffset(int widthOffset)
	{
		WidthOffset = widthOffset;
		BannerSprite.Size = new SizeF(431 + WidthOffset, 100f);
		_mainMenu.Items[0].Position = new PointF(((float)WidthOffset + Offset.X + 431f) / 2f, 20f + Offset.Y);
		((Text)_counterText).Position = new PointF(425f + Offset.X + (float)widthOffset, 110f + Offset.Y);
		if (_mainMenu.Items.Count >= 1)
		{
			((Rectangle)(UIResRectangle)(object)_mainMenu.Items[1]).Size = new SizeF(431 + WidthOffset, 37f);
		}
		if (BannerRectangle != null)
		{
			((Rectangle)BannerRectangle).Size = new SizeF(431 + WidthOffset, 100f);
		}
	}

	public void DisableInstructionalButtons(bool disable)
	{
		_buttonsEnabled = !disable;
	}

	public void SetBannerType(Sprite spriteBanner)
	{
		BannerSprite = spriteBanner;
		BannerSprite.Size = new SizeF(431 + WidthOffset, 100f);
		BannerSprite.Position = new PointF(Offset.X, Offset.Y);
	}

	public void SetBannerType(UIResRectangle rectangle)
	{
		BannerSprite = null;
		BannerRectangle = rectangle;
		((Rectangle)BannerRectangle).Position = new PointF(Offset.X, Offset.Y);
		((Rectangle)BannerRectangle).Size = new SizeF(431 + WidthOffset, 100f);
	}

	public void SetBannerType(string pathToCustomSprite)
	{
		BannerTexture = pathToCustomSprite;
	}

	public void AddItem(UIMenuItem item)
	{
		int currentSelection = CurrentSelection;
		item.Offset = Offset;
		item.Parent = this;
		item.Position(MenuItems.Count * 25 - 37 + _extraYOffset);
		MenuItems.Add(item);
		ReDraw = true;
		CurrentSelection = currentSelection;
	}

	public void AddWindow(UIMenuHeritageWindow window)
	{
		window.ParentMenu = this;
		window.Offset = Offset;
		Windows.Add(window);
		ReDraw = true;
	}

	public void RemoveWindowAt(int index)
	{
		Windows.RemoveAt(index);
		ReDraw = true;
	}

	public void UpdateDescription()
	{
		ReDraw = true;
	}

	public void RemoveItemAt(int index)
	{
		int currentSelection = CurrentSelection;
		if (Size > 9 && _maxItem == Size - 1)
		{
			_maxItem--;
			_minItem--;
		}
		MenuItems.RemoveAt(index);
		ReDraw = true;
		CurrentSelection = currentSelection;
	}

	public void RefreshIndex()
	{
		if (MenuItems.Count == 0)
		{
			_activeItem = 1000;
			_maxItem = 9;
			_minItem = 0;
		}
		else
		{
			MenuItems[_activeItem % MenuItems.Count].Selected = false;
			_activeItem = 1000 - 1000 % MenuItems.Count;
			_maxItem = 9;
			_minItem = 0;
			ReDraw = true;
		}
	}

	public void Clear()
	{
		MenuItems.Clear();
		ReDraw = true;
	}

	public void Remove(Func<UIMenuItem, bool> predicate)
	{
		foreach (UIMenuItem item in new List<UIMenuItem>(MenuItems))
		{
			if (predicate(item))
			{
				MenuItems.Remove(item);
			}
		}
		ReDraw = true;
	}

	private float CalculateWindowHeight()
	{
		float num = 0f;
		if (Windows.Count > 0)
		{
			for (int i = 0; i < Windows.Count; i++)
			{
				num += Windows[i].Background.Size.Height;
			}
		}
		return num;
	}

	private float CalculateItemHeight()
	{
		float num = 0f + _mainMenu.Items[1].Position.Y - 37f;
		for (int i = 0; i < MenuItems.Count; i++)
		{
			num += ((Rectangle)MenuItems[i]._rectangle).Size.Height;
		}
		return num;
	}

	private float CalculatePanelsPosition(bool hasDescription)
	{
		float num = CalculateWindowHeight() + 40f + ((Rectangle)_mainMenu).Position.Y;
		num += _descriptionRectangle.Size.Height + 5f;
		if (hasDescription)
		{
			num += _descriptionRectangle.Size.Height + 5f;
		}
		return CalculateItemHeight() + num;
	}

	private void DrawCalculations()
	{
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		float num = CalculateWindowHeight();
		DrawWidth = new SizeF(431 + WidthOffset, 100f);
		Safe = ScreenTools.SafezoneBounds;
		BackgroundSize = ((Size > 10) ? new SizeF(431 + WidthOffset, 38f * (10f + num)) : new SizeF(431 + WidthOffset, (float)(38 * Size) + num));
		((Rectangle)_extraRectangleUp).Size = new SizeF(431 + WidthOffset, 18f + num);
		((Rectangle)_extraRectangleDown).Size = new SizeF(431 + WidthOffset, 18f + num);
		_upAndDownSprite.Position = new PointF(190f + Offset.X + (float)((WidthOffset > 0) ? (WidthOffset / 2) : WidthOffset), 517f + Offset.Y - 37f + (float)_extraYOffset + num);
		ReDraw = false;
		if (MenuItems.Count != 0 && !string.IsNullOrWhiteSpace(MenuItems[_activeItem % MenuItems.Count].Description))
		{
			RecalculateDescriptionPosition();
			string description = MenuItems[_activeItem % MenuItems.Count].Description;
			((Text)_descriptionText).Caption = description;
			_descriptionText.Wrap = 400f;
			int lineCount = ScreenTools.GetLineCount(description, ((Text)_descriptionText).Position, ((Text)_descriptionText).Font, ((Text)_descriptionText).Scale, ((Text)_descriptionText).Position.X + 400f);
			_descriptionRectangle.Size = new SizeF(431 + WidthOffset, lineCount * 25 + 15);
		}
	}

	public void GoUpOverflow()
	{
		if (Size <= 10)
		{
			return;
		}
		if (_activeItem % MenuItems.Count <= _minItem)
		{
			if (_activeItem % MenuItems.Count == 0)
			{
				_minItem = MenuItems.Count - 9 - 1;
				_maxItem = MenuItems.Count - 1;
				MenuItems[_activeItem % MenuItems.Count].Selected = false;
				_activeItem = 1000 - 1000 % MenuItems.Count;
				_activeItem += MenuItems.Count - 1;
				MenuItems[_activeItem % MenuItems.Count].Selected = true;
			}
			else
			{
				_minItem--;
				_maxItem--;
				MenuItems[_activeItem % MenuItems.Count].Selected = false;
				_activeItem--;
				MenuItems[_activeItem % MenuItems.Count].Selected = true;
			}
		}
		else
		{
			MenuItems[_activeItem % MenuItems.Count].Selected = false;
			_activeItem--;
			MenuItems[_activeItem % MenuItems.Count].Selected = true;
		}
		Game.PlaySound(AUDIO_UPDOWN, AUDIO_LIBRARY);
		IndexChange(CurrentSelection);
	}

	public void GoUp()
	{
		if (Size <= 10)
		{
			MenuItems[_activeItem % MenuItems.Count].Selected = false;
			_activeItem--;
			MenuItems[_activeItem % MenuItems.Count].Selected = true;
			Game.PlaySound(AUDIO_UPDOWN, AUDIO_LIBRARY);
			IndexChange(CurrentSelection);
		}
	}

	public void GoDownOverflow()
	{
		if (Size <= 10)
		{
			return;
		}
		if (_activeItem % MenuItems.Count >= _maxItem)
		{
			if (_activeItem % MenuItems.Count == MenuItems.Count - 1)
			{
				_minItem = 0;
				_maxItem = 9;
				MenuItems[_activeItem % MenuItems.Count].Selected = false;
				_activeItem = 1000 - 1000 % MenuItems.Count;
				MenuItems[_activeItem % MenuItems.Count].Selected = true;
			}
			else
			{
				_minItem++;
				_maxItem++;
				MenuItems[_activeItem % MenuItems.Count].Selected = false;
				_activeItem++;
				MenuItems[_activeItem % MenuItems.Count].Selected = true;
			}
		}
		else
		{
			MenuItems[_activeItem % MenuItems.Count].Selected = false;
			_activeItem++;
			MenuItems[_activeItem % MenuItems.Count].Selected = true;
		}
		Game.PlaySound(AUDIO_UPDOWN, AUDIO_LIBRARY);
		IndexChange(CurrentSelection);
	}

	public void GoDown()
	{
		if (Size <= 10)
		{
			MenuItems[_activeItem % MenuItems.Count].Selected = false;
			_activeItem++;
			MenuItems[_activeItem % MenuItems.Count].Selected = true;
			Game.PlaySound(AUDIO_UPDOWN, AUDIO_LIBRARY);
			IndexChange(CurrentSelection);
		}
	}

	public void GoLeft()
	{
		if (MenuItems[CurrentSelection].Enabled && (MenuItems[CurrentSelection] is UIMenuListItem || MenuItems[CurrentSelection] is UIMenuSliderItem || MenuItems[CurrentSelection] is UIMenuDynamicListItem || MenuItems[CurrentSelection] is UIMenuSliderProgressItem || MenuItems[CurrentSelection] is UIMenuProgressItem))
		{
			if (MenuItems[CurrentSelection] is UIMenuListItem)
			{
				UIMenuListItem uIMenuListItem = (UIMenuListItem)MenuItems[CurrentSelection];
				uIMenuListItem.Index--;
				Game.PlaySound(AUDIO_LEFTRIGHT, AUDIO_LIBRARY);
				ListChange(uIMenuListItem, uIMenuListItem.Index);
				uIMenuListItem.ListChangedTrigger(uIMenuListItem.Index);
			}
			else if (MenuItems[CurrentSelection] is UIMenuDynamicListItem)
			{
				UIMenuDynamicListItem uIMenuDynamicListItem = (UIMenuDynamicListItem)MenuItems[CurrentSelection];
				string currentListItem = uIMenuDynamicListItem.Callback(uIMenuDynamicListItem, UIMenuDynamicListItem.ChangeDirection.Left);
				uIMenuDynamicListItem.CurrentListItem = currentListItem;
				Game.PlaySound(AUDIO_LEFTRIGHT, AUDIO_LIBRARY);
			}
			else if (MenuItems[CurrentSelection] is UIMenuSliderItem)
			{
				UIMenuSliderItem uIMenuSliderItem = (UIMenuSliderItem)MenuItems[CurrentSelection];
				uIMenuSliderItem.Value -= uIMenuSliderItem.Multiplier;
				Game.PlaySound(AUDIO_LEFTRIGHT, AUDIO_LIBRARY);
				SliderChange(uIMenuSliderItem, uIMenuSliderItem.Value);
			}
			else if (MenuItems[CurrentSelection] is UIMenuSliderProgressItem)
			{
				UIMenuSliderProgressItem uIMenuSliderProgressItem = (UIMenuSliderProgressItem)MenuItems[CurrentSelection];
				uIMenuSliderProgressItem.Value--;
				Game.PlaySound(AUDIO_LEFTRIGHT, AUDIO_LIBRARY);
				SliderProgressChange(uIMenuSliderProgressItem, uIMenuSliderProgressItem.Value);
			}
			else if (MenuItems[CurrentSelection] is UIMenuProgressItem)
			{
				UIMenuProgressItem uIMenuProgressItem = (UIMenuProgressItem)MenuItems[CurrentSelection];
				uIMenuProgressItem.Index--;
				Game.PlaySound(AUDIO_LEFTRIGHT, AUDIO_LIBRARY);
				ProgressChange(uIMenuProgressItem, uIMenuProgressItem.Index);
			}
		}
	}

	public void GoRight()
	{
		if (MenuItems[CurrentSelection].Enabled && (MenuItems[CurrentSelection] is UIMenuListItem || MenuItems[CurrentSelection] is UIMenuSliderItem || MenuItems[CurrentSelection] is UIMenuDynamicListItem || MenuItems[CurrentSelection] is UIMenuSliderProgressItem || MenuItems[CurrentSelection] is UIMenuProgressItem))
		{
			if (MenuItems[CurrentSelection] is UIMenuListItem)
			{
				UIMenuListItem uIMenuListItem = (UIMenuListItem)MenuItems[CurrentSelection];
				uIMenuListItem.Index++;
				Game.PlaySound(AUDIO_LEFTRIGHT, AUDIO_LIBRARY);
				ListChange(uIMenuListItem, uIMenuListItem.Index);
				uIMenuListItem.ListChangedTrigger(uIMenuListItem.Index);
			}
			else if (MenuItems[CurrentSelection] is UIMenuDynamicListItem)
			{
				UIMenuDynamicListItem uIMenuDynamicListItem = (UIMenuDynamicListItem)MenuItems[CurrentSelection];
				string currentListItem = uIMenuDynamicListItem.Callback(uIMenuDynamicListItem, UIMenuDynamicListItem.ChangeDirection.Right);
				uIMenuDynamicListItem.CurrentListItem = currentListItem;
				Game.PlaySound(AUDIO_LEFTRIGHT, AUDIO_LIBRARY);
			}
			else if (MenuItems[CurrentSelection] is UIMenuSliderItem)
			{
				UIMenuSliderItem uIMenuSliderItem = (UIMenuSliderItem)MenuItems[CurrentSelection];
				uIMenuSliderItem.Value += uIMenuSliderItem.Multiplier;
				Game.PlaySound(AUDIO_LEFTRIGHT, AUDIO_LIBRARY);
				SliderChange(uIMenuSliderItem, uIMenuSliderItem.Value);
			}
			else if (MenuItems[CurrentSelection] is UIMenuSliderProgressItem)
			{
				UIMenuSliderProgressItem uIMenuSliderProgressItem = (UIMenuSliderProgressItem)MenuItems[CurrentSelection];
				uIMenuSliderProgressItem.Value++;
				Game.PlaySound(AUDIO_LEFTRIGHT, AUDIO_LIBRARY);
				SliderProgressChange(uIMenuSliderProgressItem, uIMenuSliderProgressItem.Value);
			}
			else if (MenuItems[CurrentSelection] is UIMenuProgressItem)
			{
				UIMenuProgressItem uIMenuProgressItem = (UIMenuProgressItem)MenuItems[CurrentSelection];
				uIMenuProgressItem.Index++;
				Game.PlaySound(AUDIO_LEFTRIGHT, AUDIO_LIBRARY);
				ProgressChange(uIMenuProgressItem, uIMenuProgressItem.Index);
			}
		}
	}

	public void SelectItem()
	{
		if (!MenuItems[CurrentSelection].Enabled)
		{
			Game.PlaySound(AUDIO_ERROR, AUDIO_LIBRARY);
			return;
		}
		if (MenuItems[CurrentSelection] is UIMenuCheckboxItem)
		{
			UIMenuCheckboxItem uIMenuCheckboxItem = (UIMenuCheckboxItem)MenuItems[CurrentSelection];
			uIMenuCheckboxItem.Checked = !uIMenuCheckboxItem.Checked;
			Game.PlaySound(AUDIO_SELECT, AUDIO_LIBRARY);
			CheckboxChange(uIMenuCheckboxItem, uIMenuCheckboxItem.Checked);
			uIMenuCheckboxItem.CheckboxEventTrigger();
			return;
		}
		if (MenuItems[CurrentSelection] is UIMenuListItem)
		{
			UIMenuListItem uIMenuListItem = (UIMenuListItem)MenuItems[CurrentSelection];
			Game.PlaySound(AUDIO_SELECT, AUDIO_LIBRARY);
			ListSelect(uIMenuListItem, uIMenuListItem.Index);
			uIMenuListItem.ListSelectedTrigger(uIMenuListItem.Index);
			return;
		}
		Game.PlaySound(AUDIO_SELECT, AUDIO_LIBRARY);
		ItemSelect(MenuItems[CurrentSelection], CurrentSelection);
		MenuItems[CurrentSelection].ItemActivate(this);
		if (Children.ContainsKey(MenuItems[CurrentSelection]))
		{
			Visible = false;
			Children[MenuItems[CurrentSelection]].Visible = true;
			MenuChangeEv(Children[MenuItems[CurrentSelection]], forward: true);
		}
	}

	public void GoBack()
	{
		Game.PlaySound(AUDIO_BACK, AUDIO_LIBRARY);
		Visible = false;
		if (ParentMenu != null)
		{
			PointF pointF = new PointF(0.5f, 0.5f);
			ParentMenu.Visible = true;
			MenuChangeEv(ParentMenu, forward: false);
			if (ResetCursorOnOpen)
			{
				API.SetCursorLocation(pointF.X, pointF.Y);
			}
		}
	}

	public void BindMenuToItem(UIMenu menuToBind, UIMenuItem itemToBindTo)
	{
		if (!MenuItems.Contains(itemToBindTo))
		{
			AddItem(itemToBindTo);
		}
		menuToBind.ParentMenu = this;
		menuToBind.ParentItem = itemToBindTo;
		if (Children.ContainsKey(itemToBindTo))
		{
			Children[itemToBindTo] = menuToBind;
		}
		else
		{
			Children.Add(itemToBindTo, menuToBind);
		}
	}

	public bool ReleaseMenuFromItem(UIMenuItem releaseFrom)
	{
		if (!Children.ContainsKey(releaseFrom))
		{
			return false;
		}
		Children[releaseFrom].ParentItem = null;
		Children[releaseFrom].ParentMenu = null;
		Children.Remove(releaseFrom);
		return true;
	}

	public void SetKey(MenuControls control, Keys keyToSet)
	{
		if (_keyDictionary.ContainsKey(control))
		{
			_keyDictionary[control].Item1.Add(keyToSet);
			return;
		}
		_keyDictionary.Add(control, new Tuple<List<Keys>, List<Tuple<Control, int>>>(new List<Keys>(), new List<Tuple<Control, int>>()));
		_keyDictionary[control].Item1.Add(keyToSet);
	}

	public void SetKey(MenuControls control, Control gtaControl)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		SetKey(control, gtaControl, 0);
		SetKey(control, gtaControl, 1);
		SetKey(control, gtaControl, 2);
	}

	public void SetKey(MenuControls control, Control gtaControl, int controlIndex)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (_keyDictionary.ContainsKey(control))
		{
			_keyDictionary[control].Item2.Add(new Tuple<Control, int>(gtaControl, controlIndex));
			return;
		}
		_keyDictionary.Add(control, new Tuple<List<Keys>, List<Tuple<Control, int>>>(new List<Keys>(), new List<Tuple<Control, int>>()));
		_keyDictionary[control].Item2.Add(new Tuple<Control, int>(gtaControl, controlIndex));
	}

	public void ResetKey(MenuControls control)
	{
		_keyDictionary[control].Item1.Clear();
		_keyDictionary[control].Item2.Clear();
	}

	public bool HasControlJustBeenPressed(MenuControls control, Keys key = Keys.None)
	{
		new List<Keys>(_keyDictionary[control].Item1);
		if (new List<Tuple<Control, int>>(_keyDictionary[control].Item2).Any((Tuple<Control, int> tuple) => Game.IsControlJustPressed(tuple.Item2, tuple.Item1)))
		{
			return true;
		}
		return false;
	}

	public bool HasControlJustBeenReleaseed(MenuControls control, Keys key = Keys.None)
	{
		new List<Keys>(_keyDictionary[control].Item1);
		if (new List<Tuple<Control, int>>(_keyDictionary[control].Item2).Any((Tuple<Control, int> tuple) => Game.IsControlJustReleased(tuple.Item2, tuple.Item1)))
		{
			return true;
		}
		return false;
	}

	public bool IsControlBeingPressed(MenuControls control, Keys key = Keys.None)
	{
		new List<Keys>(_keyDictionary[control].Item1);
		if (new List<Tuple<Control, int>>(_keyDictionary[control].Item2).Any((Tuple<Control, int> tuple) => Game.IsControlPressed(tuple.Item2, tuple.Item1)))
		{
			return true;
		}
		return false;
	}

	public void AddInstructionalButton(InstructionalButton button)
	{
		_instructionalButtons.Add(button);
	}

	public void RemoveInstructionalButton(InstructionalButton button)
	{
		_instructionalButtons.Remove(button);
	}

	private void RecalculateDescriptionPosition()
	{
		float num = CalculateWindowHeight();
		((Rectangle)_descriptionBar).Position = new PointF(Offset.X, (float)(112 + _extraYOffset) + Offset.Y + num);
		_descriptionRectangle.Position = new PointF(Offset.X, (float)(112 + _extraYOffset) + Offset.Y + num);
		((Text)_descriptionText).Position = new PointF(Offset.X + 8f, (float)(118 + _extraYOffset) + Offset.Y + num);
		((Rectangle)_descriptionBar).Size = new SizeF(431 + WidthOffset, 4f);
		_descriptionRectangle.Size = new SizeF(431 + WidthOffset, 30f);
		int num2 = Size;
		if (num2 > 10)
		{
			num2 = 11;
		}
		((Rectangle)_descriptionBar).Position = new PointF(Offset.X, (float)(38 * num2) + ((Rectangle)_descriptionBar).Position.Y);
		_descriptionRectangle.Position = new PointF(Offset.X, (float)(38 * num2) + _descriptionRectangle.Position.Y);
		((Text)_descriptionText).Position = new PointF(Offset.X + 8f, (float)(38 * num2) + ((Text)_descriptionText).Position.Y);
	}

	private int IsMouseInListItemArrows(UIMenuItem item, PointF topLeft, PointF safezone)
	{
		API.BeginTextCommandWidth("jamyfafi");
		UIResText.AddLongString(item.Text);
		float width = Resolution.Width;
		float height = Resolution.Height;
		float num = width / height;
		float num2 = 1080f * num;
		int num3 = (int)(API.EndTextCommandGetWidth(false) * num2 * 0.35f);
		int num4 = 5 + num3 + 10;
		int num5 = 431 - num4;
		if (!ScreenTools.IsMouseInBounds(topLeft, new SizeF(num4, 38f)))
		{
			if (!ScreenTools.IsMouseInBounds(new PointF(topLeft.X + (float)num4, topLeft.Y), new SizeF(num5, 38f)))
			{
				return 0;
			}
			return 2;
		}
		return 1;
	}

	public async Task Draw()
	{
		if (!Visible)
		{
			return;
		}
		if (ControlDisablingEnabled)
		{
			Controls.Toggle(toggle: false);
		}
		if (_buttonsEnabled)
		{
			API.DrawScaleformMovieFullscreen(_instructionalButtonsScaleform.Handle, 255, 255, 255, 255, 0);
			Hud.HideComponentThisFrame((HudComponent)6);
			Hud.HideComponentThisFrame((HudComponent)7);
			Hud.HideComponentThisFrame((HudComponent)9);
		}
		if (ScaleWithSafezone)
		{
			API.SetScriptGfxAlign(76, 84);
			API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
		}
		if (ReDraw)
		{
			DrawCalculations();
		}
		if (string.IsNullOrWhiteSpace(BannerTexture))
		{
			if (BannerSprite != null)
			{
				BannerSprite.Draw();
			}
			else
			{
				UIResRectangle bannerRectangle = BannerRectangle;
				if (bannerRectangle != null)
				{
					((Rectangle)bannerRectangle).Draw();
				}
			}
		}
		else if (!ScaleWithSafezone)
		{
			new PointF(0f, 0f);
		}
		else
		{
			_ = Safe;
		}
		((Rectangle)_mainMenu).Draw();
		if (Glare)
		{
			_menuGlare.CallFunction("SET_DATA_SLOT", new object[1] { GameplayCamera.RelativeHeading });
			SizeF sizeF = new SizeF(1f, 1.054f);
			PointF pointF = new PointF(Offset.X / Resolution.Width + 0.4491f, Offset.Y / Resolution.Height + 0.475f);
			API.DrawScaleformMovie(_menuGlare.Handle, pointF.X, pointF.Y, sizeF.Width, sizeF.Height, 255, 255, 255, 255, 0);
		}
		if (MenuItems.Count == 0 && Windows.Count == 0)
		{
			API.ResetScriptGfxAlign();
			return;
		}
		_background.Size = BackgroundSize;
		_background.Draw();
		MenuItems[_activeItem % MenuItems.Count].Selected = true;
		if (!string.IsNullOrWhiteSpace(MenuItems[_activeItem % MenuItems.Count].Description))
		{
			((Rectangle)_descriptionBar).Draw();
			_descriptionRectangle.Draw();
			((Text)_descriptionText).Draw();
		}
		float num = CalculateWindowHeight();
		if (MenuItems.Count <= 10)
		{
			int num2 = 0;
			foreach (UIMenuItem menuItem in MenuItems)
			{
				menuItem.Position(num2 * 38 - 37 + _extraYOffset + (int)Math.Round(num));
				menuItem.Draw();
				num2++;
			}
		}
		else
		{
			int num3 = 0;
			for (int i = _minItem; i <= _maxItem; i++)
			{
				UIMenuItem uIMenuItem = MenuItems[i];
				uIMenuItem.Position(num3 * 38 - 37 + _extraYOffset + (int)Math.Round(num));
				uIMenuItem.Draw();
				num3++;
			}
			((Rectangle)_extraRectangleUp).Draw();
			((Rectangle)_extraRectangleDown).Draw();
			_upAndDownSprite.Draw();
			if (_counterText != null)
			{
				string text = CurrentSelection + 1 + " / " + Size;
				((Text)_counterText).Caption = CounterPretext + text;
				((Text)_counterText).Draw();
			}
		}
		if (Windows.Count > 0)
		{
			float num4 = 0f;
			for (int j = 0; j < Windows.Count; j++)
			{
				if (j > 0)
				{
					num4 += Windows[j].Background.Size.Height;
				}
				Windows[j].Position(num4 + (float)_extraYOffset + 37f);
				Windows[j].Draw();
			}
		}
		if (MenuItems[CurrentSelection] is UIMenuListItem && (MenuItems[CurrentSelection] as UIMenuListItem).Panels.Count > 0)
		{
			PanelOffset = CalculatePanelsPosition(!string.IsNullOrWhiteSpace(MenuItems[CurrentSelection].Description));
			for (int k = 0; k < (MenuItems[CurrentSelection] as UIMenuListItem).Panels.Count; k++)
			{
				if (k > 0)
				{
					PanelOffset = PanelOffset + (MenuItems[CurrentSelection] as UIMenuListItem).Panels[k - 1].Background.Size.Height + 5;
				}
				(MenuItems[CurrentSelection] as UIMenuListItem).Panels[k].Position(PanelOffset);
				(MenuItems[CurrentSelection] as UIMenuListItem).Panels[k].Draw();
			}
		}
		if (ScaleWithSafezone)
		{
			API.ResetScriptGfxAlign();
		}
	}

	public void ProcessMouse()
	{
		float num = CalculateWindowHeight();
		if (!Visible || _justOpened || MenuItems.Count == 0 || IsUsingController || !MouseControlsEnabled)
		{
			API.EnableControlAction(2, 2, true);
			API.EnableControlAction(2, 1, true);
			API.EnableControlAction(2, 25, true);
			API.EnableControlAction(2, 24, true);
			if (_itemsDirty)
			{
				MenuItems.Where((UIMenuItem i) => i.Hovered).ToList().ForEach(delegate(UIMenuItem i)
				{
					i.Hovered = false;
				});
				_itemsDirty = false;
			}
			return;
		}
		PointF safezone = ScreenTools.SafezoneBounds;
		API.ShowCursorThisFrame();
		int num2 = MenuItems.Count - 1;
		int num3 = 0;
		if (MenuItems.Count > 10)
		{
			num2 = _maxItem;
		}
		if (ScreenTools.IsMouseInBounds(new PointF(0f, 0f), new SizeF(30f, 1080f)) && MouseEdgeEnabled)
		{
			GameplayCamera.RelativeHeading += 5f;
			API.SetCursorSprite(6);
		}
		else if (ScreenTools.IsMouseInBounds(new PointF(Convert.ToInt32(Resolution.Width - 30f), 0f), new SizeF(30f, 1080f)) && MouseEdgeEnabled)
		{
			GameplayCamera.RelativeHeading -= 5f;
			API.SetCursorSprite(7);
		}
		else if (MouseEdgeEnabled)
		{
			API.SetCursorSprite(1);
		}
		for (int num4 = _minItem; num4 <= num2; num4++)
		{
			float x = Offset.X + safezone.X;
			float y = Offset.Y + 144f - 37f + (float)_extraYOffset + (float)(num3 * 38) + safezone.Y + num;
			float y2 = Offset.Y + 144f - 37f + (float)_extraYOffset + safezone.Y + (float)(CurrentSelection * 38) + num;
			int num5 = 431 + WidthOffset;
			UIMenuItem uIMenuItem = MenuItems[num4];
			if (ScreenTools.IsMouseInBounds(new PointF(x, y), new SizeF(num5, 38f)))
			{
				uIMenuItem.Hovered = true;
				int num6 = IsMouseInListItemArrows(MenuItems[num4], new PointF(x, y2), safezone);
				if (uIMenuItem.Hovered && num6 == 1 && MenuItems[num4] is IListItem)
				{
					API.SetMouseCursorSprite(5);
				}
				if (Game.IsControlJustPressed(0, (Control)24))
				{
					if (uIMenuItem.Selected && uIMenuItem.Enabled)
					{
						if (MenuItems[num4] is IListItem && IsMouseInListItemArrows(MenuItems[num4], new PointF(x, y), safezone) > 0)
						{
							switch (num6)
							{
							case 1:
								SelectItem();
								break;
							case 2:
								GoRight();
								break;
							}
						}
						else
						{
							SelectItem();
						}
					}
					else if (!uIMenuItem.Selected)
					{
						CurrentSelection = num4;
						Game.PlaySound(AUDIO_UPDOWN, AUDIO_LIBRARY);
						IndexChange(CurrentSelection);
						UpdateScaleform();
					}
					else if (!uIMenuItem.Enabled && uIMenuItem.Selected)
					{
						Game.PlaySound(AUDIO_ERROR, AUDIO_LIBRARY);
					}
				}
			}
			else
			{
				uIMenuItem.Hovered = false;
			}
			num3++;
		}
		float num7 = 524f + Offset.Y - 37f + (float)_extraYOffset + safezone.Y + num;
		float x2 = safezone.X + Offset.X;
		if (Size <= 10)
		{
			return;
		}
		if (ScreenTools.IsMouseInBounds(new PointF(x2, num7), new SizeF(431 + WidthOffset, 18f)))
		{
			((Rectangle)_extraRectangleUp).Color = Color.FromArgb(255, 30, 30, 30);
			if (Game.IsControlJustPressed(0, (Control)24))
			{
				if (Size > 10)
				{
					GoUpOverflow();
				}
				else
				{
					GoUp();
				}
			}
		}
		else
		{
			((Rectangle)_extraRectangleUp).Color = Color.FromArgb(200, 0, 0, 0);
		}
		if (ScreenTools.IsMouseInBounds(new PointF(x2, num7 + 18f), new SizeF(431 + WidthOffset, 18f)))
		{
			((Rectangle)_extraRectangleDown).Color = Color.FromArgb(255, 30, 30, 30);
			if (Game.IsControlJustPressed(0, (Control)24))
			{
				if (Size > 10)
				{
					GoDownOverflow();
				}
				else
				{
					GoDown();
				}
			}
		}
		else
		{
			((Rectangle)_extraRectangleDown).Color = Color.FromArgb(200, 0, 0, 0);
		}
	}

	public void ProcessControl(Keys key = Keys.None)
	{
		if (!Visible)
		{
			return;
		}
		if (_justOpened)
		{
			_justOpened = false;
		}
		else
		{
			if (API.UpdateOnscreenKeyboard() == 0 || API.IsWarningMessageActive())
			{
				return;
			}
			if (HasControlJustBeenReleaseed(MenuControls.Back, key) && CanUserGoBack)
			{
				GoBack();
			}
			if (MenuItems.Count == 0)
			{
				return;
			}
			if (HasControlJustBeenPressed(MenuControls.Select, key))
			{
				SelectItem();
				return;
			}
			if (IsControlBeingPressed(MenuControls.Up, key))
			{
				int num = ((_pressingTicks[0] > 2) ? 100 : 250);
				if (API.GetGameTimer() - _pressingTimer[0] > num)
				{
					if (Size > 10)
					{
						GoUpOverflow();
					}
					else
					{
						GoUp();
					}
					UpdateScaleform();
					_pressingTimer[0] = API.GetGameTimer();
					_pressingTicks[0]++;
				}
				return;
			}
			_pressingTimer[0] = 0;
			_pressingTicks[0] = 0;
			if (IsControlBeingPressed(MenuControls.Down, key))
			{
				int num2 = ((_pressingTicks[1] > 2) ? 100 : 250);
				if (API.GetGameTimer() - _pressingTimer[1] > num2)
				{
					if (Size > 10)
					{
						GoDownOverflow();
					}
					else
					{
						GoDown();
					}
					UpdateScaleform();
					_pressingTimer[1] = API.GetGameTimer();
					_pressingTicks[1]++;
				}
				return;
			}
			_pressingTimer[1] = 0;
			_pressingTicks[1] = 0;
			if (IsControlBeingPressed(MenuControls.Left, key))
			{
				int num3 = ((_pressingTicks[2] > 2) ? 250 : 500);
				if (API.GetGameTimer() - _pressingTimer[2] > num3)
				{
					GoLeft();
					_pressingTimer[2] = API.GetGameTimer();
					_pressingTicks[2]++;
				}
				return;
			}
			_pressingTimer[2] = 0;
			_pressingTicks[2] = 0;
			if (IsControlBeingPressed(MenuControls.Right, key))
			{
				int num4 = ((_pressingTicks[3] > 2) ? 250 : 500);
				if (API.GetGameTimer() - _pressingTimer[3] > num4)
				{
					GoRight();
					_pressingTimer[3] = API.GetGameTimer();
					_pressingTicks[3]++;
				}
			}
			else
			{
				_pressingTimer[3] = 0;
				_pressingTicks[3] = 0;
			}
		}
	}

	public void ProcessKey(Keys key)
	{
		if ((from MenuControls menuControl in _menuControls
			select new List<Keys>(_keyDictionary[menuControl].Item1)).Any((List<Keys> tmpKeys) => tmpKeys.Any((Keys k) => k == key)))
		{
			ProcessControl(key);
		}
	}

	public void UpdateScaleform()
	{
		if (!Visible || !_buttonsEnabled)
		{
			return;
		}
		_instructionalButtonsScaleform.CallFunction("CLEAR_ALL", new object[0]);
		_instructionalButtonsScaleform.CallFunction("TOGGLE_MOUSE_BUTTONS", new object[1] { 0 });
		_instructionalButtonsScaleform.CallFunction("CREATE_CONTAINER", new object[0]);
		int num = 0;
		foreach (InstructionalButton item in _instructionalButtons.Where((InstructionalButton button) => button.ItemBind == null || MenuItems[CurrentSelection] == button.ItemBind))
		{
			_instructionalButtonsScaleform.CallFunction("SET_DATA_SLOT", new object[3]
			{
				num,
				item.GetButtonId(),
				item.Text
			});
			num++;
		}
		_instructionalButtonsScaleform.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", new object[1] { -1 });
	}

	protected virtual void IndexChange(int newindex)
	{
		ReDraw = true;
		this.OnIndexChange?.Invoke(this, newindex);
	}

	internal virtual void ListChange(UIMenuListItem sender, int newindex)
	{
		this.OnListChange?.Invoke(this, sender, newindex);
	}

	internal virtual void ProgressChange(UIMenuProgressItem sender, int newindex)
	{
		this.OnProgressChange?.Invoke(this, sender, newindex);
	}

	protected virtual void ListSelect(UIMenuListItem sender, int newindex)
	{
		this.OnListSelect?.Invoke(this, sender, newindex);
	}

	protected virtual void SliderChange(UIMenuSliderItem sender, int newindex)
	{
		this.OnSliderChange?.Invoke(this, sender, newindex);
	}

	internal virtual void SliderProgressChange(UIMenuSliderProgressItem sender, int newindex)
	{
		this.OnProgressSliderChange?.Invoke(this, sender, newindex);
	}

	protected virtual void ItemSelect(UIMenuItem selecteditem, int index)
	{
		this.OnItemSelect?.Invoke(this, selecteditem, index);
	}

	protected virtual void CheckboxChange(UIMenuCheckboxItem sender, bool Checked)
	{
		this.OnCheckboxChange?.Invoke(this, sender, Checked);
	}

	protected virtual void MenuOpenEv()
	{
		this.OnMenuOpen?.Invoke(this);
	}

	protected virtual void MenuCloseEv()
	{
		this.OnMenuClose?.Invoke(this);
	}

	protected virtual void MenuChangeEv(UIMenu newmenu, bool forward)
	{
		this.OnMenuChange?.Invoke(this, newmenu, forward);
	}
}
