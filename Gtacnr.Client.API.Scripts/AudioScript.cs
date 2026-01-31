using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.API.Scripts;

public class AudioScript : Script
{
	public static void PlayAudio(string fileName, float volume = 1f, bool loop = false)
	{
		int profileSetting = API.GetProfileSetting(300);
		float num = volume * ((float)profileSetting / 10f);
		BaseScript.TriggerEvent("gtacnr:audio:play", new object[3] { fileName, num, loop });
	}

	public static void StopAudio()
	{
		BaseScript.TriggerEvent("gtacnr:audio:stop", new object[0]);
	}
}
