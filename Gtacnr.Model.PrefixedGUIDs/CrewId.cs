using System;

namespace Gtacnr.Model.PrefixedGUIDs;

public sealed class CrewId : PrefixedGuid
{
	protected override string Prefix => "crw";

	public CrewId(Guid value)
		: base(value)
	{
	}

	public static implicit operator CrewId(Guid value)
	{
		return new CrewId(value);
	}

	public CrewId(string prefixedGuidString)
		: base(prefixedGuidString)
	{
	}
}
