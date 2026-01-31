using System;

namespace Gtacnr.Localization;

public class LanguageChangedEventArgs : EventArgs
{
	public string OldLanguage { get; private set; }

	public string NewLanguage { get; private set; }

	public LanguageChangedEventArgs(string oldLanguage, string newLanguage)
	{
		OldLanguage = oldLanguage;
		NewLanguage = newLanguage;
	}
}
