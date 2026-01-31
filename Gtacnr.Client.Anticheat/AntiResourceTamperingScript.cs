using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Anticheat;

public class AntiResourceTamperingScript : Script
{
	private List<List<string>> bannedTextures = Gtacnr.Utils.LoadJson<List<List<string>>>("data/anticheat/bannedTextures.json");

	[EventHandler("onResourceStop")]
	private void OnResourceStop(string resource)
	{
		if (!(resource != "gtacnr_anticheat"))
		{
			BaseScript.TriggerServerEvent("gtacnr:ac:disabledACResource", new object[0]);
		}
	}

	[Update]
	private async Coroutine TexturesCheck()
	{
		await Script.Wait(60000);
		foreach (List<string> bannedTexture in bannedTextures)
		{
			if (DoesTextureExists(bannedTexture[0], bannedTexture[1]))
			{
				BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[3] { 30, 2, "using mod menu" });
			}
		}
	}

	private static bool DoesTextureExists(string dict, string texture)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		Vector3 textureResolution = API.GetTextureResolution(dict, texture);
		if (textureResolution.X == 4f)
		{
			return textureResolution.Y != 4f;
		}
		return true;
	}
}
