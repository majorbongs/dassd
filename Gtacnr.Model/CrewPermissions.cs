using System;

namespace Gtacnr.Model;

[Flags]
public enum CrewPermissions : uint
{
	None = 0u,
	AddMembers = 1u,
	RemoveMembers = 2u,
	PromoteDemoteMembers = 4u,
	ManageRanks = 8u,
	ManagePermissions = 0x10u,
	ManageCrewInfo = 0x20u
}
