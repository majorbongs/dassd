using System.Collections.Generic;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;

namespace Gtacnr.Client.Characters;

public class PvpScript : Script
{
	private static uint civiliansGroupHash = 0u;

	private static uint emergencyGroupHash = 0u;

	private static uint serviceNpcsGroupHash = 0u;

	private static uint employeesGroupHash = 0u;

	private static List<uint> gangGroups = new List<uint>
	{
		(uint)API.GetHashKey("AMBIENT_GANG_BALLAS"),
		(uint)API.GetHashKey("AMBIENT_GANG_FAMILY"),
		(uint)API.GetHashKey("AMBIENT_GANG_LOST"),
		(uint)API.GetHashKey("AMBIENT_GANG_MARABUNTE"),
		(uint)API.GetHashKey("AMBIENT_GANG_MEXICAN"),
		(uint)API.GetHashKey("AMBIENT_GANG_SALVA"),
		(uint)API.GetHashKey("AMBIENT_GANG_WEICHENG"),
		(uint)API.GetHashKey("AMBIENT_GANG_HILLBILLY"),
		(uint)API.GetHashKey("GANG_1"),
		(uint)API.GetHashKey("GANG_2"),
		(uint)API.GetHashKey("GANG_9"),
		(uint)API.GetHashKey("GANG_10")
	};

	private static List<uint> publicServiceGroups = new List<uint>
	{
		(uint)API.GetHashKey("COP"),
		(uint)API.GetHashKey("SECURITY_GUARD"),
		(uint)API.GetHashKey("FIREMAN"),
		(uint)API.GetHashKey("MEDIC"),
		(uint)API.GetHashKey("ARMY"),
		(uint)API.GetHashKey("ARMY"),
		(uint)API.GetHashKey("GUARD_DOG"),
		(uint)API.GetHashKey("PRIVATE_SECURITY")
	};

	public static uint CiviliansGroupHash => civiliansGroupHash;

	public static uint EmergencyGroupHash => emergencyGroupHash;

	public static uint ServiceNpcsGroupHash => serviceNpcsGroupHash;

	public static uint EmployeesGroupHash => employeesGroupHash;

	public PvpScript()
	{
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	protected override void OnStarted()
	{
		API.AddRelationshipGroup("civilians", ref civiliansGroupHash);
		API.AddRelationshipGroup("emergency", ref emergencyGroupHash);
		API.AddRelationshipGroup("serviceNpcs", ref serviceNpcsGroupHash);
		API.AddRelationshipGroup("employees", ref employeesGroupHash);
		API.NetworkSetFriendlyFireOption(false);
		API.SetCanAttackFriendly(API.PlayerPedId(), false, false);
		API.SetRelationshipBetweenGroups(3, civiliansGroupHash, emergencyGroupHash);
		API.SetRelationshipBetweenGroups(3, civiliansGroupHash, civiliansGroupHash);
		API.SetRelationshipBetweenGroups(3, civiliansGroupHash, serviceNpcsGroupHash);
		API.SetRelationshipBetweenGroups(3, civiliansGroupHash, employeesGroupHash);
		API.SetRelationshipBetweenGroups(0, emergencyGroupHash, emergencyGroupHash);
		API.SetRelationshipBetweenGroups(3, emergencyGroupHash, civiliansGroupHash);
		API.SetRelationshipBetweenGroups(0, emergencyGroupHash, serviceNpcsGroupHash);
		API.SetRelationshipBetweenGroups(0, emergencyGroupHash, employeesGroupHash);
		API.SetRelationshipBetweenGroups(0, serviceNpcsGroupHash, emergencyGroupHash);
		API.SetRelationshipBetweenGroups(0, serviceNpcsGroupHash, civiliansGroupHash);
		API.SetRelationshipBetweenGroups(0, serviceNpcsGroupHash, serviceNpcsGroupHash);
		API.SetRelationshipBetweenGroups(0, serviceNpcsGroupHash, employeesGroupHash);
		API.SetRelationshipBetweenGroups(0, employeesGroupHash, emergencyGroupHash);
		API.SetRelationshipBetweenGroups(3, employeesGroupHash, civiliansGroupHash);
		API.SetRelationshipBetweenGroups(0, employeesGroupHash, serviceNpcsGroupHash);
		API.SetRelationshipBetweenGroups(0, employeesGroupHash, employeesGroupHash);
		foreach (uint gangGroup in gangGroups)
		{
			API.SetRelationshipBetweenGroups(3, emergencyGroupHash, gangGroup);
			API.SetRelationshipBetweenGroups(1, gangGroup, emergencyGroupHash);
			API.SetRelationshipBetweenGroups(0, employeesGroupHash, gangGroup);
			API.SetRelationshipBetweenGroups(0, gangGroup, employeesGroupHash);
		}
		foreach (uint publicServiceGroup in publicServiceGroups)
		{
			API.SetRelationshipBetweenGroups(0, emergencyGroupHash, publicServiceGroup);
			API.SetRelationshipBetweenGroups(0, publicServiceGroup, emergencyGroupHash);
		}
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		bool flag = e.CurrentJobEnum.IsPublicService();
		API.SetPedRelationshipGroupHash(API.PlayerPedId(), flag ? emergencyGroupHash : civiliansGroupHash);
	}
}
