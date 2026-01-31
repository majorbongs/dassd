using System;
using System.Collections.Generic;
using System.Drawing;
using CitizenFX.Core;
using CitizenFX.Core.UI;

namespace NativeUI.PauseMenu;

public class TabMissionSelectItem : TabItem
{
	protected internal float _add;

	protected const int MaxItemsPerView = 15;

	protected int _minItem;

	protected int _maxItem;

	public List<MissionInformation> Heists { get; set; }

	public int Index { get; set; }

	protected Sprite _noLogo { get; set; }

	public event OnItemSelect OnItemSelect;

	public TabMissionSelectItem(string name, IEnumerable<MissionInformation> list)
		: base(name)
	{
		base.FadeInWhenFocused = true;
		DrawBg = false;
		_noLogo = new Sprite("gtav_online", "rockstarlogo256", default(PointF), new SizeF(512f, 256f));
		_maxItem = 15;
		_minItem = 0;
		base.CanBeFocused = true;
		Heists = new List<MissionInformation>(list);
	}

	public override void ProcessControls()
	{
		if (!Focused || Heists.Count == 0)
		{
			return;
		}
		if (base.JustOpened)
		{
			base.JustOpened = false;
			return;
		}
		if (Game.IsControlJustPressed(0, (Control)176))
		{
			Game.PlaySound("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
			this.OnItemSelect?.Invoke(Heists[Index]);
		}
		if (Game.IsControlJustPressed(0, (Control)188) || Game.IsControlJustPressed(0, (Control)32))
		{
			Index = (1000 - 1000 % Heists.Count + Index - 1) % Heists.Count;
			Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
			if (Heists.Count > 15)
			{
				if (Index < _minItem)
				{
					_minItem--;
					_maxItem--;
				}
				if (Index == Heists.Count - 1)
				{
					_minItem = Heists.Count - 15;
					_maxItem = Heists.Count;
				}
			}
		}
		else
		{
			if (!Game.IsControlJustPressed(0, (Control)187) && !Game.IsControlJustPressed(0, (Control)33))
			{
				return;
			}
			Index = (1000 - 1000 % Heists.Count + Index + 1) % Heists.Count;
			Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
			if (Heists.Count > 15)
			{
				if (Index >= _maxItem)
				{
					_maxItem++;
					_minItem++;
				}
				if (Index == 0)
				{
					_minItem = 0;
					_maxItem = 15;
				}
			}
		}
	}

	public override void Draw()
	{
		base.Draw();
		if (Heists.Count != 0)
		{
			int alpha = (Focused ? 120 : 30);
			int alpha2 = (Focused ? 200 : 100);
			int alpha3 = (Focused ? 255 : 150);
			float num = Resolution.Width - base.SafeSize.X * 2f;
			SizeF size = new SizeF((float)(int)num - (_add + 515f), 40f);
			int num2 = 0;
			for (int i = _minItem; i < Math.Min(Heists.Count, _maxItem); i++)
			{
				((Rectangle)new UIResRectangle(base.SafeSize.AddPoints(new PointF(0f, 43 * num2)), size, (Index == i && Focused) ? Color.FromArgb(alpha3, Colors.White) : Color.FromArgb(alpha2, Colors.Black))).Draw();
				((Text)new UIResText(Heists[i].Name, base.SafeSize.AddPoints(new PointF(6f, 5 + 43 * num2)), 0.35f, Color.FromArgb(alpha3, (Index == i && Focused) ? Colors.Black : Colors.White))).Draw();
				num2++;
			}
			if (Heists[Index].Logo == null || string.IsNullOrEmpty(Heists[Index].Logo.FileName))
			{
				_noLogo.Position = new PointF((float)(int)Resolution.Width - base.SafeSize.X - (512f + _add), base.SafeSize.Y);
				_noLogo.Color = Color.FromArgb(alpha2, 0, 0, 0);
				_noLogo.Draw();
			}
			else if ((Heists[Index].Logo == null || Heists[Index].Logo.FileName == null || Heists[Index].Logo.IsGameTexture) && Heists[Index].Logo != null && Heists[Index].Logo.FileName != null && Heists[Index].Logo.IsGameTexture)
			{
				Sprite sprite = new Sprite(Heists[Index].Logo.DictionaryName, Heists[Index].Logo.FileName, new PointF((float)(int)Resolution.Width - base.SafeSize.X - (512f + _add), base.SafeSize.Y), new SizeF(512f, 256f));
				sprite.Color = Color.FromArgb(alpha2, 0, 0, 0);
				sprite.Draw();
			}
			((Rectangle)new UIResRectangle(new PointF((float)(int)Resolution.Width - base.SafeSize.X - (512f + _add), base.SafeSize.Y + 256f), new SizeF(512f, 40f), Color.FromArgb(alpha3, Colors.Black))).Draw();
			((Text)new UIResText(Heists[Index].Name, new PointF((float)(int)Resolution.Width - base.SafeSize.X - (4f + _add), base.SafeSize.Y + 260f), 0.5f, Color.FromArgb(alpha3, Colors.White), (Font)1, (Alignment)2)).Draw();
			for (int j = 0; j < Heists[Index].ValueList.Count; j++)
			{
				((Rectangle)new UIResRectangle(new PointF((float)(int)Resolution.Width - base.SafeSize.X - (512f + _add), base.SafeSize.Y + 256f + 40f + (float)(40 * j)), new SizeF(512f, 40f), (j % 2 == 0) ? Color.FromArgb(alpha, 0, 0, 0) : Color.FromArgb(alpha2, 0, 0, 0))).Draw();
				string item = Heists[Index].ValueList[j].Item1;
				string item2 = Heists[Index].ValueList[j].Item2;
				((Text)new UIResText(item, new PointF((float)(int)Resolution.Width - base.SafeSize.X - (506f + _add), base.SafeSize.Y + 260f + 42f + (float)(40 * j)), 0.35f, Color.FromArgb(alpha3, Colors.White))).Draw();
				((Text)new UIResText(item2, new PointF((float)(int)Resolution.Width - base.SafeSize.X - (6f + _add), base.SafeSize.Y + 260f + 42f + (float)(40 * j)), 0.35f, Color.FromArgb(alpha3, Colors.White), (Font)0, (Alignment)2)).Draw();
			}
			if (!string.IsNullOrEmpty(Heists[Index].Description))
			{
				int count = Heists[Index].ValueList.Count;
				((Rectangle)new UIResRectangle(new PointF((float)(int)Resolution.Width - base.SafeSize.X - (512f + _add), base.SafeSize.Y + 256f + 42f + (float)(40 * count)), new SizeF(512f, 2f), Color.FromArgb(alpha3, Colors.White))).Draw();
				((Text)new UIResText(Heists[Index].Description, new PointF((float)(int)Resolution.Width - base.SafeSize.X - (508f + _add), base.SafeSize.Y + 256f + 45f + (float)(40 * count) + 4f), 0.35f, Color.FromArgb(alpha3, Colors.White))
				{
					Wrap = 508f + _add
				}).Draw();
				((Rectangle)new UIResRectangle(new PointF((float)(int)Resolution.Width - base.SafeSize.X - (512f + _add), base.SafeSize.Y + 256f + 44f + (float)(40 * count)), new SizeF(512f, 45 * (int)(ScreenTools.GetTextWidth(Heists[Index].Description, (Font)0, 0.35f) / 500f)), Color.FromArgb(alpha2, 0, 0, 0))).Draw();
			}
		}
	}
}
