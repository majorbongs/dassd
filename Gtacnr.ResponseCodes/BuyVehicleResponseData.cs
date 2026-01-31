using Gtacnr.Model;

namespace Gtacnr.ResponseCodes;

public class BuyVehicleResponseData
{
	public BuyItemResponse Code { get; set; } = BuyItemResponse.GenericError;

	public StoredVehicle StoredVehicle { get; set; }
}
