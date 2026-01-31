using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using Gtacnr.Client;
using Gtacnr.Client.Premium;

namespace Gtacnr.Model;

public class WeaponTextureInfo
{
	public string Txd;

	public List<string> TxdHighItems;

	public string DiffuseMap;

	public string NormalMap;

	public string SpecularMap;

	public List<string> OtherTextures;

	private string RuntimeTxdName => Txd + "_runtime";

	public async Task ReplaceTexture(string textureName, string base64Texture)
	{
		if ((int)MembershipScript.GetCurrentMembershipTier() < 2)
		{
			return;
		}
		long runtimeTxd = API.CreateRuntimeTxd(RuntimeTxdName);
		while (!API.HasStreamedTextureDictLoaded(RuntimeTxdName))
		{
			await Gtacnr.Client.Utils.Delay();
		}
		API.CreateRuntimeTextureFromImage(runtimeTxd, textureName, base64Texture);
		while (!API.HasStreamedTextureDictLoaded(Txd))
		{
			await Gtacnr.Client.Utils.Delay();
		}
		API.AddReplaceTexture(Txd, textureName, RuntimeTxdName, textureName);
		if (TxdHighItems.Contains(textureName))
		{
			while (!API.HasStreamedTextureDictLoaded(Txd + "+hi"))
			{
				await Gtacnr.Client.Utils.Delay();
			}
			API.AddReplaceTexture(Txd + "+hi", textureName, RuntimeTxdName, textureName);
		}
		API.SetStreamedTextureDictAsNoLongerNeeded(RuntimeTxdName);
	}
}
