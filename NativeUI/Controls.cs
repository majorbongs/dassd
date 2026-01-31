using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace NativeUI;

public static class Controls
{
	private static readonly Control[] NecessaryControlsKeyboard;

	private static readonly Control[] NecessaryControlsGamePad;

	public static void Toggle(bool toggle)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected I4, but got Unknown
		if (toggle)
		{
			Game.EnableAllControlsThisFrame(2);
			return;
		}
		Game.DisableAllControlsThisFrame(2);
		Control[] array = (((int)Game.CurrentInputMode == 1) ? NecessaryControlsGamePad : NecessaryControlsKeyboard);
		foreach (Control val in array)
		{
			API.EnableControlAction(0, (int)val, true);
		}
	}

	static Controls()
	{
		Control[] array = new Control[27];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		NecessaryControlsKeyboard = (Control[])(object)array;
		Control[] necessaryControlsKeyboard = NecessaryControlsKeyboard;
		Control[] array2 = new Control[4];
		RuntimeHelpers.InitializeArray(array2, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		NecessaryControlsGamePad = necessaryControlsKeyboard.Concat((IEnumerable<Control>)(object)array2).ToArray();
	}
}
