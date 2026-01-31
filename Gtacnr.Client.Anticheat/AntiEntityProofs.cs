using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Anticheat;

public class AntiEntityProofs : Script
{
	private static readonly DetectionThresholdManager proofsDetectionManager = new DetectionThresholdManager(3, TimeSpan.FromMinutes(1.0));

	private static bool _gasProof = false;

	private static bool _drownProof = false;

	public AntiEntityProofs()
	{
		EventHandlerDictionary eventHandlers = ((BaseScript)this).EventHandlers;
		eventHandlers["gtacnr:spawned"] = eventHandlers["gtacnr:spawned"] + (Delegate)new Action(OnSpawnedOrRespawned);
		eventHandlers = ((BaseScript)this).EventHandlers;
		eventHandlers["gtacnr:respawned"] = eventHandlers["gtacnr:respawned"] + (Delegate)new Action(OnSpawnedOrRespawned);
	}

	public static void SetPlayerProofs(bool gasProof, bool drownProof)
	{
		_gasProof = gasProof;
		_drownProof = drownProof;
		API.SetEntityProofs(((PoolObject)Game.PlayerPed).Handle, false, false, false, false, false, false, gasProof, drownProof);
	}

	[EventHandler("gtacnr:spawned")]
	private void OnSpawned()
	{
		base.Update += Check;
	}

	private async Coroutine Check()
	{
		await Script.Wait(50);
		Ped playerPed = Game.PlayerPed;
		if (!((Entity)(object)playerPed == (Entity)null))
		{
			bool bulletProof = false;
			bool fireProof = false;
			bool explosionProof = false;
			bool collisionProof = false;
			bool meleeProof = false;
			bool steamProof = false;
			bool gasProof = false;
			bool drownProof = false;
			API.GetEntityProofs(((PoolObject)playerPed).Handle, ref bulletProof, ref fireProof, ref explosionProof, ref collisionProof, ref meleeProof, ref steamProof, ref gasProof, ref drownProof);
			if (_gasProof)
			{
				gasProof = false;
			}
			if (_drownProof)
			{
				drownProof = false;
			}
			await Script.Wait(50);
			if (_gasProof)
			{
				gasProof = false;
			}
			if (_drownProof)
			{
				drownProof = false;
			}
			if (bulletProof || fireProof || explosionProof || collisionProof || meleeProof || steamProof || gasProof || drownProof)
			{
				BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[4]
				{
					30,
					2,
					"player proofs",
					$"bulletProof: {bulletProof}\nfireProof: {fireProof}\nexplosionProof: {explosionProof}\ncollisionProof: {collisionProof}\nmeleeProof: {meleeProof}\nsteamProof: {steamProof}\ngasProof: {gasProof}\ndrownProof: {drownProof}"
				});
			}
		}
	}

	private void OnSpawnedOrRespawned()
	{
		Utils.SetPedConfigFlagEx(API.PlayerPedId(), PedConfigFlag.DisableHelmetArmor, value: true);
	}
}
