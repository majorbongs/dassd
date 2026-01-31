using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Estates.Warehouses;
using Gtacnr.Client.Items;
using Gtacnr.Client.Weapons;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.Model.Robberies.Jewelry;

namespace Gtacnr.Client.Crimes.Robberies.Jewelry;

public class JewelryRobberyScript : Script
{
	private bool isGasDamageTaskAttached;

	private bool isGasVisible;

	private bool isLeaveAreaTaskAttached;

	private JewelryRobberyStateScript State => JewelryRobberyStateScript.Instance;

	private Business JewelryStore { get; set; }

	private BusinessJewelryRobberyData RobberyData { get; set; }

	public static event EventHandler RobberyInitiated;

	public static event EventHandler RobberyCompleted;

	public static event EventHandler RobberyFailed;

	public static event EventHandler StartedBreakingGlass;

	public static event EventHandler StoppedBreakingGlass;

	protected override async void OnStarted()
	{
		while (!BusinessScript.IsReady)
		{
			await BaseScript.Delay(0);
		}
		GetBusiness();
		AttachStateEvents();
		AddChatSuggestions();
	}

	private void GetBusiness()
	{
		JewelryStore = BusinessScript.Businesses.Values.FirstOrDefault((Business b) => b.JewelryRobbery != null);
		if (JewelryStore == null)
		{
			Print("~r~ERROR: the server is missing the proper jewelry robbery business setup.");
		}
		else
		{
			RobberyData = JewelryStore.JewelryRobbery;
		}
	}

	private void AttachStateEvents()
	{
		JewelryRobberyStateScript.RobberyStarted += OnRobberyStarted;
		JewelryRobberyStateScript.AlarmEnded += OnAlarmEnded;
		JewelryRobberyStateScript.GlassDisabled += OnGlassDisabled;
		JewelryRobberyUIScript.TeargasThrown += OnTeargasThrown;
		JewelryRobberyUIScript.StoreLeft += OnStoreLeft;
		JewelryRobberyUIScript.BreakGlassControlExecuted += OnBreakGlassControlExecuted;
		JewelryRobberyUIScript.CancelBreakGlassControlExecuted += OnCancelBreakGlassControlExecuted;
		WarehouseScript.Entered += OnWarehouseEntered;
	}

	private void AddChatSuggestions()
	{
		Chat.AddSuggestion("/jewelry", "Displays up-to-date information about the jewelry robbery.");
	}

	private void KillEmployees()
	{
		if (JewelryStore.Employees == null)
		{
			return;
		}
		foreach (BusinessEmployee item in JewelryStore.Employees.Where((BusinessEmployee e) => (Entity)(object)e.State?.Ped != (Entity)null))
		{
			Ped ped = item.State.Ped;
			item.State.PreventRespawn = true;
			CoughAndDie();
			async void CoughAndDie()
			{
				await BaseScript.Delay(Gtacnr.Utils.GetRandomInt(1, 12) * 600);
				ped.Task.ClearAllImmediately();
				ped.Task.PlayAnimation("timetable@gardener@smoking_joint", "idle_cough", 2f, 2f, 4000, (AnimationFlags)48, 0f);
				await BaseScript.Delay(Gtacnr.Utils.GetRandomInt(2, 6) * 1200);
				ped.Kill();
			}
		}
	}

	private void OnRobberyStarted(object sender, RobberyStartedEventArgs e)
	{
		AttachGasDamageTask();
	}

	private void OnAlarmEnded(object sender, EventArgs e)
	{
		DetachGasDamageTask();
		StopGasVisualFx();
		if (State.Player.IsParticipating && State.Player.Phase == RobberyPhase.Stealing && State.Player.TakenItemsCount > 0)
		{
			LeaveOrangeArea();
		}
	}

	private void OnStoreLeft(object sender, EventArgs e)
	{
		LeaveOrangeArea();
	}

	private void OnTeargasThrown(object sender, EventArgs e)
	{
		InitiateRobberyAsync();
	}

	private async void InitiateRobberyAsync()
	{
		ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:businesses:robberies:jewelry:start");
		switch (responseCode)
		{
		case ResponseCode.AlreadyInProgress:
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_ALREADY_IN_PROGRESS));
			break;
		case ResponseCode.NotEnoughCops:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_NOT_ENOUGH_COPS, JewelryStore.RobberyMinCops));
			break;
		case ResponseCode.TooEarly:
		{
			string text2 = LocalizationController.S($"weekday_{0}");
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_EARLY, text2 + " 10:00"));
			break;
		}
		case ResponseCode.TooLate:
		{
			string text = LocalizationController.S($"weekday_{6}");
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_LATE, text + " 14:00"));
			break;
		}
		default:
			Utils.DisplayError(responseCode, "", "InitiateRobberyAsync");
			break;
		case ResponseCode.Cooldown:
			break;
		case ResponseCode.Success:
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_INITIATED));
			State.Player.StartParticipating();
			StartGasVisualFx();
			JewelryRobberyScript.RobberyInitiated?.Invoke(this, new EventArgs());
			break;
		}
	}

	private async void StartGasVisualFx()
	{
		if (BusinessScript.ClosestBusiness != JewelryStore || isGasVisible)
		{
			return;
		}
		isGasVisible = true;
		while (isGasVisible)
		{
			foreach (Vector3 gasCoord in RobberyData.GasCoords)
			{
				API.AddExplosion(gasCoord.X, gasCoord.Y, gasCoord.Z, 20, 1000f, false, false, 0f);
			}
			await BaseScript.Delay(20000);
		}
	}

	private void StopGasVisualFx()
	{
		isGasVisible = false;
	}

	private void AttachGasDamageTask()
	{
		if (!isGasDamageTaskAttached)
		{
			isGasDamageTaskAttached = true;
			base.Update += GasDamageTask;
		}
	}

	private void DetachGasDamageTask()
	{
		if (isGasDamageTaskAttached)
		{
			isGasDamageTaskAttached = false;
			base.Update -= GasDamageTask;
		}
	}

	private async Coroutine GasDamageTask()
	{
		await BaseScript.Delay(100);
		if (BusinessScript.ClosestBusiness != JewelryStore || GasMaskScript.IsPlayerWearingGasMask())
		{
			return;
		}
		foreach (Vector3 gasCoord in RobberyData.GasCoords)
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			if (((Vector3)(ref position)).DistanceToSquared(gasCoord) <= 6.5f.Square())
			{
				Ped playerPed = Game.PlayerPed;
				((Entity)playerPed).Health = ((Entity)playerPed).Health - 4;
				Utils.ShakeGamepad(50);
				if (((Entity)Game.PlayerPed).Health <= 0)
				{
					DeathScript.ForceDeathCause = -37975472;
				}
				break;
			}
		}
	}

	[EventHandler("gtacnr:businesses:robberies:jewelry:cancelBreakGlass")]
	private void OnCancelBreakGlass()
	{
		CancelBreakGlass();
	}

	private async Coroutine BreakGlassAndStealAsync()
	{
		if (!State.CanBreakGlass || State.Player.IsBreakingGlass)
		{
			Utils.PlayErrorSound();
			return;
		}
		if (((Entity)Game.PlayerPed).Health == 0)
		{
			Utils.PlayErrorSound();
			return;
		}
		try
		{
			State.Player.IsBreakingGlass = true;
			JewelryRobberyScript.StartedBreakingGlass?.Invoke(this, new EventArgs());
			IEnumerable<WeaponDefinition> source = from wd in Gtacnr.Data.Items.GetAllWeaponDefinitions()
				where Game.PlayerPed.Weapons.HasWeapon((WeaponHash)wd.Hash)
				select wd;
			if (!source.Any((WeaponDefinition wd) => wd.GlassBreakChance > 0f))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_NEED_BLUNT_WEAPON));
				return;
			}
			WeaponDefinition weaponDefinition = Gtacnr.Data.Items.GetWeaponDefinitionByHash((uint)(int)Game.PlayerPed.Weapons.Current.Hash);
			if (weaponDefinition == null || weaponDefinition.GlassBreakChance <= 0f)
			{
				weaponDefinition = source.OrderByDescending((WeaponDefinition w) => w.GlassBreakChance).FirstOrDefault();
				if (weaponDefinition == null)
				{
					Print("^1Weapon configuration error.");
					Utils.PlayErrorSound();
					return;
				}
			}
			int targetGlassIndex = State.Player.TargetGlassIndex;
			Vector4 glass = RobberyData.GlassCoords.ElementAt(targetGlassIndex);
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			if (((Vector3)(ref position)).DistanceToSquared(glass.XYZ()) > 2f)
			{
				Print("^1Player moved away from the position.");
				Utils.PlayErrorSound();
				return;
			}
			WeaponBehaviorScript.BlockWeaponSwitchingById("jewelryRobbery");
			if ((int)Game.PlayerPed.Weapons.Current.Hash != weaponDefinition.Hash)
			{
				Game.PlayerPed.Weapons.Select((WeaponHash)weaponDefinition.Hash);
			}
			Game.PlayerPed.Task.ClearAll();
			Game.PlayerPed.Task.AchieveHeading(glass.W, 800);
			DateTime t = DateTime.Now;
			Dictionary<string, int> dictionary = (await TriggerServerEventAsync<string>("gtacnr:businesses:robberies:jewelry:startBreakGlass", new object[2] { targetGlassIndex, weaponDefinition.Hash })).Unjson<Dictionary<string, int>>();
			ResponseCode responseCode = (ResponseCode)dictionary["Code"];
			if (responseCode != ResponseCode.Success)
			{
				if (responseCode != ResponseCode.Cooldown)
				{
					Utils.DisplayError(responseCode, "", "BreakGlassAndStealAsync");
				}
				Game.PlayerPed.Task.ClearAll();
				return;
			}
			int successfulAttempt = dictionary["Attempt"];
			bool injured = dictionary["Injured"] != 0;
			int num = Convert.ToInt32((DateTime.Now - t).TotalMilliseconds);
			if (num < 800)
			{
				await BaseScript.Delay(800 - num);
			}
			((Entity)Game.PlayerPed).Position = glass.XYZ();
			((Entity)Game.PlayerPed).Heading = glass.W;
			bool smashed = false;
			for (int currentAttempt = 0; currentAttempt < 5; currentAttempt++)
			{
				if (smashed)
				{
					break;
				}
				if (!State.Player.IsBreakingGlass)
				{
					break;
				}
				Game.PlayerPed.Task.PlayAnimation("melee@large_wpn@streamed_core", "car_down_attack", 4f, -1, (AnimationFlags)0);
				await BaseScript.Delay(480);
				smashed = currentAttempt == successfulAttempt;
				if (smashed)
				{
					Game.PlaySound("Glass_Smash", "");
					ExitAnim();
				}
				else
				{
					Game.PlaySound("Drill_Pin_Break", "DLC_HEIST_FLEECA_SOUNDSET");
					await BaseScript.Delay(520);
				}
			}
			if (injured)
			{
				Ped playerPed = Game.PlayerPed;
				((Entity)playerPed).Health = ((Entity)playerPed).Health - 25;
			}
			if (!State.Player.IsBreakingGlass && !smashed)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_BREAK_GLASS_CANCELED));
				Game.PlayerPed.Task.ClearAll();
				return;
			}
			if (!smashed)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_BREAK_GLASS_FAILED));
				Game.PlayerPed.Task.ClearAll();
				return;
			}
			BreakGlass(targetGlassIndex);
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_BREAK_GLASS_SUCCEEDED));
			if (((Entity)Game.PlayerPed).Health == 0)
			{
				Utils.PlayErrorSound();
				return;
			}
			BaseScript.TriggerServerEvent("gtacnr:businesses:robberies:jewelry:stealFromGlass", new object[1] { targetGlassIndex });
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			State.Player.IsBreakingGlass = false;
			JewelryRobberyScript.StoppedBreakingGlass?.Invoke(this, new EventArgs());
			WeaponBehaviorScript.UnblockWeaponSwitchingById("jewelryRobbery");
		}
		static async void ExitAnim()
		{
			await BaseScript.Delay(500);
			Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
			await BaseScript.Delay(2000);
			Game.PlayerPed.Task.ClearAll();
		}
	}

	private void CancelBreakGlass()
	{
		if (State.Player.IsBreakingGlass)
		{
			State.Player.IsBreakingGlass = false;
		}
	}

	private void OnBreakGlassControlExecuted(object sender, BreakGlassEventArgs e)
	{
		BreakGlassAndStealAsync();
	}

	private void OnCancelBreakGlassControlExecuted(object sender, EventArgs e)
	{
		CancelBreakGlass();
	}

	private void OnGlassDisabled(object sender, GlassDisabledEventArgs e)
	{
		if (e.GlassIndex >= 0 && e.GlassIndex < RobberyData.GlassCoords_.Length && e.Broken && e.PlayerId != Game.Player.ServerId)
		{
			BreakGlass(e.GlassIndex);
		}
	}

	private void BreakGlass(int glassIndex)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		Vector4 val = RobberyData.GlassCoords.ToList()[glassIndex];
		string[][] glassModelSwaps = RobberyData.GlassModelSwaps;
		foreach (string[] array in glassModelSwaps)
		{
			API.CreateModelSwap(val.X, val.Y, val.Z, 0.7f, (uint)API.GetHashKey(array[0]), (uint)API.GetHashKey(array[1]), false);
		}
	}

	[EventHandler("gtacnr:businesses:robberies:jewelry:closeGates")]
	private async void OnCloseGates(int token, string jNetIds)
	{
		List<int> netIds = jNetIds.Unjson<List<int>>();
		bool flag = await HandleGates(netIds, open: false);
		BaseScript.TriggerServerEvent("gtacnr:businesses:robberies:jewelry:closeGates:response", new object[2] { token, flag });
	}

	[EventHandler("gtacnr:businesses:robberies:jewelry:openGates")]
	private async void OnOpenGates(int token, string jNetIds)
	{
		List<int> netIds = jNetIds.Unjson<List<int>>();
		bool flag = await HandleGates(netIds, open: true);
		BaseScript.TriggerServerEvent("gtacnr:businesses:robberies:jewelry:openGates:response", new object[2] { token, flag });
	}

	private async Task<bool> HandleGates(List<int> netIds, bool open)
	{
		int idx = 0;
		foreach (JewelryRobberyGate gateInfo in RobberyData.Gates)
		{
			Prop gate;
			do
			{
				if (!API.NetworkDoesEntityExistWithNetworkId(netIds[idx]))
				{
					return false;
				}
				Entity obj = Entity.FromNetworkId(netIds[idx]);
				gate = (Prop)(object)((obj is Prop) ? obj : null);
				await BaseScript.Delay(10);
			}
			while ((Entity)(object)gate == (Entity)null || !gate.Exists());
			int tries = 0;
			do
			{
				if (tries > 25)
				{
					return false;
				}
				API.NetworkRequestControlOfEntity(((PoolObject)gate).Handle);
				tries++;
				await BaseScript.Delay(10);
			}
			while (!API.NetworkHasControlOfEntity(((PoolObject)gate).Handle));
			API.SetEntityAsMissionEntity(((PoolObject)gate).Handle, true, true);
			((Entity)gate).IsPositionFrozen = true;
			((Entity)gate).IsInvincible = true;
			((Entity)gate).IsCollisionProof = true;
			((Entity)gate).Heading = gateInfo.Heading;
			SlideGates(gateInfo, gate, open);
			idx++;
		}
		return true;
	}

	private async void SlideGates(JewelryRobberyGate gateInfo, Prop prop, bool open)
	{
		Vector3 coords = (open ? gateInfo.StartCoords : gateInfo.EndCoords);
		float speed = RobberyData.GateSpeed;
		bool finished;
		do
		{
			finished = API.SlideObject(((PoolObject)prop).Handle, coords.X, coords.Y, coords.Z, speed, speed, speed, false);
			await BaseScript.Delay(0);
		}
		while (!finished);
	}

	private void LeaveOrangeArea()
	{
		BaseScript.TriggerServerEvent("gtacnr:businesses:robberies:jewelry:leavingArea", new object[0]);
		State.ChangePhase(RobberyPhase.LeavingArea);
		AttachLeaveAreaTask();
	}

	private void AttachLeaveAreaTask()
	{
		if (!isLeaveAreaTaskAttached)
		{
			isLeaveAreaTaskAttached = true;
			base.Update += LeaveAreaTask;
		}
	}

	private void DetachLeaveAreaTask()
	{
		if (isLeaveAreaTaskAttached)
		{
			isLeaveAreaTaskAttached = false;
			base.Update -= LeaveAreaTask;
		}
	}

	private async Coroutine LeaveAreaTask()
	{
		await BaseScript.Delay(500);
		if (!State.Player.IsParticipating || State.Player.Phase != RobberyPhase.LeavingArea)
		{
			DetachLeaveAreaTask();
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		if (((Vector3)(ref position)).DistanceToSquared2D(JewelryStore.Location) >= RobberyData.LeaveAreaRadius.Square())
		{
			ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:businesses:robberies:jewelry:leftRobberyArea");
			if (responseCode != ResponseCode.Success)
			{
				Utils.DisplayError(responseCode, "", "LeaveAreaTask");
				DetachLeaveAreaTask();
			}
			else
			{
				State.ChangePhase(RobberyPhase.GoingToHideout);
				DetachLeaveAreaTask();
			}
		}
	}

	private async void OnWarehouseEntered(object sender, WarehouseEventArgs e)
	{
		if (State.Player.Phase != RobberyPhase.GoingToHideout)
		{
			return;
		}
		int takenItemsCount = State.Player.TakenItemsCount;
		ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:businesses:robberies:jewelry:complete", 1, e.WarehouseId, e.WarehouseOwnerId);
		switch (responseCode)
		{
		case ResponseCode.Success:
			if (e.WarehouseOwnerId == Game.Player.ServerId)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_SUCCESS, takenItemsCount));
			}
			else
			{
				PlayerState playerState = LatentPlayers.Get(e.WarehouseOwnerId);
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_SUCCESS_OTHERS_HIDEOUT, takenItemsCount, playerState?.ColorNameAndId));
			}
			Game.PlaySound("UNDER_THE_BRIDGE", "HUD_AWARDS");
			State.ChangePhase(RobberyPhase.Finished);
			JewelryRobberyScript.RobberyCompleted?.Invoke(this, new EventArgs());
			return;
		case ResponseCode.OwnerOffline:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_PROPERTY_OWNER_OFFLINE));
			WarehouseScript.ExitWarehouse();
			return;
		case ResponseCode.InventoryError:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_SUCCESS_WITH_ERROR));
			Utils.PlayErrorSound();
			break;
		case ResponseCode.InvalidAmount:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_NO_ITEMS_TAKEN), playSound: false);
			Utils.PlayErrorSound();
			break;
		default:
			Utils.DisplayError(responseCode, "", "OnWarehouseEntered");
			break;
		}
		State.ChangePhase(RobberyPhase.Finished);
		JewelryRobberyScript.RobberyFailed?.Invoke(this, new EventArgs());
	}
}
