using System.ComponentModel;

namespace Gtacnr.Model.Enums;

public enum ResponseCode
{
	[Description("The server did not respond on time")]
	Timeout = 0,
	[Description("No errors")]
	Success = 1,
	[Description("An unhandled error has occurred on the server")]
	GenericError = 2,
	[Description("The server is still processing your last request of this type")]
	Busy = 3,
	[Description("You are not authorized to perform this action")]
	Unauthorized = 4,
	[Description("The server has imposed a temporary rate limit on this client for this action")]
	RateLimited = 5,
	[Description("The server has imposed a cooldown on this client for this action")]
	Cooldown = 6,
	[Description("The requested resource is unavailable at this moment")]
	Unavailable = 7,
	[Description("The resulting state is the same as the original state")]
	SameState = 8,
	[Description("A server-side script has canceled the action")]
	ScriptCanceled = 9,
	[Description("A client-side script has canceled the action")]
	ClientScriptCanceled = 10,
	[Description("A generic error has occurred in the database")]
	DatabaseError = 11,
	[Description("The server returned no data")]
	NoData = 12,
	[Description("A multi-step process is already in progress for this client")]
	InProgress = 13,
	[Description("A multi-step process is already in progress for the target player of this action")]
	TargetInProgress = 14,
	[Description("The target of this action can't be yourself")]
	SelfTarget = 15,
	[Description("This unique resource or identifier has already been taken")]
	Taken = 16,
	[Description("This action cannot be taken while muted")]
	Muted = 17,
	[Description("Text contains spam or bad words")]
	SpamFilter = 18,
	[Description("Text is too short")]
	TooShort = 19,
	[Description("Text is too long")]
	TooLong = 20,
	[Description("You are blacklisted from performing this action")]
	Blacklisted = 100,
	[Description("You are not whitelisted to perform this action")]
	NotWhitelisted = 101,
	[Description("You are on a job that is not allowed for this action")]
	JobNotAllowed = 102,
	[Description("You are on a certain job, but the requested resource is for another specific job")]
	JobMismatch = 103,
	[Description("You don't meet the level requirement for this action")]
	InsufficientMoney = 104,
	[Description("You don't meet the level requirement for this action")]
	InsufficientLevel = 105,
	[Description("You don't meet the playtime requirement for this action")]
	InsufficientPlaytime = 106,
	[Description("You don't meet the membership requirement for this action")]
	InsufficientMembershipTier = 107,
	[Description("You don't meet the wanted level requirement for this action")]
	WantedLevel = 108,
	[Description("You must pass a test before performing this action")]
	TestRequired = 109,
	[Description("There must be police officers around you in order to perform this action, but there are none")]
	NoCopsAround = 110,
	[Description("There's a limit to the number of players that can perform this action")]
	PlayerLimit = 111,
	[Description("You are physically too distant from the location the server expects you to be at")]
	TooFar = 112,
	[Description("You are physically too close to the location the server expects you to be far from")]
	TooClose = 113,
	[Description("The vehicle is physically too distant from the location the server expects it to be at")]
	VehicleTooFar = 114,
	[Description("The specified vehicle is a rented vehicle while it shouldn't be")]
	RentedVehicle = 115,
	[Description("The primary object of this action is invalid, unacceptable or doesn't exist")]
	Invalid = 200,
	[Description("The specified amount is invalid or unacceptable")]
	InvalidAmount = 201,
	[Description("The specified ped doesn't exist")]
	InvalidPed = 202,
	[Description("The specified vehicle doesn't exist")]
	InvalidVehicle = 203,
	[Description("The specified prop doesn't exist")]
	InvalidProp = 204,
	[Description("The specified player isn't connected")]
	InvalidPlayer = 205,
	[Description("The specified source entity is invalid, unacceptable, doesn't exist, or isn't connected")]
	InvalidSource = 206,
	[Description("The specified target entity is invalid, unacceptable, doesn't exist, or isn't connected")]
	InvalidTarget = 207,
	[Description("The specified user doesn't exist")]
	InvalidUser = 208,
	[Description("The specified character doesn't exist")]
	InvalidCharacter = 209,
	[Description("The specified destination entity is invalid or unacceptable")]
	InvalidDestination = 210,
	[Description("The specified item doesn't exist")]
	InvalidItem = 211,
	[Description("The specified business doesn't exist")]
	InvalidBusiness = 212,
	[Description("The specified property doesn't exist")]
	InvalidProperty = 213,
	[Description("The specified job doesn't exist")]
	InvalidJob = 214,
	[Description("The specified vehicle model doesn't exist")]
	InvalidVehicleModel = 215,
	[Description("The specified routing bucket is invalid or unacceptable")]
	InvalidRoutingBucket = 216,
	[Description("The specified vehicle data is invalid")]
	InvalidVehicleData = 217,
	[Description("The specified name is invalid or unacceptable")]
	InvalidName = 218,
	[Description("The specified email is invalid or unacceptable")]
	InvalidEmail = 219,
	[Description("The specified identifier type is invalid or unacceptable")]
	InvalidIdentifierType = 220,
	[Description("The specified supply is invalid")]
	InvalidSupply = 221,
	[Description("The specified supply data is invalid")]
	InvalidSupplyData = 222,
	[Description("The specified offer doesn't exist")]
	InvalidOffer = 223,
	[Description("The specified drug corner doesn't exist")]
	InvalidCorner = 224,
	[Description("The specified drug doesn't exist")]
	InvalidDrug = 225,
	[Description("The specified weapon doesn't exist")]
	InvalidWeapon = 226,
	[Description("The specified drop doesn't exist")]
	InvalidDrop = 227,
	[Description("The specified collectible doesn't exist")]
	InvalidCollectible = 228,
	[Description("The specified wanted level is invalid or unacceptable")]
	InvalidWantedLevel = 229,
	[Description("The specified index is out of bounds")]
	InvalidIndex = 230,
	[Description("A generic error has occurred in the Accounts API")]
	APIError = 300,
	[Description("This user is already registered")]
	AlreadyRegistered = 301,
	[Description("The username cannot be empty")]
	EmptyUsername = 302,
	[Description("You cannot perform this action when you are using a fake username")]
	FakeUsername = 303,
	[Description("The rename action failed for some reason")]
	RenameFailed = 304,
	[Description("Staff members cannot change their names on their own")]
	StaffRename = 305,
	[Description("Rename will use a rename token")]
	WillUseToken = 306,
	[Description("The entered code is incorrect")]
	IncorrectCode = 307,
	[Description("The entered code has expired")]
	ExpiredCode = 308,
	[Description("The identifier is empty")]
	EmptyIdentifier = 309,
	[Description("This service is already linked")]
	AlreadyLinked = 310,
	[Description("Unable to link this service")]
	UnableToLink = 311,
	[Description("Unable to unlink this service")]
	UnableToUnlink = 312,
	[Description("A generic error during a money transaction")]
	TransactionError = 400,
	[Description("Unable to complete the resource transfer")]
	UnableToTransfer = 401,
	[Description("There is sufficient money to complete a transaction, but not enough to cover the extra fees")]
	CantCoverFees = 402,
	[Description("The rent payment for this resource is overdue")]
	RentOverdue = 403,
	[Description("There are insufficient funds in the account where the money is being taken from")]
	InsufficientFunds = 404,
	[Description("You transferred too much money")]
	TransactionLimit = 405,
	[Description("You transferred too much money recently")]
	TransactionCooldown = 406,
	[Description("The target player received too much money")]
	TransactionTargetLimit = 407,
	[Description("The target player received too much money recently")]
	TransactionTargetCooldown = 408,
	[Description("A generic error during an inventory action")]
	InventoryError = 500,
	[Description("This item cannot be used")]
	CannotUse = 501,
	[Description("This item cannot be given")]
	CannotGive = 502,
	[Description("This item cannot be dropped")]
	CannotDrop = 503,
	[Description("This item cannot be moved between inventories")]
	CannotMove = 504,
	[Description("The limit for this item has been reached")]
	LimitReached = 505,
	[Description("There's no space left in the inventory")]
	NoSpaceLeft = 506,
	[Description("You don't have a sufficient amount of this item")]
	InsufficientAmount = 507,
	[Description("You already own the resource")]
	AlreadyOwned = 508,
	[Description("You don't own the resource")]
	NotOwned = 509,
	[Description("")]
	VehicleTypeNotPermitted = 510,
	[Description("")]
	InsufficientMembershipTierStorage = 511,
	[Description("The seller doesn't have the required certification")]
	NoCertification = 512,
	[Description("The requested item is not for sale at this moment")]
	NotForSaleAtThisMoment = 513,
	[Description("The requested item is out of stock")]
	OutOfStock = 514,
	[Description("This item is already active")]
	AlreadyActive = 515,
	[Description("Unable to deactivate this item")]
	UnableToDeactivate = 516,
	[Description("")]
	AlreadySurrendered = 517,
	[Description("")]
	AttachedToTowtruck = 518,
	[Description("")]
	ItemLimitReached = 519,
	[Description("")]
	Spotted = 520,
	[Description("")]
	AlreadyInMaintenance = 521,
	[Description("")]
	UnableToSaveHealthData = 522,
	[Description("")]
	UnableToDespawn = 523,
	[Description("")]
	CannotBeRobbed = 524,
	[Description("")]
	RecentlyRobbed = 525,
	[Description("")]
	AlreadyInProgress = 526,
	[Description("")]
	Joined = 527,
	[Description("")]
	NotInProgress = 528,
	[Description("")]
	AlreadyInRobbery = 529,
	[Description("Already broken")]
	AlreadyBroken = 530,
	[Description("")]
	NotEnoughCops = 531,
	[Description("")]
	TooEarly = 532,
	[Description("")]
	TooLate = 533,
	[Description("")]
	NotSearched = 534,
	[Description("")]
	NotInCustody = 535,
	[Description("")]
	NotWanted = 536,
	[Description("")]
	NothingFound = 537,
	[Description("")]
	AlreadySearched = 538,
	[Description("")]
	PropDoesntMatch = 539,
	[Description("")]
	TooManyDrops = 540,
	[Description("")]
	ConcurrentPickUp = 541,
	[Description("")]
	SamePed = 542,
	[Description("")]
	TooManyTimes = 543,
	[Description("")]
	AlreadyPickpocketing = 544,
	[Description("")]
	TooManyPlayers = 545,
	[Description("")]
	SnitchCooldown = 546,
	[Description("")]
	ExcessivePrice = 547,
	[Description("")]
	UndercoverCop = 548,
	[Description("")]
	OwnerOffline = 549,
	[Description("")]
	MissingToolkit = 550,
	[Description("")]
	AlreadyInOrOut = 551,
	[Description("")]
	NoWarrant = 552,
	[Description("")]
	RentNotOverdue = 553,
	[Description("")]
	NotARentalVehicle = 554,
	[Description("")]
	TooDamaged = 555,
	[Description("")]
	VehicleNotWanted = 556,
	[Description("")]
	MissionVehicle = 557,
	[Description("")]
	PersonalVehicle = 558,
	[Description("")]
	CopsNearby = 559,
	[Description("")]
	UnableToRegisterVehicle = 560,
	[Description("")]
	UnableToStoreVehicle = 561,
	[Description("")]
	MissingPermission = 562,
	[Description("")]
	VehicleNotOwned = 563,
	[Description("")]
	NotInVehicle = 564,
	[Description("")]
	NoParkingSpace = 565,
	[Description("")]
	InMaintenance = 566,
	[Description("")]
	NoAvailableRoutingBucket = 567,
	[Description("")]
	ForceRespawn = 568,
	[Description("")]
	ForceCallMedic = 569,
	[Description("")]
	AlreadyCollected = 570,
	[Description("")]
	FixUnavailable = 571,
	[Description("")]
	RecentlyAutoCuffed = 572,
	[Description("Already on an existing mission.")]
	AlreadyOnMission = 573,
	[Description("Not on a mission.")]
	NotOnMission = 574,
	[Description("Invalid mission.")]
	InvalidMission = 575,
	[Description("Invalid mission state.")]
	InvalidMissionState = 576,
	[Description("This job is no longer available.")]
	MissionNoLongerAvailable = 577
}
