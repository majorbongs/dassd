using System.Drawing;
using System.Threading.Tasks;

namespace NativeUI;

public class UIMenuPanel
{
	internal dynamic Background;

	protected SizeF Resolution = ScreenTools.ResolutionMaintainRatio;

	public virtual bool Selected { get; set; }

	public virtual bool Enabled { get; set; }

	public UIMenuListItem ParentItem { get; set; }

	internal virtual void Position(float y)
	{
	}

	public virtual void UpdateParent()
	{
		ParentItem.Parent.ListChange(ParentItem, ParentItem.Index);
		ParentItem.ListChangedTrigger(ParentItem.Index);
	}

	internal virtual async Task Draw()
	{
	}

	public void SetParentItem(UIMenuListItem item)
	{
		ParentItem = item;
	}
}
