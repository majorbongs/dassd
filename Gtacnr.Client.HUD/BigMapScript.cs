using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Characters.Lifecycle;
using MenuAPI;

namespace Gtacnr.Client.HUD;

public class BigMapScript : Script
{
	protected override void OnStarted()
	{
		KeysScript.AttachListener((Control)20, OnKeyEvent, 100);
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)control == 20 && eventType == KeyEventType.DoublePressed && !MenuController.IsAnyMenuOpen() && SpawnScript.HasSpawned)
		{
			if (inputType == InputType.Controller && API.IsAimCamActive())
			{
				return false;
			}
			if (API.IsBigmapActive())
			{
				API.SetBigmapActive(false, false);
			}
			else
			{
				API.SetBigmapActive(true, false);
			}
			return true;
		}
		return false;
	}
}
