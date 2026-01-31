using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.Premium;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.API;

public static class Clothes
{
	private class ClothesScript : Script
	{
		private static readonly Dictionary<string, HashSet<string>> ownedClothesCache = new Dictionary<string, HashSet<string>>();

		private static ClothesScript instance;

		public ClothesScript()
		{
			instance = this;
		}

		public static async Task<HashSet<string>> GetAllOwned(string job = "none", bool force = false)
		{
			if (!ownedClothesCache.ContainsKey(job) || force)
			{
				Job jobData = Gtacnr.Data.Jobs.GetJobData(job);
				if (jobData == null || !jobData.SeparateOutfit)
				{
					job = "none";
				}
				string text = await instance.TriggerServerEventAsync<string>("gtacnr:clothes:get", new object[2] { null, job });
				ownedClothesCache[job] = ((!string.IsNullOrEmpty(text)) ? text.Unjson<HashSet<string>>() : new HashSet<string>());
			}
			return ownedClothesCache[job];
		}

		public static async Task<bool> Owns(string itemId, string job = "none")
		{
			if (!ownedClothesCache.ContainsKey(job))
			{
				await GetAllOwned(job);
			}
			return ownedClothesCache[job].Contains(itemId);
		}

		public static bool HasJobBeenCached(string job)
		{
			if (string.IsNullOrEmpty(job))
			{
				return false;
			}
			return ownedClothesCache.ContainsKey(job);
		}

		[EventHandler("gtacnr:jobs:jobChanged")]
		private async void OnJobChanged(string previousJob, string currentJob)
		{
			HashSet<string> value = await GetAllOwned(currentJob);
			ownedClothesCache[currentJob] = value;
		}

		[EventHandler("gtacnr:clothes:obtained")]
		private void OnClothingItemObtained(string job, string clothingId)
		{
			if (!ownedClothesCache.ContainsKey(job))
			{
				ownedClothesCache[job] = new HashSet<string>();
			}
			if (!ownedClothesCache[job].Contains(clothingId))
			{
				ownedClothesCache[job].Add(clothingId);
			}
		}

		[EventHandler("gtacnr:clothes:removed")]
		private void OnClothingItemRemoved(string job, string clothingId)
		{
			if (!ownedClothesCache.ContainsKey(job))
			{
				ownedClothesCache[job] = new HashSet<string>();
			}
			if (ownedClothesCache[job].Contains(clothingId))
			{
				ownedClothesCache[job].Remove(clothingId);
			}
		}

		[EventHandler("gtacnr:clothes:apply")]
		private void OnApplyApparel(string apparelJson)
		{
			if (!string.IsNullOrEmpty(apparelJson))
			{
				HashSet<string> hashSet = new HashSet<string>(apparelJson.Unjson<List<string>>());
				if (CustomScript.DataLoaded)
				{
					hashSet.ExceptWith(CustomScript.GetUnauthorizedClothingIds());
				}
				CurrentApparel = new Apparel(hashSet);
			}
		}
	}

	private static Apparel _currentApparel;

	private static bool isSaveInProgress;

	public static Apparel CurrentApparel
	{
		get
		{
			return _currentApparel;
		}
		set
		{
			if (_currentApparel == null)
			{
				_currentApparel = new Apparel();
				_currentApparel.Changed += OnCurrentApparelChanged;
			}
			_currentApparel.Set(value ?? Apparel.GetDefault(Jobs.CachedJob, MainScript.SelectedCharacter?.Sex ?? Sex.Male));
		}
	}

	private static void OnCurrentApparelChanged(object sender, ApparelChangedEventArgs e)
	{
		Apparel.ClearFromPed(Game.PlayerPed, clearMakeup: true, clearDecorations: true, clearDamage: true);
		_currentApparel.ApplyOnPlayer();
		BaseScript.TriggerEvent("gtacnr:apparelChanged", new object[0]);
	}

	public static async Task<HashSet<string>> GetAllOwned(string job = "none", bool force = false)
	{
		return await ClothesScript.GetAllOwned(job, force);
	}

	public static async Task<bool> Owns(string clothingId, string job = "none")
	{
		return await ClothesScript.Owns(clothingId);
	}

	public static async Task SaveApparel()
	{
		if (isSaveInProgress)
		{
			return;
		}
		try
		{
			Utils.ClearPedDamage(Game.PlayerPed);
			isSaveInProgress = true;
			string text = Jobs.CachedJob;
			if (!Gtacnr.Data.Jobs.GetJobData(text).SeparateOutfit)
			{
				text = "none";
			}
			if (await Outfits.SaveOutfit(text, 0, _currentApparel))
			{
				Character character = await Characters.GetActiveCharacter();
				if (character != null)
				{
					character.Apparel = _currentApparel;
					if (!(await Characters.Update(character.Slot, character)))
					{
						Debug.WriteLine("[Clothes] An unexpected error has occurred while saving the current apparel.");
						Utils.SendNotification(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x10"));
					}
				}
				else
				{
					Debug.WriteLine("[Clothes] An unexpected error has occurred while saving the current apparel.");
					Utils.SendNotification(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x11"));
				}
			}
			else
			{
				Debug.WriteLine("[Clothes] An unexpected error has occurred while saving the current apparel.");
				Utils.SendNotification(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x12"));
			}
		}
		finally
		{
			isSaveInProgress = false;
		}
	}
}
