using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client;

namespace Gtacnr;

public class ResourceEvents : Script
{
	public static event EventHandler CurrentResourceStart;

	public static event EventHandler CurrentResourceStop;

	[EventHandler("onClientResourceStart")]
	private void OnResourceStart(string resourceName)
	{
		if (!(resourceName != API.GetCurrentResourceName()))
		{
			ResourceEvents.CurrentResourceStart?.Invoke(this, EventArgs.Empty);
		}
	}

	[EventHandler("onClientResourceStop")]
	private void OnResourceStop(string resourceName)
	{
		if (!(resourceName != API.GetCurrentResourceName()))
		{
			ResourceEvents.CurrentResourceStop?.Invoke(this, EventArgs.Empty);
		}
	}
}
