using System;
using MenuAPI;

namespace Gtacnr.Model;

public sealed class CrewCreationRequest
{
	public Guid Id { get; set; }

	public string OwnerId { get; set; }

	public string Acronym { get; set; }

	public string Name { get; set; }

	public CrewCreationRequestStatus Status { get; set; }

	public DateTime DateTime { get; set; }

	public string OwnerUsername { get; set; }

	public MenuItem ToMenuItem()
	{
		string text = Status switch
		{
			CrewCreationRequestStatus.Pending => "~y~Pending", 
			CrewCreationRequestStatus.Approved => "~g~Approved", 
			CrewCreationRequestStatus.Rejected => "~r~Rejected", 
			CrewCreationRequestStatus.Invalidated => "~o~Invalidated", 
			CrewCreationRequestStatus.InvalidatedAndRefunded => "~o~Invalidated and Refunded", 
			_ => "~w~Unknown", 
		};
		MenuItem menuItem = new MenuItem(Acronym);
		menuItem.Description = "~b~Name: ~s~" + Name + "~n~~b~Owner: ~s~" + OwnerUsername + "~n~~b~Status: ~s~" + text + "~n~~b~Date: ~s~" + DateTime.ToString("MMMM dd, yyyy HH:mm");
		menuItem.Enabled = Status == CrewCreationRequestStatus.Pending;
		menuItem.ItemData = this;
		return menuItem;
	}
}
