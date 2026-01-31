using System;

namespace Gtacnr.Model;

public class Transaction
{
	public string Id { get; set; }

	public string CharacterId { get; set; }

	public string Account { get; set; }

	public DateTime DateTime { get; set; }

	public long Amount { get; set; }

	public string Description { get; set; }

	public string LinkedTransactionId { get; set; }

	public Transaction()
	{
	}

	public Transaction(Character character, string account, long amount, string description)
	{
		CharacterId = character.Id;
		Account = account;
		Amount = amount;
		Description = description;
	}

	public Transaction(string characterId, string account, long amount, string description)
	{
		CharacterId = characterId;
		Account = account;
		Amount = amount;
		Description = description;
	}

	public override string ToString()
	{
		return $"{Amount} - {Description}";
	}
}
