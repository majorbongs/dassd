using Gtacnr.Localization;
using MenuAPI;

namespace Gtacnr.Client.Jobs;

public abstract class BaseCallDispatch<T> : BaseDispatch<T> where T : CallInfo
{
	private static readonly MenuItem noCallsMenuItem = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_JOB_NO_CALLS_TEXT), LocalizationController.S(Entries.Imenu.IMENU_JOB_NO_CALLS_DESCRIPTION))
	{
		Enabled = false
	};

	protected T? lastCall;

	protected override MenuItem NoItemsMenuItem => noCallsMenuItem;

	public BaseCallDispatch(string name, string subtitle)
		: base(name, subtitle)
	{
	}

	public override void ResetMenu()
	{
		lastCall = null;
		base.ResetMenu();
	}

	protected void TryRespondToCall(T call)
	{
		if (!call.Responded)
		{
			RespondToCall(call);
			lastCall = call;
			call.Responded = true;
			callItems[call].Description = call.GetMenuItemDescription();
		}
	}

	protected abstract void RespondToCall(T call);

	protected override void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem.ItemData is T call)
		{
			TryRespondToCall(call);
		}
	}
}
