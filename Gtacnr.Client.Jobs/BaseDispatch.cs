using System.Collections.Generic;
using System.Linq;
using Gtacnr.Model;
using MenuAPI;
using Rock.Collections;

namespace Gtacnr.Client.Jobs;

public abstract class BaseDispatch<T> where T : DispatchInfoBase
{
	private const int MAX_CALLS = 40;

	protected OrderedDictionary<T, MenuItem> callItems = new OrderedDictionary<T, MenuItem>();

	public Menu CallsMenu { get; private set; }

	protected abstract MenuItem NoItemsMenuItem { get; }

	public BaseDispatch(string name, string subtitle)
	{
		CallsMenu = new Menu(name, subtitle);
		CallsMenu.OnItemSelect += OnItemSelect;
		CallsMenu.OnMenuOpen += OnMenuOpen;
		CallsMenu.AddMenuItem(NoItemsMenuItem);
	}

	public virtual void ResetMenu()
	{
		CallsMenu.ClearMenuItems();
		CallsMenu.AddMenuItem(NoItemsMenuItem);
		callItems.Clear();
	}

	public abstract void OnDispatch(int playerId, string jData);

	public void OnDispatch(int playerId)
	{
		OnDispatch(playerId, null);
	}

	protected void AddCall(T call)
	{
		if (callItems.Count == 0 && CallsMenu.GetMenuItems().Count == 1)
		{
			CallsMenu.ClearMenuItems();
		}
		if (callItems.Count == 40)
		{
			T key = callItems.Keys.First();
			CallsMenu.RemoveMenuItem(callItems[key]);
			callItems.Remove(key);
		}
		RefreshCalls();
		MenuItem menuItem = call.ToMenuItem();
		CallsMenu.InsertMenuItem(menuItem, 0);
		callItems.Add(call, menuItem);
	}

	private void OnMenuOpen(Menu menu)
	{
		menu.CurrentIndex = 0;
		RefreshCalls();
	}

	protected abstract void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex);

	private void RefreshCalls()
	{
		foreach (KeyValuePair<T, MenuItem> callItem in callItems)
		{
			callItem.Key.UpdateMenuItem(callItem.Value);
		}
	}
}
