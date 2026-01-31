using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr;
using Gtacnr.Client.Libs;

namespace MenuAPI;

public class Menu
{
	public delegate void ItemSelectEvent(Menu menu, MenuItem menuItem, int itemIndex);

	public delegate void CheckboxItemChangeEvent(Menu menu, MenuCheckboxItem menuItem, int itemIndex, bool newCheckedState);

	public delegate void ListItemSelectedEvent(Menu menu, MenuListItem listItem, int selectedIndex, int itemIndex);

	public delegate void ListItemIndexChangedEvent(Menu menu, MenuListItem listItem, int oldSelectionIndex, int newSelectionIndex, int itemIndex);

	public delegate void MenuClosedEvent(Menu menu, MenuClosedEventArgs e);

	public delegate bool MenuClosingEvent(Menu menu);

	public delegate void MenuOpenedEvent(Menu menu);

	public delegate void IndexChangedEvent(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex);

	public delegate void SliderPositionChangedEvent(Menu menu, MenuSliderItem sliderItem, int oldPosition, int newPosition, int itemIndex);

	public delegate void SliderItemSelectedEvent(Menu menu, MenuSliderItem sliderItem, int sliderPosition, int itemIndex);

	public delegate void DynamicListItemCurrentItemChangedEvent(Menu menu, MenuDynamicListItem dynamicListItem, string oldValue, string newValue);

	public delegate void DynamicListItemSelectedEvent(Menu menu, MenuDynamicListItem dynamicListItem, string currentItem);

	public struct InstructionalButton
	{
		public string controlString;

		public string instructionText;

		public InstructionalButton(string controlString, string instructionText)
		{
			this.controlString = controlString;
			this.instructionText = instructionText;
		}
	}

	public enum ControlPressCheckType
	{
		JUST_RELEASED,
		JUST_PRESSED,
		RELEASED,
		PRESSED
	}

	public class ButtonPressHandler
	{
		public Control Control { get; private set; }

		public ControlPressCheckType PressType { get; private set; }

		public Action<Menu, Control> Function { get; private set; }

		public bool DisableControl { get; private set; }

		public ButtonPressHandler()
		{
		}

		public ButtonPressHandler(Control control, ControlPressCheckType pressType, Action<Menu, Control> function, bool disableControl)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			Control = control;
			PressType = pressType;
			Function = function;
			DisableControl = disableControl;
		}
	}

	public const float Width = 500f;

	private static KeyValuePair<float, float> headerSize = new KeyValuePair<float, float>(500f, 110f);

	private int index;

	private bool visible;

	private float menuYOffset;

	private float previousMenuYOffset = -1f;

	private readonly int ColorPanelScaleform = API.RequestScaleformMovie("COLOUR_SWITCHER_02");

	private readonly int OpacityPanelScaleform = API.RequestScaleformMovie("COLOUR_SWITCHER_01");

	private string _menuTitle;

	private string _menuSubtitle;

	private bool disableDpadNavigation;

	private string _counterPreText;

	private readonly string[] weaponStatNames = new string[4] { "Damage", "Fire Rate", "Accuracy", "Range" };

	private readonly string[] vehicleStatNames = new string[4] { "FMMC_VEHST_0", "FMMC_VEHST_1", "FMMC_VEHST_2", "FMMC_VEHST_3" };

	private bool filterActive;

	public Dictionary<Control, string> InstructionalButtons = new Dictionary<Control, string>
	{
		{
			(Control)201,
			API.GetLabelText("HUD_INPUT28")
		},
		{
			(Control)202,
			API.GetLabelText("HUD_INPUT53")
		}
	};

	public List<InstructionalButton> CustomInstructionalButtons = new List<InstructionalButton>();

	public List<ButtonPressHandler> ButtonPressHandlers = new List<ButtonPressHandler>();

	public int ViewIndexOffset { get; private set; }

	private List<MenuItem> VisibleMenuItems
	{
		get
		{
			if (filterActive)
			{
				return FilterItems.ToList().GetRange(ViewIndexOffset, Math.Min(MaxItemsOnScreen, Size - ViewIndexOffset));
			}
			return GetMenuItems().ToList().GetRange(ViewIndexOffset, Math.Min(MaxItemsOnScreen, Size - ViewIndexOffset));
		}
	}

	private List<MenuItem> FilterItems { get; set; } = new List<MenuItem>();

	private List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

	public string MenuTitle
	{
		get
		{
			return _menuTitle;
		}
		set
		{
			_menuTitle = value.RegionalIndicatorsToLetters();
		}
	}

	public string MenuSubtitle
	{
		get
		{
			return _menuSubtitle;
		}
		set
		{
			_menuSubtitle = value.RegionalIndicatorsToLetters();
		}
	}

	public KeyValuePair<string, string> HeaderTexture { get; set; }

	public KeyValuePair<string, string> OverlayHeaderTexture { get; set; }

	public Font HeaderFont { get; set; } = MenuController.DefaultHeaderFont;

	public float HeaderFontSize { get; set; } = 1215f;

	public Font TextFont { get; set; } = MenuController.DefaultTextFont;

	public float TextFontSize { get; set; } = 378f;

	public MenuController.TextDirection TextDirection { get; set; } = MenuController.DefaultTextDirection;

	public bool IgnoreDontOpenMenus { get; set; }

	public int MaxItemsOnScreen { get; internal set; } = 10;

	public int Size
	{
		get
		{
			if (!filterActive)
			{
				return MenuItems.Count;
			}
			return FilterItems.Count;
		}
	}

	public bool PlaySelectSound { get; set; } = true;

	public bool PlayErrorSound { get; set; } = true;

	public bool CloseWhenDead { get; set; } = true;

	public bool DisableDpadNavigation => disableDpadNavigation;

	public float MaxDistance { get; set; }

	public Vector3 MenuCoords { get; set; }

	public bool Visible
	{
		get
		{
			return visible;
		}
		set
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			if (value)
			{
				MenuController.VisibleMenus.Add(this);
				disableDpadNavigation = true;
				MenuCoords = ((Entity)Game.PlayerPed).Position;
				Change();
			}
			else
			{
				MenuController.VisibleMenus.Remove(this);
			}
			visible = value;
			async void Change()
			{
				while (Game.IsControlPressed(2, (Control)190) || Game.IsControlPressed(2, (Control)189) || Game.IsControlPressed(2, (Control)187) || Game.IsControlPressed(2, (Control)188))
				{
					await BaseScript.Delay(0);
				}
				disableDpadNavigation = false;
			}
		}
	}

	public bool LeftAligned => MenuController.MenuAlignment == MenuController.MenuAlignmentOption.Left;

	public Vector2 Position { get; private set; } = new Vector2(0f, 40f);

	public float MenuItemsYOffset { get; private set; }

	public string CounterPreText
	{
		get
		{
			return _counterPreText;
		}
		set
		{
			_counterPreText = value.RegionalIndicatorsToLetters();
		}
	}

	public bool ShowCount { get; set; }

	public Menu? ParentMenu { get; internal set; }

	public List<Menu> ChildrenMenus { get; internal set; } = new List<Menu>();

	public int CurrentIndex
	{
		get
		{
			return index;
		}
		internal set
		{
			index = MathUtil.Clamp(value, 0, Math.Max(0, Size - 1));
		}
	}

	public bool EnableInstructionalButtons { get; set; } = true;

	public float[] WeaponStats { get; private set; } = new float[4];

	public float[] WeaponComponentStats { get; private set; } = new float[4];

	public float[] VehicleStats { get; private set; } = new float[4];

	public float[] VehicleUpgradeStats { get; private set; } = new float[4];

	public bool ShowWeaponStatsPanel { get; set; }

	public bool ShowVehicleStatsPanel { get; set; }

	public event ItemSelectEvent OnItemSelect;

	public event CheckboxItemChangeEvent OnCheckboxChange;

	public event ListItemSelectedEvent OnListItemSelect;

	public event ListItemIndexChangedEvent OnListIndexChange;

	public event MenuClosedEvent OnMenuClose;

	public event MenuClosingEvent OnMenuClosing;

	public event MenuOpenedEvent OnMenuOpen;

	public event IndexChangedEvent OnIndexChange;

	public event SliderPositionChangedEvent OnSliderPositionChange;

	public event SliderItemSelectedEvent OnSliderItemSelect;

	public event DynamicListItemCurrentItemChangedEvent OnDynamicListItemCurrentItemChange;

	public event DynamicListItemSelectedEvent OnDynamicListItemSelect;

	protected virtual void ItemSelectedEvent(MenuItem menuItem, int itemIndex)
	{
		this.OnItemSelect?.Invoke(this, menuItem, itemIndex);
	}

	protected virtual void CheckboxChangedEvent(MenuCheckboxItem menuItem, int itemIndex, bool _checked)
	{
		this.OnCheckboxChange?.Invoke(this, menuItem, itemIndex, _checked);
	}

	protected virtual void ListItemSelectEvent(Menu menu, MenuListItem listItem, int selectedIndex, int itemIndex)
	{
		this.OnListItemSelect?.Invoke(menu, listItem, selectedIndex, itemIndex);
	}

	protected virtual void ListItemIndexChangeEvent(Menu menu, MenuListItem listItem, int oldSelectionIndex, int newSelectionIndex, int itemIndex)
	{
		this.OnListIndexChange?.Invoke(menu, listItem, oldSelectionIndex, newSelectionIndex, itemIndex);
	}

	protected virtual void MenuCloseEvent(Menu menu, bool closedByUser, bool isOpeningSubmenu)
	{
		MenuClosedEventArgs e = new MenuClosedEventArgs
		{
			ClosedByUser = closedByUser,
			IsOpeningSubmenu = isOpeningSubmenu
		};
		this.OnMenuClose?.Invoke(menu, e);
	}

	protected virtual bool FireMenuClosingEvent(Menu menu)
	{
		return this.OnMenuClosing?.Invoke(menu) ?? true;
	}

	protected virtual void MenuOpenEvent(Menu menu)
	{
		this.OnMenuOpen?.Invoke(menu);
	}

	protected virtual void IndexChangeEvent(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex)
	{
		this.OnIndexChange?.Invoke(menu, oldItem, newItem, oldIndex, newIndex);
	}

	protected virtual void SliderItemChangedEvent(Menu menu, MenuSliderItem sliderItem, int oldPosition, int newPosition, int itemIndex)
	{
		this.OnSliderPositionChange?.Invoke(menu, sliderItem, oldPosition, newPosition, itemIndex);
	}

	protected virtual void SliderSelectedEvent(Menu menu, MenuSliderItem sliderItem, int sliderPosition, int itemIndex)
	{
		this.OnSliderItemSelect?.Invoke(menu, sliderItem, sliderPosition, itemIndex);
	}

	protected virtual void DynamicListItemCurrentItemChanged(Menu menu, MenuDynamicListItem dynamicListItem, string oldValue, string newValue)
	{
		this.OnDynamicListItemCurrentItemChange?.Invoke(menu, dynamicListItem, oldValue, newValue);
	}

	protected virtual void DynamicListItemSelectEvent(Menu menu, MenuDynamicListItem dynamicListItem, string currentItem)
	{
		this.OnDynamicListItemSelect?.Invoke(menu, dynamicListItem, currentItem);
	}

	public Menu(string name)
		: this(name, null)
	{
	}

	public Menu(string name, string subtitle)
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		MenuTitle = name;
		MenuSubtitle = subtitle;
		SetWeaponStats(0f, 0f, 0f, 0f);
		SetWeaponComponentStats(0f, 0f, 0f, 0f);
	}

	public void SetMaxItemsOnScreen(int max)
	{
		if (max < 11 && max > 2)
		{
			MaxItemsOnScreen = max;
		}
	}

	public void RefreshIndex()
	{
		RefreshIndex(0, 0);
	}

	public void RefreshIndex(int index)
	{
		RefreshIndex(index, (index > MaxItemsOnScreen) ? (index - MaxItemsOnScreen) : 0);
	}

	public void RefreshIndex(int index, int viewOffset)
	{
		CurrentIndex = index;
		ViewIndexOffset = viewOffset;
	}

	public List<MenuItem> GetMenuItems()
	{
		if (!filterActive)
		{
			return MenuItems.ToList();
		}
		return FilterItems.ToList();
	}

	public MenuItem GetCurrentMenuItem()
	{
		List<MenuItem> menuItems = GetMenuItems();
		if (menuItems.Count > CurrentIndex)
		{
			try
			{
				return menuItems[CurrentIndex];
			}
			catch (Exception ex)
			{
				string text = "";
				foreach (MenuItem item in menuItems)
				{
					text = text + item.Text + ", ";
				}
				text = text.Trim(',', ' ');
				Debug.WriteLine($"[MenuAPI ({API.GetCurrentResourceName()})] Error: Could not get currrent menu item, error details: {ex.Message}. Current index: {CurrentIndex}. Current menu size: {Size}. Current menu name: {MenuTitle}. List of menu items: {text}.");
			}
		}
		return null;
	}

	public void ClearMenuItems()
	{
		CurrentIndex = 0;
		ViewIndexOffset = 0;
		MenuItems.Clear();
		FilterItems.Clear();
	}

	public void ClearMenuItems(bool dontResetIndex)
	{
		if (!dontResetIndex)
		{
			CurrentIndex = 0;
			ViewIndexOffset = 0;
		}
		MenuItems.Clear();
		FilterItems.Clear();
	}

	public void AddMenuItem(MenuItem item)
	{
		MenuItems.Add(item);
		item.PositionOnScreen = item.Index;
		item.ParentMenu = this;
	}

	public void InsertMenuItem(MenuItem item, int index)
	{
		MenuItems.Insert(index, item);
		item.PositionOnScreen = item.Index;
		item.ParentMenu = this;
	}

	public void RemoveMenuItem(int itemIndex)
	{
		if (CurrentIndex >= itemIndex)
		{
			if (Size > CurrentIndex)
			{
				CurrentIndex--;
			}
			else
			{
				CurrentIndex = 0;
			}
		}
		if (itemIndex < Size && itemIndex > -1)
		{
			RemoveMenuItem(MenuItems[itemIndex]);
		}
	}

	public void RemoveMenuItem(MenuItem item)
	{
		if (!MenuItems.Contains(item))
		{
			return;
		}
		if (CurrentIndex >= item.Index)
		{
			if (Size > CurrentIndex)
			{
				CurrentIndex--;
			}
			else
			{
				CurrentIndex = 0;
			}
		}
		MenuItems.Remove(item);
	}

	public void SelectItem(int index)
	{
		if (!filterActive)
		{
			if (index > -1 && MenuItems.Count - 1 >= index)
			{
				SelectItem(MenuItems[index]);
			}
		}
		else if (index > -1 && FilterItems.Count - 1 >= index)
		{
			SelectItem(FilterItems[index]);
		}
	}

	public void SelectItem(MenuItem item)
	{
		if (item != null && item.Enabled)
		{
			if (item is MenuCheckboxItem menuCheckboxItem)
			{
				menuCheckboxItem.Checked = !menuCheckboxItem.Checked;
				CheckboxChangedEvent(menuCheckboxItem, item.Index, menuCheckboxItem.Checked);
			}
			else if (item is MenuListItem menuListItem)
			{
				ListItemSelectEvent(this, menuListItem, menuListItem.ListIndex, menuListItem.Index);
			}
			else if (item is MenuDynamicListItem menuDynamicListItem)
			{
				DynamicListItemSelectEvent(this, menuDynamicListItem, menuDynamicListItem.CurrentItem);
			}
			else if (item is MenuSliderItem menuSliderItem)
			{
				SliderSelectedEvent(this, menuSliderItem, menuSliderItem.Position, menuSliderItem.Index);
			}
			else
			{
				ItemSelectedEvent(item, item.Index);
			}
			if (PlaySelectSound && item.PlaySelectSound)
			{
				API.PlaySoundFrontend(-1, "SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
			}
			if (MenuController.MenuButtons.ContainsKey(item))
			{
				MenuController.AddSubmenu(MenuController.GetCurrentMenu(), MenuController.MenuButtons[item]);
				MenuController.GetCurrentMenu().CloseMenu(closedByUser: false, isOpeningSubmenu: true);
				MenuController.MenuButtons[item].OpenMenu();
			}
		}
		else if (item != null && !item.Enabled && PlayErrorSound && item.PlayErrorSound)
		{
			API.PlaySoundFrontend(-1, "ERROR", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
		}
	}

	public void GoBack()
	{
		API.PlaySoundFrontend(-1, "BACK", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
		if (FireMenuClosingEvent(this))
		{
			CloseMenu(closedByUser: true, ParentMenu != null);
			if (ParentMenu != null)
			{
				ParentMenu.OpenMenu();
			}
		}
	}

	public void CloseMenu(bool closedByUser = false, bool isOpeningSubmenu = false)
	{
		Visible = false;
		MenuCloseEvent(this, closedByUser, isOpeningSubmenu);
		if (!MenuController.IsAnyMenuOpen())
		{
			previousMenuYOffset = -1f;
		}
	}

	public void OpenMenu()
	{
		previousMenuYOffset = -1f;
		Visible = true;
		MenuOpenEvent(this);
	}

	public void GoUp()
	{
		if (!Visible || Size <= 1)
		{
			return;
		}
		MenuItem menuItem = ((!filterActive) ? MenuItems[CurrentIndex] : FilterItems[CurrentIndex]);
		if (CurrentIndex == 0)
		{
			CurrentIndex = Size - 1;
		}
		else
		{
			CurrentIndex--;
		}
		MenuItem currentMenuItem = GetCurrentMenuItem();
		if (currentMenuItem == null || !VisibleMenuItems.Contains(currentMenuItem))
		{
			ViewIndexOffset--;
			if (ViewIndexOffset < 0)
			{
				ViewIndexOffset = Math.Max(Size - MaxItemsOnScreen, 0);
			}
		}
		IndexChangeEvent(this, menuItem, currentMenuItem, menuItem.Index, CurrentIndex);
		API.PlaySoundFrontend(-1, "NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
	}

	public void GoDown()
	{
		if (!Visible || Size <= 1)
		{
			return;
		}
		MenuItem menuItem = ((!filterActive) ? MenuItems[CurrentIndex] : FilterItems[CurrentIndex]);
		if (CurrentIndex > 0 && CurrentIndex >= Size - 1)
		{
			CurrentIndex = 0;
		}
		else
		{
			CurrentIndex++;
		}
		MenuItem currentMenuItem = GetCurrentMenuItem();
		if (currentMenuItem == null || !VisibleMenuItems.Contains(currentMenuItem))
		{
			ViewIndexOffset++;
			if (CurrentIndex == 0)
			{
				ViewIndexOffset = 0;
			}
		}
		IndexChangeEvent(this, menuItem, currentMenuItem, menuItem.Index, CurrentIndex);
		API.PlaySoundFrontend(-1, "NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
	}

	public void GoLeft()
	{
		if (!MenuController.AreMenuButtonsEnabled)
		{
			return;
		}
		MenuItem currentMenuItem = GetCurrentMenuItem();
		if (currentMenuItem == null)
		{
			return;
		}
		if (currentMenuItem.Enabled && currentMenuItem is MenuListItem menuListItem)
		{
			if (menuListItem.ItemsCount > 0)
			{
				int listIndex = menuListItem.ListIndex;
				int num = listIndex;
				num = (menuListItem.ListIndex = ((menuListItem.ListIndex >= 1) ? (num - 1) : (menuListItem.ItemsCount - 1)));
				ListItemIndexChangeEvent(this, menuListItem, listIndex, num, menuListItem.Index);
				API.PlaySoundFrontend(-1, "NAV_LEFT_RIGHT", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
			}
		}
		else if (currentMenuItem.Enabled && currentMenuItem is MenuSliderItem menuSliderItem)
		{
			if (menuSliderItem.Position > menuSliderItem.Min)
			{
				SliderItemChangedEvent(this, menuSliderItem, menuSliderItem.Position, menuSliderItem.Position - 1, menuSliderItem.Index);
				menuSliderItem.Position--;
				API.PlaySoundFrontend(-1, "NAV_LEFT_RIGHT", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
			}
			else
			{
				API.PlaySoundFrontend(-1, "ERROR", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
			}
		}
		else if (currentMenuItem.Enabled && currentMenuItem is MenuDynamicListItem menuDynamicListItem)
		{
			string currentItem = menuDynamicListItem.CurrentItem;
			string newValue = (menuDynamicListItem.CurrentItem = menuDynamicListItem.Callback(menuDynamicListItem, left: true));
			DynamicListItemCurrentItemChanged(this, menuDynamicListItem, currentItem, newValue);
			API.PlaySoundFrontend(-1, "NAV_LEFT_RIGHT", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
		}
		else if (currentMenuItem.Enabled && currentMenuItem is MenuCheckboxItem item)
		{
			SelectItem(item);
		}
		else if (MenuController.NavigateMenuUsingArrows && !MenuController.DisableBackButton && (!MenuController.PreventExitingMenu || ParentMenu != null))
		{
			GoBack();
		}
	}

	public void GoRight()
	{
		if (!MenuController.AreMenuButtonsEnabled)
		{
			return;
		}
		MenuItem currentMenuItem = GetCurrentMenuItem();
		if (currentMenuItem != null && currentMenuItem.Enabled && currentMenuItem is MenuListItem menuListItem)
		{
			if (menuListItem.ItemsCount > 0)
			{
				int listIndex = menuListItem.ListIndex;
				int num = listIndex;
				num = (menuListItem.ListIndex = ((menuListItem.ListIndex < menuListItem.ItemsCount - 1) ? (num + 1) : 0));
				ListItemIndexChangeEvent(this, menuListItem, listIndex, num, menuListItem.Index);
				API.PlaySoundFrontend(-1, "NAV_LEFT_RIGHT", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
			}
		}
		else if (currentMenuItem.Enabled && currentMenuItem is MenuSliderItem menuSliderItem)
		{
			if (menuSliderItem.Position < menuSliderItem.Max)
			{
				SliderItemChangedEvent(this, menuSliderItem, menuSliderItem.Position, menuSliderItem.Position + 1, menuSliderItem.Index);
				menuSliderItem.Position++;
				API.PlaySoundFrontend(-1, "NAV_LEFT_RIGHT", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
			}
			else
			{
				API.PlaySoundFrontend(-1, "ERROR", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
			}
		}
		else if (currentMenuItem.Enabled && currentMenuItem is MenuDynamicListItem menuDynamicListItem)
		{
			string currentItem = menuDynamicListItem.CurrentItem;
			string newValue = (menuDynamicListItem.CurrentItem = menuDynamicListItem.Callback(menuDynamicListItem, left: false));
			DynamicListItemCurrentItemChanged(this, menuDynamicListItem, currentItem, newValue);
			API.PlaySoundFrontend(-1, "NAV_LEFT_RIGHT", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
		}
		else if (currentMenuItem.Enabled && currentMenuItem is MenuCheckboxItem item)
		{
			SelectItem(item);
		}
		else if (MenuController.NavigateMenuUsingArrows && currentMenuItem.Enabled)
		{
			SelectItem(currentMenuItem);
		}
	}

	public void SortMenuItems(Comparison<MenuItem> compare)
	{
		if (filterActive)
		{
			filterActive = false;
			FilterItems.Clear();
		}
		MenuItems.Sort(compare);
	}

	public void FilterMenuItems(Func<MenuItem, bool> predicate)
	{
		if (filterActive)
		{
			ResetFilter();
		}
		RefreshIndex(0, 0);
		ViewIndexOffset = 0;
		FilterItems = MenuItems.Where((MenuItem i) => predicate(i)).ToList();
		filterActive = true;
	}

	public void ResetFilter()
	{
		RefreshIndex(0, 0);
		filterActive = false;
		FilterItems.Clear();
	}

	public void SetWeaponStats(float damage, float fireRate, float accuracy, float range)
	{
		WeaponStats = new float[4]
		{
			MathUtil.Clamp(damage, 0f, 1f),
			MathUtil.Clamp(fireRate, 0f, 1f),
			MathUtil.Clamp(accuracy, 0f, 1f),
			MathUtil.Clamp(range, 0f, 1f)
		};
	}

	public void SetWeaponComponentStats(float damage, float fireRate, float accuracy, float range)
	{
		WeaponComponentStats = new float[4]
		{
			MathUtil.Clamp(WeaponStats[0] + damage, 0f, 1f),
			MathUtil.Clamp(WeaponStats[1] + fireRate, 0f, 1f),
			MathUtil.Clamp(WeaponStats[2] + accuracy, 0f, 1f),
			MathUtil.Clamp(WeaponStats[3] + range, 0f, 1f)
		};
	}

	public void SetVehicleStats(float topSpeed, float acceleration, float braking, float traction)
	{
		VehicleStats = new float[4]
		{
			MathUtil.Clamp(topSpeed, 0f, 1f),
			MathUtil.Clamp(acceleration, 0f, 1f),
			MathUtil.Clamp(braking, 0f, 1f),
			MathUtil.Clamp(traction, 0f, 1f)
		};
	}

	public void SetVehicleUpgradeStats(float topSpeed, float acceleration, float braking, float traction)
	{
		VehicleUpgradeStats = new float[4]
		{
			MathUtil.Clamp(VehicleStats[0] + topSpeed, 0f, 1f),
			MathUtil.Clamp(VehicleStats[1] + acceleration, 0f, 1f),
			MathUtil.Clamp(VehicleStats[2] + braking, 0f, 1f),
			MathUtil.Clamp(VehicleStats[3] + traction, 0f, 1f)
		};
	}

	internal async void Draw()
	{
		if (MaxDistance > 0f)
		{
			Vector3 menuCoords = MenuCoords;
			Vector3 val = default(Vector3);
			if (menuCoords != val)
			{
				val = ((Entity)Game.PlayerPed).Position;
				if (((Vector3)(ref val)).DistanceToSquared2D(MenuCoords) > MaxDistance * MaxDistance)
				{
					CloseMenu();
					return;
				}
			}
		}
		float num20;
		if (!Game.IsPaused && API.IsScreenFadedIn() && !API.IsPlayerSwitchInProgress())
		{
			if (ButtonPressHandlers.Count > 0 && !MenuController.DisableMenuButtons)
			{
				foreach (ButtonPressHandler buttonPressHandler in ButtonPressHandlers)
				{
					if (buttonPressHandler.DisableControl)
					{
						API.DisableControlAction(0, (int)buttonPressHandler.Control, true);
						API.DisableControlAction(2, (int)buttonPressHandler.Control, true);
					}
					switch (buttonPressHandler.PressType)
					{
					case ControlPressCheckType.JUST_PRESSED:
						if (Game.IsControlJustPressed(0, buttonPressHandler.Control) || Game.IsDisabledControlJustPressed(0, buttonPressHandler.Control))
						{
							buttonPressHandler.Function(this, buttonPressHandler.Control);
						}
						break;
					case ControlPressCheckType.JUST_RELEASED:
						if (Game.IsControlJustReleased(0, buttonPressHandler.Control) || Game.IsDisabledControlJustReleased(0, buttonPressHandler.Control))
						{
							buttonPressHandler.Function(this, buttonPressHandler.Control);
						}
						break;
					case ControlPressCheckType.PRESSED:
						if (Game.IsControlPressed(0, buttonPressHandler.Control) || Game.IsDisabledControlPressed(0, buttonPressHandler.Control))
						{
							buttonPressHandler.Function(this, buttonPressHandler.Control);
						}
						break;
					case ControlPressCheckType.RELEASED:
						if (!Game.IsControlPressed(0, buttonPressHandler.Control) && !Game.IsDisabledControlPressed(0, buttonPressHandler.Control))
						{
							buttonPressHandler.Function(this, buttonPressHandler.Control);
						}
						break;
					}
				}
			}
			MenuItemsYOffset = 0f;
			menuYOffset = 0f;
			if (MenuController.SetDrawOrder)
			{
				API.SetScriptGfxDrawOrder(1);
			}
			if (!string.IsNullOrEmpty(MenuTitle))
			{
				API.SetScriptGfxAlign(LeftAligned ? 76 : 82, 84);
				API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
				float x = (Position.X + headerSize.Key / 2f) / MenuController.ScreenWidth;
				float y = (Position.Y + MenuItemsYOffset + headerSize.Value / 2f) / MenuController.ScreenHeight;
				float width = headerSize.Key / MenuController.ScreenWidth;
				float height = headerSize.Value / MenuController.ScreenHeight;
				if (!string.IsNullOrEmpty(HeaderTexture.Key) && !string.IsNullOrEmpty(HeaderTexture.Value))
				{
					if (!API.HasStreamedTextureDictLoaded(HeaderTexture.Key))
					{
						API.RequestStreamedTextureDict(HeaderTexture.Key, false);
						while (!API.HasStreamedTextureDictLoaded(HeaderTexture.Key))
						{
							await BaseScript.Delay(0);
						}
					}
					API.DrawSprite(HeaderTexture.Key, HeaderTexture.Value, x, y, width, height, 0f, 255, 255, 255, 255);
				}
				else
				{
					API.DrawSprite("commonmenu", "interaction_bgd", x, y, width, height, 0f, 255, 255, 255, 255);
				}
				if (!string.IsNullOrEmpty(OverlayHeaderTexture.Key) && !string.IsNullOrEmpty(OverlayHeaderTexture.Value))
				{
					if (!API.HasStreamedTextureDictLoaded(OverlayHeaderTexture.Key))
					{
						API.RequestStreamedTextureDict(OverlayHeaderTexture.Key, false);
						while (!API.HasStreamedTextureDictLoaded(OverlayHeaderTexture.Key))
						{
							await BaseScript.Delay(0);
						}
					}
					Vector3 textureResolution = API.GetTextureResolution(OverlayHeaderTexture.Key, OverlayHeaderTexture.Value);
					float num = textureResolution.X / textureResolution.Y;
					float num2 = num * headerSize.Value / MenuController.ScreenWidth;
					float num3 = (Position.X + num * headerSize.Value / 2f) / MenuController.ScreenWidth - (width - num2) / 2f;
					API.DrawSprite(OverlayHeaderTexture.Key, OverlayHeaderTexture.Value, num3, y, num2, height, 0f, 255, 255, 255, 255);
				}
				API.ResetScriptGfxAlign();
				int headerFont = (int)HeaderFont;
				float num4 = HeaderFontSize / MenuController.ScreenHeight;
				BetterDrawTextHelper.BeginTextCommandDisplayText(MenuTitle);
				BetterDrawTextHelper.SetScriptGfxAlign(GfxAlign.Left, GfxAlign.Top);
				BetterDrawTextHelper.SetTextFont((Font)headerFont);
				BetterDrawTextHelper.SetTextColour(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
				BetterDrawTextHelper.SetTextScale(num4);
				BetterDrawTextHelper.SetTextJustification(TextJustification.Center);
				if (LeftAligned)
				{
					BetterDrawTextHelper.EndTextCommandDisplayText(headerSize.Key / 2f / MenuController.ScreenWidth, y - API.GetTextScaleHeight(num4, headerFont) / 2f);
				}
				else
				{
					BetterDrawTextHelper.EndTextCommandDisplayText(API.GetSafeZoneSize() - headerSize.Key / 2f / MenuController.ScreenWidth, y - API.GetTextScaleHeight(num4, headerFont) / 2f);
				}
				MenuItemsYOffset += headerSize.Value;
				menuYOffset += y + height;
			}
			API.SetScriptGfxAlign(LeftAligned ? 76 : 82, 84);
			API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
			float num5 = 38f;
			float num6 = (Position.X + headerSize.Key / 2f) / MenuController.ScreenWidth;
			float num7 = (Position.Y + MenuItemsYOffset + num5 / 2f) / MenuController.ScreenHeight;
			float num8 = headerSize.Key / MenuController.ScreenWidth;
			float num9 = num5 / MenuController.ScreenHeight;
			API.DrawRect(num6, num7, num8, num9, 0, 0, 0, 220);
			API.ResetScriptGfxAlign();
			menuYOffset += num9;
			if (!string.IsNullOrEmpty(MenuSubtitle))
			{
				int textFont = (int)TextFont;
				float num10 = TextFontSize / MenuController.ScreenHeight;
				if (MenuSubtitle.Contains("~") || string.IsNullOrEmpty(MenuTitle))
				{
					BetterDrawTextHelper.BeginTextCommandDisplayText(MenuSubtitle.ToUpper());
				}
				else
				{
					BetterDrawTextHelper.BeginTextCommandDisplayText("~HUD_COLOUR_FREEMODE~" + MenuSubtitle.ToUpper());
				}
				BetterDrawTextHelper.SetScriptGfxAlign(GfxAlign.Left, GfxAlign.Top);
				BetterDrawTextHelper.SetTextFont((Font)textFont);
				BetterDrawTextHelper.SetTextScale(num10);
				BetterDrawTextHelper.SetTextDirection(TextDirection == MenuController.TextDirection.RTL);
				float y2 = num7 - (API.GetTextScaleHeight(num10, textFont) / 2f + 4f / MenuController.ScreenHeight);
				float x2 = ((!LeftAligned) ? (API.GetSafeZoneSize() - (headerSize.Key - 10f) / MenuController.ScreenWidth) : (10f / MenuController.ScreenWidth));
				BetterDrawTextHelper.EndTextCommandDisplayText(x2, y2);
			}
			string text = (CounterPreText ?? "") ?? "";
			if (ShowCount)
			{
				text += $" {CurrentIndex + 1}/{MenuItems.Count}";
			}
			if (!string.IsNullOrEmpty(text) || MaxItemsOnScreen < Size)
			{
				int textFont2 = (int)TextFont;
				float num11 = TextFontSize / MenuController.ScreenHeight;
				if ((MenuSubtitle ?? "").Contains("~") || (CounterPreText ?? "").Contains("~") || string.IsNullOrEmpty(MenuTitle))
				{
					BetterDrawTextHelper.BeginTextCommandDisplayText(text.ToUpper());
				}
				else
				{
					BetterDrawTextHelper.BeginTextCommandDisplayText("~HUD_COLOUR_FREEMODE~" + text.ToUpper());
				}
				BetterDrawTextHelper.SetScriptGfxAlign(GfxAlign.Left, GfxAlign.Top);
				float y3 = num7 - (API.GetTextScaleHeight(num11, textFont2) / 2f + 4f / MenuController.ScreenHeight);
				float num12;
				float num14;
				float num13;
				if (LeftAligned)
				{
					num12 = 510f / MenuController.ScreenWidth;
					num13 = API.GetSafeZoneSize() - 510f / MenuController.ScreenWidth;
					num14 = 20f / MenuController.ScreenWidth;
					num13 += 0.01f;
					num12 += 0.01f;
					num14 += 0.01f;
				}
				else
				{
					num12 = API.GetSafeZoneSize() - 10f / MenuController.ScreenWidth;
					num14 = 0f;
					num13 = 0f;
					num13 += 0.005f;
					num12 += 0.005f;
					num14 += 0.005f;
				}
				BetterDrawTextHelper.SetTextWrap(num14, num12);
				BetterDrawTextHelper.SetTextJustification(TextJustification.Right);
				BetterDrawTextHelper.SetTextFont((Font)textFont2);
				BetterDrawTextHelper.SetTextScale(num11);
				BetterDrawTextHelper.EndTextCommandDisplayText(num13, y3);
			}
			if (!string.IsNullOrEmpty(MenuSubtitle) || CounterPreText != null || MaxItemsOnScreen < Size)
			{
				MenuItemsYOffset += num5 - 1f;
			}
			if (Size > 0)
			{
				API.SetScriptGfxAlign(LeftAligned ? 76 : 82, 84);
				API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
				float num15 = 38f * (float)MathUtil.Clamp(Size, 0, MaxItemsOnScreen);
				float num16 = (Position.X + headerSize.Key / 2f) / MenuController.ScreenWidth;
				float num17 = (Position.Y + MenuItemsYOffset + (num15 + 1f) / 2f) / MenuController.ScreenHeight;
				float num18 = headerSize.Key / MenuController.ScreenWidth;
				float num19 = (num15 + 1f) / MenuController.ScreenHeight;
				API.DrawRect(num16, num17, num18, num19, 0, 0, 0, 180);
				MenuItemsYOffset += num15 - 1f;
				menuYOffset += num19;
				API.ResetScriptGfxAlign();
			}
			if (Size > 0)
			{
				foreach (MenuItem visibleMenuItem in VisibleMenuItems)
				{
					await visibleMenuItem.Draw(ViewIndexOffset);
				}
			}
			num20 = 0f;
			if (Size > 0 && Size > MaxItemsOnScreen)
			{
				float num21 = 500f / MenuController.ScreenWidth;
				float num22 = 60f / MenuController.ScreenWidth;
				float num23 = (Position.X + 250f) / MenuController.ScreenWidth;
				float num24 = (Position.Y + MenuItemsYOffset) / MenuController.ScreenHeight + num22 / 2f + 6f / MenuController.ScreenHeight;
				API.SetScriptGfxAlign(LeftAligned ? 76 : 82, 84);
				API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
				API.DrawRect(num23, num24, num21, num22, 0, 0, 0, 180);
				num20 = num22;
				menuYOffset += num22;
				API.ResetScriptGfxAlign();
				API.SetScriptGfxAlign(76, 84);
				API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
				float num25 = 0f;
				float num26 = 500f / MenuController.ScreenWidth;
				float num27 = 250f / MenuController.ScreenWidth;
				float num28 = num24 - 20f / MenuController.ScreenHeight;
				float num29 = num24 - 10f / MenuController.ScreenHeight;
				API.BeginTextCommandDisplayText("STRING");
				API.AddTextComponentSubstringPlayerName("^");
				API.SetTextFont(0);
				API.SetTextScale(1f, 378f / MenuController.ScreenHeight);
				API.SetTextJustification(0);
				if (LeftAligned)
				{
					API.SetTextWrap(num25, num26);
					API.EndTextCommandDisplayText(num27, num28);
				}
				else
				{
					num25 = API.GetSafeZoneSize() - 490f / MenuController.ScreenWidth;
					num26 = API.GetSafeZoneSize() - 10f / MenuController.ScreenWidth;
					num27 = API.GetSafeZoneSize() - 250f / MenuController.ScreenWidth;
					API.SetTextWrap(num25, num26);
					API.EndTextCommandDisplayText(num27, num28);
				}
				API.BeginTextCommandDisplayText("STRING");
				API.AddTextComponentSubstringPlayerName("v");
				API.SetTextFont(0);
				API.SetTextScale(1f, 378f / MenuController.ScreenHeight);
				API.SetTextJustification(0);
				if (LeftAligned)
				{
					API.SetTextWrap(num25, num26);
					API.EndTextCommandDisplayText(num27, num29);
				}
				else
				{
					API.SetTextWrap(num25, num26);
					API.EndTextCommandDisplayText(num27, num29);
				}
				API.ResetScriptGfxAlign();
			}
			if (Size > 0)
			{
				MenuItem currentMenuItem = GetCurrentMenuItem();
				if (currentMenuItem != null && !string.IsNullOrEmpty(currentMenuItem.Description))
				{
					int textFont3 = (int)TextFont;
					float num30 = TextFontSize / MenuController.ScreenHeight;
					float num31 = 0f + 10f / MenuController.ScreenWidth;
					float num32 = 500f / MenuController.ScreenWidth - 10f / MenuController.ScreenWidth;
					float num33 = (Position.Y + MenuItemsYOffset) / MenuController.ScreenHeight + 16f / MenuController.ScreenHeight + num20;
					float textScaleHeight = API.GetTextScaleHeight(num30, textFont3);
					string description = currentMenuItem.Description;
					API.SetScriptGfxAlign(76, 84);
					API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
					API.BeginTextCommandLineCount("CELL_EMAIL_BCON");
					API.SetTextScale(num30, num30);
					API.SetTextJustification(1);
					API.SetTextFont(textFont3);
					string[] array = MenuUtils.SplitString(description);
					foreach (string obj in array)
					{
						StringBuilder stringBuilder = new StringBuilder();
						string text2 = obj;
						foreach (char c in text2)
						{
							if (c >= '一' && c <= 195103)
							{
								stringBuilder.Append("xx");
							}
							else if (c > 'ÿ')
							{
								stringBuilder.Append("x");
							}
							else
							{
								stringBuilder.Append(c);
							}
						}
						API.AddTextComponentSubstringPlayerName(stringBuilder.ToString());
					}
					API.SetTextWrap(num31, num32);
					int textScreenLineCount = API.GetTextScreenLineCount(num31, num33);
					API.ResetScriptGfxAlign();
					if (!LeftAligned)
					{
						num31 = API.GetSafeZoneSize() - 490f / MenuController.ScreenWidth;
						num32 = API.GetSafeZoneSize() - 10f / MenuController.ScreenWidth;
					}
					float num34 = num31;
					float num35 = num32;
					float num36 = num33;
					float num37 = num34 + 0.015f;
					num35 += 0.015f;
					num36 += 0.015f;
					BetterDrawTextHelper.BeginTextCommandDisplayText(description);
					BetterDrawTextHelper.SetTextScale(num30);
					BetterDrawTextHelper.SetTextWrap(num37, num35);
					BetterDrawTextHelper.EndTextCommandDisplayText(num37, num36);
					float num38 = 500f / MenuController.ScreenWidth;
					float num39 = (textScaleHeight + 0.005f) * (float)textScreenLineCount + 8f / MenuController.ScreenHeight + 2.5f / MenuController.ScreenHeight;
					float num40 = (Position.X + 250f) / MenuController.ScreenWidth;
					float num41 = num33 - 6f / MenuController.ScreenHeight + num39 / 2f;
					API.SetScriptGfxAlign(LeftAligned ? 76 : 82, 84);
					API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
					API.DrawRect(num40, num41 - num39 / 2f + 2f / MenuController.ScreenHeight, num38, 4f / MenuController.ScreenHeight, 0, 0, 0, 200);
					API.DrawRect(num40, num41, num38, num39, 0, 0, 0, 180);
					menuYOffset += num39;
					API.ResetScriptGfxAlign();
					num20 += num41 + num39 / 2f - 4f / MenuController.ScreenHeight;
				}
				else
				{
					num20 += MenuItemsYOffset / MenuController.ScreenHeight + 2f / MenuController.ScreenHeight + num20;
				}
			}
			if (Size > 0)
			{
				MenuItem currentMenuItem2 = GetCurrentMenuItem();
				if (currentMenuItem2 == null || (currentMenuItem2 is MenuListItem menuListItem && (menuListItem.ShowColorPanel || menuListItem.ShowOpacityPanel)))
				{
					goto IL_15c9;
				}
			}
			if (ShowWeaponStatsPanel || ShowVehicleStatsPanel)
			{
				float textScale = 378f / MenuController.ScreenHeight;
				float num42 = 500f / MenuController.ScreenWidth;
				float num43 = 140f / MenuController.ScreenHeight;
				float num44 = 250f / MenuController.ScreenWidth;
				float num45 = num20 + num43 / 2f + 8f / MenuController.ScreenHeight;
				if (Size > MaxItemsOnScreen)
				{
					num45 -= 30f / MenuController.ScreenHeight;
				}
				API.SetScriptGfxAlign(LeftAligned ? 76 : 82, 84);
				API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
				API.DrawRect(num44, num45, num42, num43, 0, 0, 0, 180);
				API.ResetScriptGfxAlign();
				float num46 = 250f / MenuController.ScreenWidth;
				float num47 = num44 + num46 / 2f - 10f / MenuController.ScreenWidth;
				if (!LeftAligned)
				{
					num47 = num44 - num46 / 2f - 10f / MenuController.ScreenWidth;
				}
				float num48 = num45 - num43 / 2f + 25f / MenuController.ScreenHeight;
				float num49 = 10f / MenuController.ScreenHeight;
				for (int k = 0; k < 4; k++)
				{
					int[] array2 = new int[3] { 93, 182, 229 };
					float num50 = num46 * (ShowWeaponStatsPanel ? WeaponStats[k] : VehicleStats[k]);
					float num51 = num46 * (ShowWeaponStatsPanel ? WeaponComponentStats[k] : VehicleUpgradeStats[k]);
					if (num51 < num50)
					{
						float num52 = num50 - num51;
						num50 -= num52;
						num51 += num52;
						array2 = new int[3] { 224, 50, 50 };
					}
					float num53;
					float num54;
					if (LeftAligned)
					{
						num53 = num47 - num46 / 2f + num50 / 2f;
						num54 = num47 - num46 / 2f + num51 / 2f;
					}
					else
					{
						num53 = num50 * 1.5f - num46 - 10f / MenuController.ScreenWidth;
						num54 = num51 * 1.5f - num46 - 10f / MenuController.ScreenWidth;
					}
					API.SetScriptGfxAlign(LeftAligned ? 76 : 82, 84);
					API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
					API.DrawRect(num47, num48, num46, num49, 100, 100, 100, 180);
					API.DrawRect(num54, num48, num51, num49, array2[0], array2[1], array2[2], 255);
					API.DrawRect(num53, num48, num50, num49, 255, 255, 255, 255);
					API.ResetScriptGfxAlign();
					num48 += 30f / MenuController.ScreenHeight;
				}
				menuYOffset += num43;
				float num55 = (LeftAligned ? (num44 - num42 / 2f + 10f / MenuController.ScreenWidth) : (API.GetSafeZoneSize() - 490f / MenuController.ScreenWidth));
				float num56 = num45 - num43 / 2f + 10f / MenuController.ScreenHeight;
				for (int l = 0; l < 4; l++)
				{
					BetterDrawTextHelper.SetScriptGfxAlign(GfxAlign.Left, GfxAlign.Top);
					BetterDrawTextHelper.BeginTextCommandDisplayText(ShowWeaponStatsPanel ? weaponStatNames[l] : API.GetLabelText(vehicleStatNames[l]));
					BetterDrawTextHelper.SetTextJustification(TextJustification.Left);
					BetterDrawTextHelper.SetTextScale(textScale);
					BetterDrawTextHelper.EndTextCommandDisplayText(num55 + 0.015f, num56 + 0.015f);
					num56 += 30f / MenuController.ScreenHeight;
				}
			}
			goto IL_15c9;
		}
		goto IL_197b;
		IL_15c9:
		if (Size > 0)
		{
			MenuItem currentMenuItem3 = GetCurrentMenuItem();
			if (currentMenuItem3 != null && currentMenuItem3 is MenuListItem menuListItem2)
			{
				if (menuListItem2.ShowOpacityPanel)
				{
					API.BeginScaleformMovieMethod(OpacityPanelScaleform, "SET_TITLE");
					API.PushScaleformMovieMethodParameterString("Opacity");
					API.PushScaleformMovieMethodParameterString("");
					API.ScaleformMovieMethodAddParamInt(menuListItem2.ListIndex * 10);
					API.EndScaleformMovieMethod();
					float num57 = 500f / MenuController.ScreenWidth;
					float num58 = 700f / MenuController.ScreenHeight;
					float num59 = 250f / MenuController.ScreenWidth;
					float num60 = num20 + num58 / 2f + 4f / MenuController.ScreenHeight;
					if (Size > MaxItemsOnScreen)
					{
						num60 -= 30f / MenuController.ScreenHeight;
					}
					API.SetScriptGfxAlign(LeftAligned ? 76 : 82, 84);
					API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
					API.DrawScaleformMovie(OpacityPanelScaleform, num59, num60, num57, num58, 255, 255, 255, 255, 0);
					API.ResetScriptGfxAlign();
					menuYOffset += num58;
				}
				else if (menuListItem2.ShowColorPanel)
				{
					API.BeginScaleformMovieMethod(ColorPanelScaleform, "SET_TITLE");
					API.PushScaleformMovieMethodParameterString("Opacity");
					API.BeginTextCommandScaleformString("FACE_COLOUR");
					API.AddTextComponentInteger(menuListItem2.ListIndex + 1);
					API.AddTextComponentInteger(menuListItem2.ItemsCount);
					API.EndTextCommandScaleformString();
					API.ScaleformMovieMethodAddParamInt(0);
					API.ScaleformMovieMethodAddParamBool(true);
					API.EndScaleformMovieMethod();
					API.BeginScaleformMovieMethod(ColorPanelScaleform, "SET_DATA_SLOT_EMPTY");
					API.EndScaleformMovieMethod();
					for (int m = 0; m < 64; m++)
					{
						int num61 = 0;
						int num62 = 0;
						int num63 = 0;
						if (menuListItem2.ColorPanelColorType == MenuListItem.ColorPanelType.Hair)
						{
							API.GetHairRgbColor(m, ref num61, ref num62, ref num63);
						}
						else
						{
							API.GetMakeupRgbColor(m, ref num61, ref num62, ref num63);
						}
						API.BeginScaleformMovieMethod(ColorPanelScaleform, "SET_DATA_SLOT");
						API.ScaleformMovieMethodAddParamInt(m);
						API.ScaleformMovieMethodAddParamInt(num61);
						API.ScaleformMovieMethodAddParamInt(num62);
						API.ScaleformMovieMethodAddParamInt(num63);
						API.EndScaleformMovieMethod();
					}
					API.BeginScaleformMovieMethod(ColorPanelScaleform, "DISPLAY_VIEW");
					API.EndScaleformMovieMethod();
					API.BeginScaleformMovieMethod(ColorPanelScaleform, "SET_HIGHLIGHT");
					API.ScaleformMovieMethodAddParamInt(menuListItem2.ListIndex);
					API.EndScaleformMovieMethod();
					API.BeginScaleformMovieMethod(ColorPanelScaleform, "SHOW_OPACITY");
					API.ScaleformMovieMethodAddParamBool(false);
					API.ScaleformMovieMethodAddParamBool(true);
					API.EndScaleformMovieMethod();
					float num64 = 500f / MenuController.ScreenWidth;
					float num65 = 700f / MenuController.ScreenHeight;
					float num66 = 250f / MenuController.ScreenWidth;
					float num67 = num20 + num65 / 2f + 4f / MenuController.ScreenHeight;
					if (Size > MaxItemsOnScreen)
					{
						num67 -= 30f / MenuController.ScreenHeight;
					}
					API.SetScriptGfxAlign(LeftAligned ? 76 : 82, 84);
					API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
					API.DrawScaleformMovie(ColorPanelScaleform, num66, num67, num64, num65, 255, 255, 255, 255, 0);
					API.ResetScriptGfxAlign();
					menuYOffset += num65;
				}
			}
		}
		if (MenuController.SetDrawOrder)
		{
			API.SetScriptGfxDrawOrder(0);
		}
		previousMenuYOffset = menuYOffset;
		goto IL_197b;
		IL_197b:
		await Task.FromResult(0);
	}
}
