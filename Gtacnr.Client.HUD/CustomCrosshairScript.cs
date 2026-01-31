using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Localization;
using MenuAPI;

namespace Gtacnr.Client.HUD;

public class CustomCrosshairScript : Script
{
	private class Crosshair
	{
		public bool DrawDot { get; set; } = true;

		public bool DrawOutline { get; set; } = true;

		public bool DrawT { get; set; }

		public int PresetColor { get; set; }

		public Color Color { get; set; } = Color.FromHexString("ffffffff");

		public float Length { get; set; } = 1.8f;

		public float Gap { get; set; } = -1f;

		public float Thickness { get; set; } = 0.6f;

		public float OutlineThickness { get; set; } = 0.3f;
	}

	private static Menu menu;

	private static Dictionary<string, MenuItem> mItems = new Dictionary<string, MenuItem>();

	private bool crosshairEnabled;

	private Crosshair currentCrosshair;

	private static CustomCrosshairScript instance;

	public CustomCrosshairScript()
	{
		instance = this;
		crosshairEnabled = Utils.GetPreference("gtacnr:crosshairEnabled", defaultValue: false);
		string preference = Utils.GetPreference<string>("gtacnr:crosshair");
		if (string.IsNullOrEmpty(preference))
		{
			currentCrosshair = new Crosshair();
		}
		else
		{
			currentCrosshair = preference.Unjson<Crosshair>();
		}
		menu = new Menu("Crosshair", "Customize your crosshair");
		menu.OnItemSelect += OnItemSelect;
		menu.OnListIndexChange += OnListIndexChange;
		menu.OnSliderItemSelect += OnSliderItemSelect;
		menu.OnCheckboxChange += OnCheckboxChange;
		menu.OnSliderPositionChange += OnSliderPositionChange;
		MenuController.AddMenu(menu);
		menu.AddMenuItem(mItems["enable"] = new MenuCheckboxItem("Enable", "Enable or disable the custom crosshair. Requires FiveM to be in ~r~beta mode~s~.")
		{
			Checked = crosshairEnabled
		});
		menu.AddMenuItem(Utils.GetSpacerMenuItem("\u02c5 VISIBILITY \u02c5"));
		menu.AddMenuItem(mItems["drawDot"] = new MenuCheckboxItem("Dot", "Choose whether the center dot is visible."));
		menu.AddMenuItem(mItems["drawOutline"] = new MenuCheckboxItem("Outline", "Choose whether the outline is visible."));
		menu.AddMenuItem(mItems["drawT"] = new MenuCheckboxItem("T-shaped", "Choose whether the top line is hidden."));
		menu.AddMenuItem(Utils.GetSpacerMenuItem("\u02c5 COLOR \u02c5"));
		menu.AddMenuItem(mItems["color"] = new MenuListItem("Color", new string[6] { "Red", "Green", "Yellow", "Purple", "Cyan", "Custom" })
		{
			Description = "Select a preset color"
		});
		menu.AddMenuItem(mItems["customColor"] = new MenuItem("Custom color", "Input a custom color in the hex format. You can Google ''color picker''.\n~y~Example: ~s~#20b04d"));
		menu.AddMenuItem(mItems["alpha"] = new MenuSliderItem("Transparency", 0, 30, 0)
		{
			Description = "Change the crosshair's transparency."
		});
		menu.AddMenuItem(Utils.GetSpacerMenuItem("\u02c5 GEOMETRY \u02c5"));
		menu.AddMenuItem(mItems["length"] = new MenuSliderItem("Length", 0, 30, 0)
		{
			Description = "Change the length of the crosshair lines. Press enter to enter a value manually."
		});
		menu.AddMenuItem(mItems["gap"] = new MenuSliderItem("Gap", 0, 30, 0)
		{
			Description = "Change the size of the gap between the center and the lines. Press enter to enter a value manually."
		});
		menu.AddMenuItem(mItems["thickness"] = new MenuSliderItem("Thickness", 0, 30, 0)
		{
			Description = "Change the thickness of the crosshair lines. Press enter to enter a value manually."
		});
		menu.AddMenuItem(mItems["outThickness"] = new MenuSliderItem("Outline thickness", 0, 30, 0)
		{
			Description = "Change the thickness of the crosshair outline. Press enter to enter a value manually."
		});
		menu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)37, Menu.ControlPressCheckType.JUST_PRESSED, OnImport, disableControl: true));
		menu.InstructionalButtons[(Control)37] = "Import";
		RefreshEnabledState();
		RefreshValues();
		Chat.AddSuggestion("/crosshair", "Customize your crosshair. Requires FiveM to be in beta or developer mode.");
	}

	private void RefreshEnabledState()
	{
		mItems["drawDot"].Enabled = crosshairEnabled;
		mItems["drawOutline"].Enabled = crosshairEnabled;
		mItems["drawT"].Enabled = crosshairEnabled;
		mItems["color"].Enabled = crosshairEnabled;
		mItems["customColor"].Enabled = crosshairEnabled && crosshairEnabled && currentCrosshair.PresetColor == 5;
		mItems["alpha"].Enabled = crosshairEnabled && crosshairEnabled && currentCrosshair.PresetColor == 5;
		mItems["length"].Enabled = crosshairEnabled;
		mItems["gap"].Enabled = crosshairEnabled;
		mItems["thickness"].Enabled = crosshairEnabled;
		mItems["outThickness"].Enabled = crosshairEnabled;
	}

	private void RefreshValues()
	{
		((MenuCheckboxItem)mItems["drawDot"]).Checked = currentCrosshair.DrawDot;
		((MenuCheckboxItem)mItems["drawOutline"]).Checked = currentCrosshair.DrawOutline;
		((MenuCheckboxItem)mItems["drawT"]).Checked = currentCrosshair.DrawT;
		((MenuListItem)mItems["color"]).ListIndex = currentCrosshair.PresetColor;
		mItems["customColor"].Label = currentCrosshair.Color.ToHexString(alpha: false);
		((MenuSliderItem)mItems["alpha"]).Position = Gtacnr.Utils.ConvertRange((int)currentCrosshair.Color.A, 0f, 255f, 0f, 30f).ToInt();
		((MenuSliderItem)mItems["length"]).Position = currentCrosshair.Length.ConvertRange(0f, 10f, 0f, 30f).ToInt();
		((MenuSliderItem)mItems["gap"]).Position = currentCrosshair.Gap.ConvertRange(-5f, 20f, 0f, 30f).ToInt();
		((MenuSliderItem)mItems["thickness"]).Position = currentCrosshair.Thickness.ConvertRange(0f, 3f, 0f, 30f).ToInt();
		((MenuSliderItem)mItems["outThickness"]).Position = currentCrosshair.OutlineThickness.ConvertRange(0f, 6f, 0f, 30f).ToInt();
		mItems["alpha"].Description = $"{currentCrosshair.Color.A}";
		mItems["length"].Description = $"{currentCrosshair.Length}";
		mItems["gap"].Description = $"{currentCrosshair.Gap}";
		mItems["thickness"].Description = $"{currentCrosshair.Thickness}";
		mItems["outThickness"].Description = $"{currentCrosshair.OutlineThickness}";
	}

	private void OnCheckboxChange(Menu menu, MenuCheckboxItem menuItem, int itemIndex, bool check)
	{
		if (menuItem == mItems["enable"])
		{
			crosshairEnabled = check;
		}
		else if (menuItem == mItems["drawDot"])
		{
			currentCrosshair.DrawDot = check;
		}
		else if (menuItem == mItems["drawOutline"])
		{
			currentCrosshair.DrawOutline = check;
		}
		else if (menuItem == mItems["drawT"])
		{
			currentCrosshair.DrawT = check;
		}
		RefreshEnabledState();
		RefreshValues();
		ApplyCrosshair();
	}

	private void OnListIndexChange(Menu menu, MenuListItem menuItem, int oldSelectionIndex, int newSelectionIndex, int itemIndex)
	{
		if (menuItem == mItems["color"])
		{
			currentCrosshair.PresetColor = newSelectionIndex;
		}
		RefreshEnabledState();
		RefreshValues();
		ApplyCrosshair();
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == mItems["customColor"])
		{
			string text = await Utils.GetUserInput("Custom Color", "Input a color in RGB hex format", "#ffffff", 7);
			if (text == null || string.IsNullOrWhiteSpace(text))
			{
				Utils.PlayErrorSound();
				return;
			}
			try
			{
				text = text.TrimStart('#').Trim();
				Color color = Color.FromHexString(text + "ff");
				currentCrosshair.Color = new Color(color.R, color.G, color.B, currentCrosshair.Color.A);
			}
			catch
			{
				Utils.PlayErrorSound();
				return;
			}
		}
		RefreshEnabledState();
		RefreshValues();
		ApplyCrosshair();
	}

	private void OnSliderPositionChange(Menu menu, MenuSliderItem menuItem, int oldPosition, int newPosition, int itemIndex)
	{
		if (menuItem == mItems["alpha"])
		{
			byte a = Convert.ToByte(Gtacnr.Utils.ConvertRange(newPosition, 0f, 30f, 0f, 255f));
			currentCrosshair.Color = new Color(currentCrosshair.Color.R, currentCrosshair.Color.G, currentCrosshair.Color.B, a);
		}
		else if (menuItem == mItems["length"])
		{
			currentCrosshair.Length = Gtacnr.Utils.ConvertRange(newPosition, 0f, 30f, 0f, 10f);
		}
		else if (menuItem == mItems["gap"])
		{
			currentCrosshair.Gap = Gtacnr.Utils.ConvertRange(newPosition, 0f, 30f, -5f, 20f);
		}
		else if (menuItem == mItems["thickness"])
		{
			currentCrosshair.Thickness = Gtacnr.Utils.ConvertRange(newPosition, 0f, 30f, 0f, 3f);
		}
		else if (menuItem == mItems["outThickness"])
		{
			currentCrosshair.OutlineThickness = Gtacnr.Utils.ConvertRange(newPosition, 0f, 30f, 0f, 6f);
		}
		RefreshEnabledState();
		RefreshValues();
		ApplyCrosshair();
	}

	private async void OnSliderItemSelect(Menu menu, MenuSliderItem menuItem, int sliderPosition, int itemIndex)
	{
		string text = await Utils.GetUserInput("Custom value", "Enter the desired value", "", 8, "number");
		if (string.IsNullOrEmpty(text) || !float.TryParse(text, out var result))
		{
			Utils.PlayErrorSound();
			return;
		}
		if (menuItem == mItems["alpha"])
		{
			byte a = Convert.ToByte(result);
			currentCrosshair.Color = new Color(currentCrosshair.Color.R, currentCrosshair.Color.G, currentCrosshair.Color.B, a);
		}
		else if (menuItem == mItems["length"])
		{
			currentCrosshair.Length = result;
		}
		else if (menuItem == mItems["gap"])
		{
			currentCrosshair.Gap = result;
		}
		else if (menuItem == mItems["thickness"])
		{
			currentCrosshair.Thickness = result;
		}
		else if (menuItem == mItems["outThickness"])
		{
			currentCrosshair.OutlineThickness = result;
		}
		RefreshEnabledState();
		RefreshValues();
		ApplyCrosshair();
	}

	public static void OpenMenu(Menu parent = null)
	{
		menu.ParentMenu = parent;
		menu.OpenMenu();
		instance.ApplyCrosshair();
	}

	[Command("crosshair")]
	private void CrosshairCommand()
	{
		OpenMenu();
	}

	private void ApplyCrosshair()
	{
		string content = (crosshairEnabled ? ("profile_reticuleSize -10; cl_customCrosshair True; cl_crosshairstyle 3; " + $"cl_crosshairdot {currentCrosshair.DrawDot}; " + $"cl_crosshair_drawoutline {currentCrosshair.DrawOutline}; " + $"cl_crosshair_t {currentCrosshair.DrawT}; " + "cl_crosshairusealpha True; " + $"cl_crosshaircolor {currentCrosshair.PresetColor}; " + $"cl_crosshaircolor_r {currentCrosshair.Color.R}; " + $"cl_crosshaircolor_g {currentCrosshair.Color.G}; " + $"cl_crosshaircolor_b {currentCrosshair.Color.B}; " + $"cl_crosshairalpha {currentCrosshair.Color.A}; " + $"cl_crosshairsize {currentCrosshair.Length}; " + $"cl_crosshairgap {currentCrosshair.Gap}; " + $"cl_crosshairthickness {currentCrosshair.Thickness}; " + $"cl_crosshair_outlinethickness {currentCrosshair.OutlineThickness};") : "cl_customCrosshair false; profile_reticuleSize -2;");
		Utils.SetPreference("gtacnr:crosshairEnabled", crosshairEnabled);
		Utils.SetPreference("gtacnr:crosshair", currentCrosshair.Json());
		API.SendNuiMessage(new
		{
			method = "copyText",
			content = content
		}.Json());
		Utils.SendNotification("The ~p~crosshair code ~s~has been copied to your ~g~clipboard~s~; press F8, CTRL+V and enter to apply it. You can also share it with your friends.\nUnfortunately, due to ~r~FiveM restrictions~s~, we can't apply your crosshair automatically.");
	}

	private async void OnImport(Menu menu, Control control)
	{
		Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_IMPLEMENTED));
	}
}
