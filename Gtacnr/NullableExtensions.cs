using System.Diagnostics.CodeAnalysis;

namespace Gtacnr;

public static class NullableExtensions
{
	public static bool IsStringNullOrEmpty([NotNullWhen(false)] string? value)
	{
		return string.IsNullOrEmpty(value);
	}
}
