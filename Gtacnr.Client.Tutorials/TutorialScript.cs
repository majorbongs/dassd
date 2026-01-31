using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.HUD;
using Gtacnr.Model;

namespace Gtacnr.Client.Tutorials;

public class TutorialScript : Script
{
	public static bool IsInTutorial { get; private set; }

	public List<Tutorial> Tutorials { get; set; } = new List<Tutorial>();

	protected override void OnStarted()
	{
		foreach (string item in Gtacnr.Utils.LoadJson<List<string>>("data/tutorials/files.json"))
		{
			Tutorials.Add(Gtacnr.Utils.LoadJson<Tutorial>("data/tutorials/" + item));
		}
	}

	[EventHandler("gtacnr:tutorials:start")]
	private async void StartTutorial(string tutorialId)
	{
		Tutorial tutorial = Tutorials.FirstOrDefault((Tutorial t) => t.Id == tutorialId);
		if (Utils.IsSwitchInProgress())
		{
			await Utils.SwitchIn();
		}
		if (API.GetIsLoadingScreenActive())
		{
			API.ShutdownLoadingScreen();
			API.ShutdownLoadingScreenNui();
		}
		IsInTutorial = true;
		Utils.Freeze();
		API.DisplayRadar(false);
		HideHUDScript.ToggleChat(toggle: false, showMessage: false);
		bool isSongPlaying = false;
		List<int> cameras = new List<int>();
		foreach (TutorialIntroStep step in tutorial.Intro.Steps)
		{
			int camera1 = API.CreateCamWithParams("DEFAULT_SCRIPTED_CAMERA", step.Camera.VFrom.X, step.Camera.VFrom.Y, step.Camera.VFrom.Z, 0f, 0f, 180f, 30f, false, 0);
			API.SetCamActive(camera1, true);
			API.PointCamAtCoord(camera1, step.Camera.VFocus.X, step.Camera.VFocus.Y, step.Camera.VFocus.Z);
			API.RenderScriptCams(true, false, 2000, true, true);
			API.SetFocusPosAndVel(step.Camera.VFocus.X, step.Camera.VFocus.Y, step.Camera.VFocus.Z, 0f, 0f, 0f);
			Utils.DisplayHelpText();
			await BaseScript.Delay(1000);
			await Utils.FadeIn();
			if (!isSongPlaying && tutorial.Song != null)
			{
				AudioScript.PlayAudio(tutorial.Song, 0.1f);
				isSongPlaying = true;
			}
			int camera2 = API.CreateCamWithParams("DEFAULT_SCRIPTED_CAMERA", step.Camera.VTo.X, step.Camera.VTo.Y, step.Camera.VTo.Z, 0f, 0f, 180f, 30f, false, 0);
			API.PointCamAtCoord(camera2, step.Camera.VFocus.X, step.Camera.VFocus.Y, step.Camera.VFocus.Z);
			await BaseScript.Delay(100);
			Utils.DisplayHelpText(await ApplyReplacements(step.Message));
			int pauseTime = 2000;
			await BaseScript.Delay(pauseTime);
			int time = Convert.ToInt32(100000f / step.Camera.Speed);
			API.SetCamActiveWithInterp(camera2, camera1, time, 1, 1);
			await Utils.FadeIn();
			await BaseScript.Delay(time + pauseTime);
			await Utils.FadeOut();
			cameras.Add(camera1);
			cameras.Add(camera2);
		}
		foreach (int item in cameras)
		{
			API.SetCamActive(item, false);
			API.DestroyCam(item, false);
		}
		API.RenderScriptCams(false, false, 0, true, false);
		API.SetFocusEntity(((PoolObject)Game.PlayerPed).Handle);
		if (isSongPlaying)
		{
			AudioScript.StopAudio();
		}
		BaseScript.TriggerEvent("gtacnr:spawn", new object[1] { tutorial.Intro.VTeleportTo });
		BaseScript.TriggerServerEvent("gtacnr:tutorials:onTutorialEnd", new object[1] { tutorialId });
		IsInTutorial = false;
		if (tutorialId == "main")
		{
			NewbieScript.StartNewbieMode();
		}
		static async Task<string> ApplyReplacements(string input)
		{
			return input.Replace("@PlayerName", await Authentication.GetAccountName());
		}
	}
}
