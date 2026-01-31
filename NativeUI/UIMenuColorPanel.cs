using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

namespace NativeUI;

public class UIMenuColorPanel : UIMenuPanel
{
	public enum ColorPanelType
	{
		Hair,
		Makeup
	}

	private ColorPanelData Data = new ColorPanelData();

	private List<UIResRectangle> Bar = new List<UIResRectangle>();

	private bool EnableArrow;

	private Sprite LeftArrow;

	private Sprite RightArrow;

	private UIResRectangle SelectedRectangle;

	private UIResText Text;

	private List<Color> Colors = new List<Color>();

	private int r;

	private int g;

	private int b;

	public ColorPanelType ColorPanelColorType;

	public int CurrentSelection
	{
		get
		{
			if (Data.Items.Count == 0)
			{
				return 0;
			}
			if (Data.Index % Data.Items.Count == 0)
			{
				return 0;
			}
			return Data.Index % Data.Items.Count;
		}
		set
		{
			if (Data.Items.Count == 0)
			{
				Data.Index = 0;
			}
			Data.Index = 1000000 - 1000000 % Data.Items.Count + value;
			if (CurrentSelection > Data.Pagination.Max)
			{
				Data.Pagination.Min = CurrentSelection - (Data.Pagination.Total + 1);
				Data.Pagination.Max = CurrentSelection;
			}
			else if (CurrentSelection < Data.Pagination.Min)
			{
				Data.Pagination.Min = CurrentSelection - 1;
				Data.Pagination.Max = CurrentSelection + Data.Pagination.Total + 1;
			}
			UpdateSelection(update: false);
		}
	}

	public UIMenuColorPanel(string Title, ColorPanelType ColorType)
	{
		switch (ColorType)
		{
		case ColorPanelType.Hair:
		{
			Colors.Clear();
			for (int j = 0; j < 64; j++)
			{
				API.GetHairRgbColor(j, ref r, ref g, ref b);
				Colors.Add(Color.FromArgb(r, g, b));
			}
			break;
		}
		case ColorPanelType.Makeup:
		{
			Colors.Clear();
			for (int i = 0; i < 64; i++)
			{
				API.GetMakeupRgbColor(i, ref r, ref g, ref b);
				Colors.Add(Color.FromArgb(r, g, b));
			}
			break;
		}
		}
		Data.Pagination.Min = 0;
		Data.Pagination.Max = 7;
		Data.Pagination.Total = 7;
		Data.Index = 1000;
		Data.Items = Colors;
		Data.Title = Title ?? "Title";
		Enabled = true;
		Data.Value = 1;
		Background = new Sprite("commonmenu", "gradient_bgd", new Point(0, 0), new Size(431, 122));
		EnableArrow = true;
		LeftArrow = new Sprite("commonmenu", "arrowleft", new Point(0, 0), new Size(30, 30));
		RightArrow = new Sprite("commonmenu", "arrowright", new Point(0, 0), new Size(30, 30));
		SelectedRectangle = new UIResRectangle(new Point(0, 0), new Size(44, 8), Color.FromArgb(255, 255, 255));
		Text = new UIResText(Title + " [1 / " + Colors.Count + "]", new Point(0, 0), 0.35f, Color.FromArgb(255, 255, 255, 255), (Font)0, (Alignment)0);
		base.ParentItem = null;
		for (int k = 0; k < Colors.Count && k < 9; k++)
		{
			Bar.Add(new UIResRectangle(new PointF(0f, 0f), new SizeF(44.5f, 44.5f), Colors[k]));
		}
		if (Data.Items.Count != 0)
		{
			Data.Index = 1000 - 1000 % Data.Items.Count;
			Data.Pagination.Max = Data.Pagination.Total + 1;
			Data.Pagination.Min = 0;
		}
	}

	internal override void Position(float y)
	{
		float x = base.ParentItem.Offset.X;
		float num = base.ParentItem.Parent.WidthOffset;
		Background.Position = new PointF(x, y);
		for (int i = 0; i < Bar.Count; i++)
		{
			((Rectangle)Bar[i]).Position = new PointF(15f + 44.5f * (float)i + x + num / 2f, 55f + y);
		}
		((Rectangle)SelectedRectangle).Position = new PointF(15f + 44.5f * (float)(CurrentSelection - Data.Pagination.Min) + x + num / 2f, 47f + y);
		if (EnableArrow)
		{
			LeftArrow.Position = new PointF(7.5f + x + num / 2f, 15f + y);
			RightArrow.Position = new PointF(393.5f + x + num / 2f, 15f + y);
		}
		((Text)Text).Position = new PointF(215.5f + x + num / 2f, 15f + y);
	}

	private void UpdateSelection(bool update)
	{
		if (update)
		{
			base.ParentItem.Parent.ListChange(base.ParentItem, base.ParentItem.Index);
			base.ParentItem.ListChangedTrigger(base.ParentItem.Index);
		}
		((Rectangle)SelectedRectangle).Position = new PointF(15f + 44.5f * (float)(CurrentSelection - Data.Pagination.Min) + base.ParentItem.Offset.X, ((Rectangle)SelectedRectangle).Position.Y);
		for (int i = 0; i < 9; i++)
		{
			((Rectangle)Bar[i]).Color = Data.Items[Data.Pagination.Min + i];
		}
		((Text)Text).Caption = Data.Title + " [" + (CurrentSelection + 1) + " / " + Data.Items.Count + "]";
	}

	private void Functions()
	{
		Point safezoneBounds = ScreenTools.SafezoneBounds;
		if (ScreenTools.IsMouseInBounds(new PointF(LeftArrow.Position.X + (float)safezoneBounds.X, LeftArrow.Position.Y + (float)safezoneBounds.Y), LeftArrow.Size) && (API.IsDisabledControlJustPressed(0, 24) || API.IsControlJustPressed(0, 24)))
		{
			GoLeft();
		}
		if (ScreenTools.IsMouseInBounds(new PointF(RightArrow.Position.X + (float)safezoneBounds.X, RightArrow.Position.Y + (float)safezoneBounds.Y), RightArrow.Size) && (API.IsDisabledControlJustPressed(0, 24) || API.IsControlJustPressed(0, 24)))
		{
			GoRight();
		}
		for (int i = 0; i < Bar.Count; i++)
		{
			if (ScreenTools.IsMouseInBounds(new PointF(((Rectangle)Bar[i]).Position.X + (float)safezoneBounds.X, ((Rectangle)Bar[i]).Position.Y + (float)safezoneBounds.Y), ((Rectangle)Bar[i]).Size) && (API.IsDisabledControlJustPressed(0, 24) || API.IsControlJustPressed(0, 24)))
			{
				CurrentSelection = Data.Pagination.Min + i;
				UpdateSelection(update: true);
			}
		}
	}

	private void GoLeft()
	{
		if (Data.Items.Count > Data.Pagination.Total + 1)
		{
			if (CurrentSelection <= Data.Pagination.Min)
			{
				if (CurrentSelection == 0)
				{
					Data.Pagination.Min = Data.Items.Count - (Data.Pagination.Total + 1) - 1;
					Data.Pagination.Max = Data.Items.Count - 1;
					Data.Index = 1000 - 1000 % Data.Items.Count;
					Data.Index += Data.Items.Count - 1;
					UpdateSelection(update: true);
				}
				else
				{
					Data.Pagination.Min--;
					Data.Pagination.Max--;
					Data.Index--;
					UpdateSelection(update: true);
				}
			}
			else
			{
				Data.Index--;
				UpdateSelection(update: true);
			}
		}
		else
		{
			Data.Index--;
			UpdateSelection(update: true);
		}
	}

	private void GoRight()
	{
		if (Data.Items.Count > Data.Pagination.Total + 1)
		{
			if (CurrentSelection >= Data.Pagination.Max)
			{
				if (CurrentSelection == Data.Items.Count - 1)
				{
					Data.Pagination.Min = 0;
					Data.Pagination.Max = Data.Pagination.Total + 1;
					Data.Index = 1000 - 1000 % Data.Items.Count;
					UpdateSelection(update: true);
				}
				else
				{
					Data.Pagination.Max++;
					Data.Pagination.Min = Data.Pagination.Max - (Data.Pagination.Total + 1);
					Data.Index++;
					UpdateSelection(update: true);
				}
			}
			else
			{
				Data.Index++;
				UpdateSelection(update: true);
			}
		}
		else
		{
			Data.Index++;
			UpdateSelection(update: true);
		}
	}

	internal override async Task Draw()
	{
		if (Enabled)
		{
			Background.Size = new Size(431 + base.ParentItem.Parent.WidthOffset, 112);
			Background.Draw();
			if (EnableArrow)
			{
				LeftArrow.Draw();
				RightArrow.Draw();
			}
			((Text)Text).Draw();
			for (int i = 0; i < Bar.Count; i++)
			{
				((Rectangle)Bar[i]).Draw();
			}
			((Rectangle)SelectedRectangle).Draw();
			Functions();
		}
		await Task.FromResult(0);
	}
}
