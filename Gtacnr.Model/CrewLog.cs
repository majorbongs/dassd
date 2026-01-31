using System;
using Gtacnr.Model.PrefixedGUIDs;
using MenuAPI;

namespace Gtacnr.Model;

public sealed class CrewLog
{
	public Guid Id { get; set; }

	public CrewId CrewId { get; set; }

	public string UserId { get; set; }

	public DateTime DateTime { get; set; }

	public string AffectedUserId { get; set; }

	public CrewLogType LogType { get; set; }

	public CrewLogKickData? KickData { get; set; }

	public CrewLogAcronymStyleChangedData? AcronymStyleChangedData { get; set; }

	public CrewLogAcronymSeparatorChangedData? AcronymSeparatorChangedData { get; set; }

	public CrewLogMemberPermissionsChangedData? MemberPermissionsChangedData { get; set; }

	public CrewLogMemberRankChangedData? MemberRankChangedData { get; set; }

	public string Username { get; set; }

	public string AffectedUsername { get; set; }

	public MenuItem ToMenuItem()
	{
		DateTime dateTime = DateTime.ToLocalTime();
		string description = LogType switch
		{
			CrewLogType.MemberAdded => "~b~Member Added: ~s~" + AffectedUsername + "~n~~b~By: ~s~" + Username + "~n~~b~Date: ~s~" + dateTime.ToString("MMMM dd, yyyy HH:mm"), 
			CrewLogType.MemberKicked => "~b~Member Kicked: ~s~" + AffectedUsername + "~n~~b~By: ~s~" + Username + "~n~~b~Date: ~s~" + dateTime.ToString("MMMM dd, yyyy HH:mm") + ((KickData != null && !string.IsNullOrWhiteSpace(KickData.Reason)) ? ("~n~~b~Reason: ~s~" + KickData.Reason) : ""), 
			CrewLogType.AcronymStyleChanged => "~b~Acronym Style Changed~n~~b~By: ~s~" + Username + "~n~~b~Date: ~s~" + dateTime.ToString("MMMM dd, yyyy HH:mm") + "~n~" + ((AcronymStyleChangedData != null) ? $"~b~From: ~s~{AcronymStyleChangedData.Previous} ~b~To: ~s~{AcronymStyleChangedData.New}" : ""), 
			CrewLogType.AcronymSeparatorChanged => "~b~Acronym Separator Changed~n~~b~By: ~s~" + Username + "~n~~b~Date: ~s~" + dateTime.ToString("MMMM dd, yyyy HH:mm") + "~n~" + ((AcronymSeparatorChangedData != null) ? $"~b~From: ~s~{AcronymSeparatorChangedData.Previous} ~b~To: ~s~{AcronymSeparatorChangedData.New}" : ""), 
			CrewLogType.MemberPermissionsChanged => "~b~Member Permissions Changed: ~s~" + AffectedUsername + "~n~~b~By: ~s~" + Username + "~n~~b~Date: ~s~" + dateTime.ToString("MMMM dd, yyyy HH:mm") + "~n~" + ((MemberPermissionsChangedData != null) ? ("~b~From: ~s~" + MemberPermissionsChangedData.Previous.PermsToStrings() + " ~b~To: ~s~" + MemberPermissionsChangedData.New.PermsToStrings()) : ""), 
			CrewLogType.MemberRankChanged => "~b~Member Rank Changed: ~s~" + AffectedUsername + "~n~~b~By: ~s~" + Username + "~n~~b~Date: ~s~" + dateTime.ToString("MMMM dd, yyyy HH:mm") + "~n~" + ((MemberRankChangedData != null) ? ("~b~From: ~s~" + CrewRankData.GetDefaultRankName(MemberRankChangedData.Previous) + " ~b~To: ~s~" + CrewRankData.GetDefaultRankName(MemberRankChangedData.New)) : ""), 
			CrewLogType.MemberLeft => "~b~Member Left: ~s~" + Username + "~n~~b~Date: ~s~" + dateTime.ToString("MMMM dd, yyyy HH:mm"), 
			CrewLogType.OwnershipTransferred => "~b~Ownership Transferred: ~s~" + Username + " => " + AffectedUsername + "~n~~b~Date: ~s~" + dateTime.ToString("MMMM dd, yyyy HH:mm"), 
			_ => "~b~Log Type: ~s~Unknown~n~~b~By: ~s~" + Username + "~n~~b~Date: ~s~" + dateTime.ToString("MMMM dd, yyyy HH:mm"), 
		};
		return new MenuItem($"{LogType}")
		{
			Description = description,
			ItemData = this
		};
	}
}
