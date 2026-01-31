using System.Runtime.InteropServices;

namespace CitizenFX.Core;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct Repeat
{
	public const ulong Infinite = ulong.MaxValue;
}
