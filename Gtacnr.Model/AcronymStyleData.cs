using System;

namespace Gtacnr.Model;

public class AcronymStyleData
{
	public AcronymStyleSeparator Separator { get; set; }

	public AcronymStyle Style { get; set; } = AcronymStyle.SBrackets;

	public string ApplyToUsername(string username, string acronym)
	{
		string text = Separator switch
		{
			AcronymStyleSeparator.Dot => string.Join(".", acronym.ToCharArray()), 
			AcronymStyleSeparator.Dash => string.Join("-", acronym.ToCharArray()), 
			_ => acronym, 
		};
		string arg = ((Style == AcronymStyle.SBrackets) ? ("[" + text + "]") : text);
		return string.Format(Style switch
		{
			AcronymStyle.Pipe => "{0} | {1}", 
			AcronymStyle.DPipe => "{0} || {1}", 
			AcronymStyle.SBrackets => "{0} {1}", 
			AcronymStyle.X => "{0} x {1}", 
			_ => throw new NotImplementedException(), 
		}, arg, username);
	}
}
