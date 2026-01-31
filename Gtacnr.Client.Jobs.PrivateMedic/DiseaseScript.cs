using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.API;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Items;
using Gtacnr.Data;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.PrivateMedic;

public class DiseaseScript : Script
{
	private class DiseaseState
	{
		public DateTime HealthT { get; set; }

		public DateTime FeverT { get; set; }

		public DateTime CoughT { get; set; }

		public DateTime VomitT { get; set; }

		public int FeverCount { get; set; }

		public int CoughCount { get; set; }

		public int VomitCount { get; set; }

		public int FeverR { get; set; }

		public int CoughR { get; set; }

		public int VomitR { get; set; }
	}

	private static HashSet<string> currentDiseases = new HashSet<string>();

	private bool isDiseaseTaskAttached;

	private Dictionary<string, DiseaseState> diseaseState = new Dictionary<string, DiseaseState>();

	private bool feverTaskAttached;

	public static IEnumerable<IDisease> CurrentDiseaseDefinitions => currentDiseases.Select(Diseases.GetDiseaseById).WhereNotNull();

	private void StartDisease(Disease disease)
	{
		if (!currentDiseases.Contains(disease.Id))
		{
			Debug.WriteLine("Diseases: " + disease.Name + " started");
			currentDiseases.Add(disease.Id);
			Utils.DisplayHelpText("You have contracted a ~y~disease ~s~(~r~" + disease.Name + "~s~). You should seek medical attention.");
		}
		if (currentDiseases.Count > 0 && !isDiseaseTaskAttached)
		{
			isDiseaseTaskAttached = true;
			base.Update += DiseaseTask;
		}
	}

	private void EndDisease(Disease disease, bool message = true)
	{
		if (disease != null)
		{
			Debug.WriteLine("Diseases: " + disease.Name + " ended");
			currentDiseases.Remove(disease.Id);
			if (message)
			{
				Utils.DisplayHelpText("Your ~r~disease ~s~(~y~" + disease.Name + "~s~) has ended.");
			}
		}
		if (currentDiseases.Count == 0 && isDiseaseTaskAttached)
		{
			isDiseaseTaskAttached = false;
			base.Update -= DiseaseTask;
		}
	}

	[EventHandler("gtacnr:diseases:start")]
	private void StartDisease(string diseaseId)
	{
		Disease diseaseById = Diseases.GetDiseaseById(diseaseId);
		if (diseaseById != null)
		{
			StartDisease(diseaseById);
		}
	}

	[EventHandler("gtacnr:diseases:end")]
	private void EndDisease(string id)
	{
		Disease diseaseById = Diseases.GetDiseaseById(id);
		if (diseaseById != null)
		{
			EndDisease(diseaseById);
		}
	}

	[EventHandler("gtacnr:diseases:endAll")]
	private void EndAllDiseases()
	{
		foreach (string item in currentDiseases.ToList())
		{
			Disease diseaseById = Diseases.GetDiseaseById(item);
			if (diseaseById != null)
			{
				EndDisease(diseaseById, message: false);
			}
		}
	}

	[EventHandler("gtacnr:diseases:playerCoughedOrVomited")]
	private async void OnPlayerCoughed(int playerId, int type, int audioId)
	{
		int playerFromServerId = API.GetPlayerFromServerId(playerId);
		if (playerFromServerId < 1)
		{
			return;
		}
		Ped ped = new Ped(API.GetPlayerPed(playerFromServerId));
		if ((Entity)(object)ped == (Entity)null)
		{
			return;
		}
		Vector3 position = ((Entity)ped).Position;
		if (((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) <= 30f.Square() && playerId != Game.Player.ServerId)
		{
			Utils.SendNotification(LatentPlayers.Get(playerId).ColorNameAndId + " ~r~" + ((type == 1) ? "coughed" : "vomited") + "~s~.");
		}
		bool flag = Utils.GetFreemodePedSex(ped) == Sex.Male;
		string audioName = type switch
		{
			2 => $"vomit_{audioId}", 
			1 => "cough_" + (flag ? "m" : "f") + $"_{audioId}", 
			_ => null, 
		};
		if (audioName != null)
		{
			PlayDelayedSound();
			if (playerId == Game.Player.ServerId && API.GetVehiclePedIsEntering(((PoolObject)ped).Handle) == 0)
			{
				switch (type)
				{
				case 1:
					PlayAnimation("timetable@gardener@smoking_joint", "idle_cough", 4000);
					break;
				case 2:
					PlayAnimation("re@construction", "out_of_breath", 4000);
					break;
				}
			}
		}
		if (type == 2)
		{
			await BaseScript.Delay(1000);
			if ((Entity)(object)ped.CurrentVehicle == (Entity)null)
			{
				PlayParticles(3000);
			}
		}
		async void PlayAnimation(string animDict, string animName, int duration)
		{
			Game.PlayerPed.Task.PlayAnimation(animDict, animName, 2f, 2f, duration, (AnimationFlags)48, 0f);
			DateTime startT = DateTime.UtcNow;
			while (!Gtacnr.Utils.CheckTimePassed(startT, duration))
			{
				await BaseScript.Delay(250);
				if ((int)Game.PlayerPed.Weapons.Current.Hash != -1569615261 || API.GetVehiclePedIsEntering(((PoolObject)ped).Handle) != 0)
				{
					Game.PlayerPed.Task.ClearAnimation(animDict, animName);
					break;
				}
			}
		}
		async void PlayDelayedSound()
		{
			await BaseScript.Delay(1250);
			Utils.PlaySoundFromEntityFromAudioBank(-1, audioName, ((PoolObject)ped).Handle, "gtacnr_diseases", isNetwork: false, "gtacnr_audio/diseases");
		}
		async void PlayParticles(int duration)
		{
			API.RequestNamedPtfxAsset("scr_paletoscore");
			while (!API.HasNamedPtfxAssetLoaded("scr_paletoscore"))
			{
				await BaseScript.Delay(100);
			}
			API.UseParticleFxAssetNextCall("scr_paletoscore");
			int ptfxHandle = API.StartParticleFxLoopedOnPedBone("scr_trev_puke", ((PoolObject)ped).Handle, 0f, 0f, 0f, 0f, 0f, 0f, 31086, 1f, false, false, false);
			await BaseScript.Delay(duration);
			API.StopParticleFxLooped(ptfxHandle, false);
			API.RemoveNamedPtfxAsset("scr_paletoscore");
		}
	}

	[EventHandler("gtacnr:diseases:tryAirSpread")]
	private void TrySpreadByAir(int token, int spreader, string jDisease)
	{
		int num;
		if (API.GetPedDrawableVariation(((PoolObject)Game.PlayerPed).Handle, 1) > 0)
		{
			num = ((Gtacnr.Utils.GetRandomDouble() < 0.5) ? 1 : 0);
			if (num == 0)
			{
				Debug.WriteLine("Diseases: spread failed, protected by mask");
			}
		}
		else
		{
			num = 1;
		}
		Respond((byte)num != 0);
		void Respond(bool response)
		{
			BaseScript.TriggerServerEvent("gtacnr:diseases:tryAirSpread:response", new object[2] { token, response });
		}
	}

	private void DepleteHealth(int amount, bool fatal)
	{
		if (((Entity)Game.PlayerPed).Health > amount + 1 || fatal)
		{
			Ped playerPed = Game.PlayerPed;
			((Entity)playerPed).Health = ((Entity)playerPed).Health - amount;
		}
		else if (((Entity)Game.PlayerPed).IsAlive)
		{
			((Entity)Game.PlayerPed).Health = 1;
		}
		if (((Entity)Game.PlayerPed).Health <= 0 || ((Entity)Game.PlayerPed).IsDead)
		{
			DeathScript.ForceDeathCause = -998;
			Game.PlayerPed.Kill();
		}
	}

	private async Coroutine DiseaseTask()
	{
		await BaseScript.Delay(500);
		if ((Entity)(object)Game.PlayerPed == (Entity)null || ((Entity)Game.PlayerPed).IsDead || ModeratorMenuScript.IsOnDuty)
		{
			return;
		}
		foreach (string currentDisease in currentDiseases)
		{
			Disease diseaseById = Diseases.GetDiseaseById(currentDisease);
			if (diseaseById == null)
			{
				continue;
			}
			if (!this.diseaseState.ContainsKey(diseaseById.Id))
			{
				this.diseaseState[diseaseById.Id] = new DiseaseState();
			}
			DiseaseState diseaseState = this.diseaseState[diseaseById.Id];
			if (Gtacnr.Utils.CheckTimePassed(diseaseState.HealthT, diseaseById.Time * 1000))
			{
				diseaseState.HealthT = DateTime.UtcNow;
				DepleteHealth(diseaseById.Health, diseaseById.Fatal);
			}
			if (diseaseById.Fever > 0)
			{
				if (diseaseState.FeverR == 0)
				{
					diseaseState.FeverR = Gtacnr.Utils.GetRandomInt(40, 80) * 1000;
				}
				if (diseaseState.FeverCount < diseaseById.Fever && Gtacnr.Utils.CheckTimePassed(diseaseState.FeverT, diseaseState.FeverR))
				{
					diseaseState.FeverT = DateTime.UtcNow;
					diseaseState.FeverCount++;
					Utils.SendNotification("You have ~r~fever~s~! Fever affects your movement.");
					FeverAsync();
				}
			}
			if (diseaseById.Cough > 0 && !DrugScript.CurrentDrugs.Any<KeyValuePair<string, DrugScript.DrugState>>((KeyValuePair<string, DrugScript.DrugState> kvp) => kvp.Key == "opiate"))
			{
				if (diseaseState.CoughR == 0)
				{
					diseaseState.CoughR = Gtacnr.Utils.GetRandomInt(25, 50) * 1000;
				}
				if (diseaseState.CoughCount < diseaseById.Cough && Gtacnr.Utils.CheckTimePassed(diseaseState.CoughT, diseaseState.CoughR))
				{
					diseaseState.CoughT = DateTime.UtcNow;
					diseaseState.CoughCount++;
					int amount = ((float)diseaseById.Health * 0.5f).ToInt();
					DepleteHealth(amount, diseaseById.Fatal);
					bool flag = API.GetPedDrawableVariation(((PoolObject)Game.PlayerPed).Handle, 1) > 0;
					BaseScript.TriggerServerEvent("gtacnr:diseases:cough", new object[2] { diseaseById.Id, flag });
					if ((Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)null)
					{
						Utils.SendNotification("You ~r~coughed~s~! You risk infecting nearby players.");
					}
					else
					{
						Utils.SendNotification("You ~r~coughed~s~! You risk infecting players in your vehicle.");
					}
				}
			}
			if (diseaseById.Vomit <= 0)
			{
				continue;
			}
			if (diseaseState.VomitR == 0)
			{
				diseaseState.VomitR = Gtacnr.Utils.GetRandomInt(100, 180) * 1000;
			}
			if (diseaseState.VomitCount < diseaseById.Vomit && Gtacnr.Utils.CheckTimePassed(diseaseState.VomitT, diseaseState.VomitR))
			{
				diseaseState.VomitT = DateTime.UtcNow;
				diseaseState.VomitCount++;
				int amount2 = ((float)diseaseById.Health * 1.5f).ToInt();
				DepleteHealth(amount2, diseaseById.Fatal);
				BaseScript.TriggerServerEvent("gtacnr:diseases:vomit", new object[1] { diseaseById.Id });
				if ((Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)null)
				{
					Utils.SendNotification("You ~r~vomited~s~! You contaminated this area, players who get in the area risk getting infected.");
				}
				else
				{
					Utils.SendNotification("You ~r~vomited~s~! You contaminated this vehicle, players are in this vehicle risk getting infected.");
				}
			}
		}
		async void FeverAsync()
		{
			AttachFeverTask();
			DateTime t = DateTime.UtcNow;
			while (!Gtacnr.Utils.CheckTimePassed(t, 20000.0))
			{
				await BaseScript.Delay(25);
				if (Game.PlayerPed.IsJumping)
				{
					Game.PlayerPed.Ragdoll(100, (RagdollType)0);
				}
				API.RequestAnimSet("move_m@hobo@a");
				while (!API.HasAnimSetLoaded("move_m@hobo@a"))
				{
					await BaseScript.Delay(1);
				}
				API.SetPedMovementClipset(API.PlayerPedId(), "move_m@hobo@a", 0.1f);
				API.RemoveAnimSet("move_m@hobo@a");
			}
			API.ResetPedMovementClipset(((PoolObject)Game.PlayerPed).Handle, 1f);
			DetachFeverTask();
		}
	}

	private async Coroutine FeverTask()
	{
		API.SetPedMoveRateOverride(API.PlayerPedId(), 0.75f);
	}

	private void AttachFeverTask()
	{
		if (!feverTaskAttached)
		{
			feverTaskAttached = true;
			base.Update += FeverTask;
		}
	}

	private void DetachFeverTask()
	{
		if (feverTaskAttached)
		{
			feverTaskAttached = false;
			base.Update -= FeverTask;
		}
	}
}
