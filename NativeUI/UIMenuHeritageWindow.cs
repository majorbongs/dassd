using System.Drawing;
using CitizenFX.Core.Native;

namespace NativeUI;

public class UIMenuHeritageWindow : UIMenuWindow
{
	private Sprite MomSprite;

	private Sprite DadSprite;

	private int Mom;

	private int Dad;

	public PointF Offset;

	public UIMenuHeritageWindow(int mom, int dad)
	{
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		Background = new Sprite("pause_menu_pages_char_mom_dad", "mumdadbg", new Point(0, 0), new Size(431, 228));
		Mom = mom;
		Dad = dad;
		MomSprite = new Sprite("char_creator_portraits", (Mom < 21) ? ("female_" + Mom) : ("special_female_" + (Mom - 21)), new Point(0, 0), new Size(228, 228));
		DadSprite = new Sprite("char_creator_portraits", (Dad < 21) ? ("Male_" + Dad) : ("special_male_" + (Dad - 21)), new Point(0, 0), new Size(228, 228));
		MomSprite.Visible = API.GetTextureResolution("char_creator_portraits", MomSprite.TextureName).X > 127f;
		DadSprite.Visible = API.GetTextureResolution("char_creator_portraits", DadSprite.TextureName).X > 127f;
		Offset = new PointF(0f, 0f);
	}

	internal override void Position(float y)
	{
		Background.Position = new PointF(Offset.X, 70f + y + base.ParentMenu.Offset.Y);
		MomSprite.Position = new PointF(Offset.X + (float)(base.ParentMenu.WidthOffset / 2) + 25f, 70f + y + base.ParentMenu.Offset.Y);
		DadSprite.Position = new PointF(Offset.X + (float)(base.ParentMenu.WidthOffset / 2) + 195f, 70f + y + base.ParentMenu.Offset.Y);
	}

	public void Index(int mom, int dad)
	{
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		Mom = mom;
		Dad = dad;
		MomSprite.TextureName = ((Mom < 21) ? ("female_" + Mom) : ("special_female_" + (Mom - 21)));
		DadSprite.TextureName = ((Dad < 21) ? ("male_" + Dad) : ("special_male_" + (Dad - 21)));
		MomSprite.Visible = API.GetTextureResolution("char_creator_portraits", MomSprite.TextureName).X > 127f;
		DadSprite.Visible = API.GetTextureResolution("char_creator_portraits", DadSprite.TextureName).X > 127f;
	}

	internal override void Draw()
	{
		Background.Size = new Size(431 + base.ParentMenu.WidthOffset, 228);
		Background.Draw();
		DadSprite.Draw();
		MomSprite.Draw();
	}
}
