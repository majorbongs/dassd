using CitizenFX.Core;
using Gtacnr.Client.API;

namespace Gtacnr.Client.Premium;

public class ReferralScript : Script
{
	[EventHandler("gtacnr:referral:announce")]
	private void OnAnnounceReferralProgram()
	{
		Utils.DisplayHelpText("~y~Announcement! ~s~Do you want to earn ~g~rewards ~s~for inviting your friends? Type ~b~/referral ~s~for more info.");
		Chat.AddSuggestion("/referral", "Obtain rewards for inviting friends to CnR!");
	}

	[EventHandler("gtacnr:referral:sendInfo")]
	private async void OnGetReferallInfo(string region, int reward, int xpToGain)
	{
		Chat.AddMessage(Gtacnr.Utils.Colors.Info, "The referral program has not been launched yet. Please, wait patiently until we announce it!");
	}
}
