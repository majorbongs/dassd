using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Data;
using Gtacnr.Model;

namespace Gtacnr.Client.Weapons;

public class CustomWeaponTexturesScript : Script
{
	private static Dictionary<string, Dictionary<string, string>> customWeaponTextures;

	protected override async void OnStarted()
	{
		Chat.AddSuggestion("/clearcustomweapontextures", "Clears your custom textures.");
	}

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		customWeaponTextures = Preferences.CustomWeaponTextures.Get();
		foreach (KeyValuePair<string, Dictionary<string, string>> customWeaponTexture in customWeaponTextures)
		{
			WeaponDefinition weapon = Gtacnr.Data.Items.GetWeaponDefinition(customWeaponTexture.Key);
			if (weapon == null || weapon.TextureInfo == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, string> item in customWeaponTexture.Value)
			{
				await weapon.TextureInfo.ReplaceTexture(item.Key, item.Value);
			}
		}
	}

	public static void UpdateTextures(string weaponID, Dictionary<string, string> textures)
	{
		if (textures.Count != 0)
		{
			customWeaponTextures[weaponID] = textures;
		}
		else
		{
			customWeaponTextures.Remove(weaponID);
		}
		Preferences.CustomWeaponTextures.Set(customWeaponTextures);
	}

	[Command("clearcustomweapontextures")]
	private void ClearCustomWeaponTextures()
	{
		customWeaponTextures.Clear();
		Preferences.CustomWeaponTextures.Set(customWeaponTextures);
	}
}
