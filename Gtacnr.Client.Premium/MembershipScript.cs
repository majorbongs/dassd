using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Premium;

public class MembershipScript : Script
{
	private static List<MembershipEntry> subscriptions;

	public static EventHandler SubscriptionInfoUpdated;

	private static MembershipScript instance;

	public static List<MembershipEntry> Subscriptions => subscriptions?.ToList();

	public static MembershipTier MembershipTier => GetCurrentMembershipTier();

	public static MembershipTier GetCurrentMembershipTier()
	{
		if (Subscriptions == null || Subscriptions.Count == 0)
		{
			return MembershipTier.None;
		}
		MembershipTier membershipTier = MembershipTier.None;
		foreach (MembershipEntry subscription in Subscriptions)
		{
			if ((int)subscription.Tier > (int)membershipTier)
			{
				membershipTier = subscription.Tier;
			}
		}
		return membershipTier;
	}

	public MembershipScript()
	{
		instance = this;
	}

	protected override async void OnStarted()
	{
		await Utils.WaitUntilAccountDataLoaded();
		await Refresh();
	}

	public static async Task Refresh()
	{
		subscriptions = null;
		string text = await instance.TriggerServerEventAsync<string>("gtacnr:memberships:getActiveSubscriptions", new object[0]);
		if (text != null)
		{
			subscriptions = text.Unjson<List<MembershipEntry>>();
		}
		StaffLevel staffLevel = (StaffLevel)(await instance.TriggerServerEventAsync<int>("gtacnr:admin:getLevel", new object[0]));
		if ((int)staffLevel >= 110)
		{
			subscriptions.Insert(0, new MembershipEntry
			{
				Tier = MembershipTier.Gold,
				IsTemporary = true
			});
		}
		else if ((int)staffLevel >= 10)
		{
			subscriptions.Insert(0, new MembershipEntry
			{
				Tier = MembershipTier.Silver,
				IsTemporary = true
			});
		}
		if (subscriptions.Count > 0)
		{
			instance.Print($"^6{subscriptions.Count} active subscriptions:");
		}
		foreach (MembershipEntry subscription in subscriptions)
		{
			string text2 = ((!subscription.IsTemporary) ? (subscription.StartDate.ToFormalDate() + " - " + subscription.ExpiryDate.ToFormalDate()) : "temporary");
			instance.Print("    " + Gtacnr.Utils.GetDescription(subscription.Tier) + " - " + text2);
		}
		SubscriptionInfoUpdated?.Invoke(instance, new EventArgs());
		BaseScript.TriggerEvent("gtacnr:membershipUpdated", new object[1] { (int)GetCurrentMembershipTier() });
	}
}
