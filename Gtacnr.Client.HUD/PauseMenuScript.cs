using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Model;

namespace Gtacnr.Client.HUD;

public class PauseMenuScript : Script
{
	protected override async void OnStarted()
	{
		API.AddTextEntry("FE_THDR_GTAO", "~b~Cops ~s~and ~r~Robbers ~s~V");
	}

	public static void Refresh()
	{
		PlayerState playerState = LatentPlayers.Get(Game.Player);
		if (playerState != null)
		{
			API.BeginScaleformMovieMethodOnFrontendHeader("SET_HEADING_DETAILS");
			API.PushScaleformMovieFunctionParameterString(playerState.ColorTextCode + playerState.Name);
			API.PushScaleformMovieFunctionParameterString("CASH " + playerState.Cash.ToCurrencyString());
			API.PushScaleformMovieFunctionParameterString("BANK " + playerState.Bank.ToCurrencyString());
			API.ScaleformMovieMethodAddParamBool(false);
			API.ScaleformMovieMethodAddParamBool(false);
			API.EndScaleformMovieMethod();
		}
	}

	[Update]
	private async Coroutine RefreshTask()
	{
		Refresh();
	}
}
