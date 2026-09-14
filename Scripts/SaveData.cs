using System.Collections.Generic;

public sealed class SaveData
{
	public int StashMoney { get; set; }
	public int StorageUpgradeLevel { get; set; }

	public List<string> UnlockedWorldIds { get; set; } = new();

	public Dictionary<string, WorldProgressSaveData>
		WorldProgressById { get; set; } = new();
}
