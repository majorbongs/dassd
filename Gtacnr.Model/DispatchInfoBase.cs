using System;
using Gtacnr.Client.API;
using MenuAPI;

namespace Gtacnr.Model;

public abstract class DispatchInfoBase
{
	public int PlayerId { get; set; }

	public DateTime DateTime { get; set; }

	public MenuItem ToMenuItem()
	{
		PlayerState? obj = LatentPlayers.Get(PlayerId) ?? PlayerState.CreateDisconnectedPlayer(PlayerId);
		string text = Utils.CalculateTimeAgo(DateTime);
		return new MenuItem(obj.ColorNameAndId)
		{
			Label = "~c~" + text,
			Description = GetMenuItemDescription(),
			ItemData = this
		};
	}

	public abstract string GetMenuItemDescription();

	public void UpdateMenuItem(MenuItem item)
	{
		PlayerState playerState = LatentPlayers.Get(PlayerId) ?? PlayerState.CreateDisconnectedPlayer(PlayerId);
		string text = Utils.CalculateTimeAgo(DateTime);
		item.Text = playerState.ColorNameAndId;
		item.Label = "~c~" + text;
		item.Description = GetMenuItemDescription();
	}
}
