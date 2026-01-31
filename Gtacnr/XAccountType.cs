using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gtacnr;

public sealed class XAccountType
{
	public static readonly XAccountType Cash = new XAccountType("cash", "Cash");

	public static readonly XAccountType Bank = new XAccountType("bank", "Bank");

	public static readonly XAccountType Hospital = new XAccountType("hospital", "Hospital", isDebt: true);

	public static readonly XAccountType BailBond = new XAccountType("bail_bond", "Bail Bonds", isDebt: true);

	public static readonly XAccountType Government = new XAccountType("government", "State of San Andreas", isDebt: true);

	public static readonly XAccountType Delivery = new XAccountType("delivery", "Delivery Company", isDebt: true);

	public static readonly XAccountType TaxesDue = new XAccountType("taxes_due", "Taxes Due", isDebt: true);

	public static readonly XAccountType TaxesPaid = new XAccountType("taxes_paid", "Taxes Paid", isDebt: true);

	public static readonly XAccountType StaffPenalty = new XAccountType("staff_penalty", "Staff Penalty", isDebt: true);

	public static readonly XAccountType AmmunationGiftCard = new XAccountType("ammunation_giftcard", "Ammu-Nation Gift Card");

	public static readonly XAccountType DealershipGiftCard = new XAccountType("dealership_giftcard", "Dealership Gift Card");

	public static readonly IReadOnlyCollection<XAccountType> All = new ReadOnlyCollection<XAccountType>(new List<XAccountType>
	{
		Cash, Bank, Hospital, BailBond, Government, Delivery, TaxesDue, TaxesPaid, StaffPenalty, AmmunationGiftCard,
		DealershipGiftCard
	});

	public string Id { get; set; }

	public string Name { get; set; }

	public bool IsDebt { get; set; }

	public static IEnumerable<XAccountType> AllCredit => All.Where((XAccountType a) => !a.IsDebt);

	public static IEnumerable<XAccountType> AllDebt => All.Where((XAccountType a) => a.IsDebt);

	private XAccountType(string id, string name, bool isDebt = false)
	{
		Id = id;
		Name = name;
		IsDebt = isDebt;
	}
}
