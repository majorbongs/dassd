using System;
using Gtacnr.Model.PrefixedGUIDs;
using MenuAPI;

namespace Gtacnr.Model;

public class CrewApplication
{
	public CrewAppId Id { get; set; }

	public CrewId CrewId { get; set; }

	public string SenderUserId { get; set; }

	public string Introduction { get; set; }

	public DateTime SentDateTime { get; set; }

	public string ResponderUserId { get; set; }

	public string ResponseText { get; set; }

	public DateTime? RespondedDateTime { get; set; }

	public CrewApplicationResponse Response { get; set; }

	public string Username { get; set; }

	public string RespondedByUsername { get; set; }

	public int Level { get; set; }

	public MenuItem ToMenuItem()
	{
		string text = Response switch
		{
			CrewApplicationResponse.Pending => "~y~Pending", 
			CrewApplicationResponse.Accepted => "~g~Accepted", 
			CrewApplicationResponse.Rejected => "~r~Rejected", 
			CrewApplicationResponse.Invalidated => "~o~Invalidated", 
			_ => "~w~Unknown", 
		};
		MenuItem menuItem = new MenuItem($"{Username} (Level {Level})");
		menuItem.Description = "~b~Status: ~s~" + text + "~n~~b~Sent: ~s~" + SentDateTime.ToString("MMMM dd, yyyy HH:mm") + "~n~" + ((Response != CrewApplicationResponse.Pending && RespondedDateTime.HasValue) ? ("~b~Responded: ~s~" + RespondedDateTime.Value.ToString("MMMM dd, yyyy HH:mm") + "~n~") : "") + "~b~Introduction: ~s~" + Introduction + "~n~" + ((Response != CrewApplicationResponse.Pending) ? ("~b~Response: ~s~" + ResponseText + "~n~") : "") + ((RespondedByUsername != null) ? ("~b~Responded by~s~: " + RespondedByUsername) : "");
		menuItem.Enabled = Response == CrewApplicationResponse.Pending;
		menuItem.ItemData = this;
		return menuItem;
	}
}
