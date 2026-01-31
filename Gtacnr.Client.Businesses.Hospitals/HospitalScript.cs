using System.Collections.Generic;
using System.Linq;
using Gtacnr.Model;

namespace Gtacnr.Client.Businesses.Hospitals;

public class HospitalScript : Script
{
	public static List<FireDepartment> Departments { get; set; } = Gtacnr.Utils.LoadJson<List<FireDepartment>>("data/paramedic/fireDepartments.json");

	public static IEnumerable<Hospital> Hospitals => from b in BusinessScript.Businesses.Values
		where b.Hospital != null
		select b.Hospital;

	public static Hospital? CurrentHospital => BusinessScript.ClosestBusiness?.Hospital;
}
