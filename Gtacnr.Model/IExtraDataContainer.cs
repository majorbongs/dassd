using System.Collections.Generic;

namespace Gtacnr.Model;

public interface IExtraDataContainer
{
	Dictionary<string, object> ExtraData { get; set; }
}
