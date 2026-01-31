using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.HUD;

public class NuiDialogScript : Script
{
	private static bool isDialogShown;

	private static bool hasInput;

	private static string input;

	protected override void OnStarted()
	{
		API.RegisterNuiCallbackType("gtacnr:onDialogResponse");
		API.SetNuiFocus(false, false);
	}

	public static async Task<string> Show(NuiDialog dialog)
	{
		if (isDialogShown)
		{
			return null;
		}
		isDialogShown = true;
		hasInput = false;
		input = null;
		API.SendNuiMessage(new
		{
			method = "showDialog",
			dialog = dialog
		}.Json());
		API.SetNuiFocus(true, true);
		API.SetNuiFocusKeepInput(false);
		while (!hasInput)
		{
			await BaseScript.Delay(10);
		}
		return input;
	}

	[EventHandler("__cfx_nui:gtacnr:onDialogResponse")]
	private void OnDialogResponse(dynamic data, CallbackDelegate callback)
	{
		isDialogShown = false;
		hasInput = true;
		input = data.Input as string;
		API.SetNuiFocus(false, false);
	}
}
