using System.Collections.Generic;

namespace Gtacnr.Model.Enums;

public static class AccountType
{
	public static readonly string Cash = "cash";

	public static readonly string Bank = "bank";

	public static readonly string Hospital = "hospital";

	public static readonly string BailBond = "bail_bond";

	public static readonly string Government = "government";

	public static readonly string Delivery = "delivery";

	public static readonly string TaxesDue = "taxes_due";

	public static readonly string TaxesPaid = "taxes_paid";

	public static readonly string StaffPenalty = "staff_penalty";

	public static readonly string AmmunationGiftCard = "ammunation_giftcard";

	public static readonly string DealershipGiftCard = "dealership_giftcard";

	public static readonly string RacingBet = "racing_bet";

	private static Dictionary<string, string> names = new Dictionary<string, string>
	{
		{ Cash, "Cash" },
		{ Bank, "Bank" },
		{ Hospital, "Hospital" },
		{ BailBond, "Bail Bonds" },
		{ Government, "State of San Andreas" },
		{ Delivery, "Delivery Company" },
		{ StaffPenalty, "Staff Penalty" },
		{ TaxesDue, "Taxes Due" },
		{ TaxesPaid, "Taxes Paid" },
		{ AmmunationGiftCard, "Ammu-Nation Gift Card" },
		{ DealershipGiftCard, "Dealership Gift Card" },
		{ RacingBet, "Racing Bet" }
	};

	public static IReadOnlyCollection<string> All => new _003C_003Ez__ReadOnlyArray<string>(new string[11]
	{
		Cash, Bank, Hospital, BailBond, Government, Delivery, TaxesDue, TaxesPaid, StaffPenalty, AmmunationGiftCard,
		DealershipGiftCard
	});

	public static IReadOnlyCollection<string> AllCredit => new _003C_003Ez__ReadOnlyArray<string>(new string[4] { Cash, Bank, AmmunationGiftCard, DealershipGiftCard });

	public static IReadOnlyCollection<string> AllDebt => new _003C_003Ez__ReadOnlyArray<string>(new string[7] { Hospital, BailBond, Government, Delivery, TaxesDue, TaxesPaid, StaffPenalty });

	public static string GetName(string account)
	{
		return names[account];
	}
}
