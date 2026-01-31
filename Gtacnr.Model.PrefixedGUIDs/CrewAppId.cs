using System;

namespace Gtacnr.Model.PrefixedGUIDs;

public sealed class CrewAppId : PrefixedGuid
{
	protected override string Prefix => "capp";

	public CrewAppId(Guid value)
		: base(value)
	{
	}

	public CrewAppId(string prefixedGuidString)
		: base(prefixedGuidString)
	{
	}
}
