using Godot;
using System.Collections.Generic;
using System;
using System.Text.Json;

public partial class GameState : Node
{
	private const string SavePath = "user://desktop_digger_save.json";
	private readonly HashSet<string> unlockedWorldIds = new();
	
	private const int BaseStorageCapacity = 25;
	private const int StoragePerUpgrade = 25;
	private const int BaseStorageUpgradeCost = 50;
	private const int BaseLayerRemovalCost = 100;

	private const int MaxStorageCapacity = 7200;
	private const int MaxRemovedLayers = 32;
	private const int BaseSpeedUpgradeCost = 75;
	private const float SpeedIncreasePerUpgrade = 0.20f;

	private readonly Dictionary<string, WorldProgress> worldProgress = new();

	public int StashMoney { get; private set; }
	public int StorageUpgradeLevel { get; private set; }
	

	public WorldDefinition SelectedWorld { get; private set; } = null!;
	

	public int MinerStorageCapacity =>
		BaseStorageCapacity + StorageUpgradeLevel * StoragePerUpgrade;

	public int NextStorageUpgradeCost =>
		BaseStorageUpgradeCost * (StorageUpgradeLevel + 1);

	public int SelectedWorldRemovedLayers =>
		GetSelectedWorldProgress().RemovedLayers;

	public int NextSelectedWorldLayerRemovalCost =>
		BaseLayerRemovalCost * (SelectedWorldRemovedLayers + 1);

	public bool CanRemoveSelectedWorldLayer =>
		SelectedWorldRemovedLayers < MaxRemovedLayers;

	public bool CanUpgradeStorage =>
	MinerStorageCapacity < MaxStorageCapacity;

	public int SelectedWorldSpeedUpgradeLevel =>
		GetSelectedWorldProgress().SpeedUpgradeLevel;

	public float SelectedWorldMinerSpeedMultiplier =>
		1f + SelectedWorldSpeedUpgradeLevel * SpeedIncreasePerUpgrade;

	public int NextSelectedWorldSpeedUpgradeCost =>
		BaseSpeedUpgradeCost * (SelectedWorldSpeedUpgradeLevel + 1);

	public bool CanUpgradeSelectedWorldSpeed => true;

	public override void _Ready()
	{
		LoadGame();
	}
	public void SelectWorld(WorldDefinition world)
	{
		SelectedWorld = world;
	}

	public void BankRun(int amount)
	{
		if (amount > 0)
			StashMoney += amount;
		SaveGame();
	}

	public bool TryUpgradeStorage()
	{
		if (!CanUpgradeStorage)
			return false;

		if (StashMoney < NextStorageUpgradeCost)
			return false;

		StashMoney -= NextStorageUpgradeCost;
		StorageUpgradeLevel++;
		SaveGame();

		return true;
	}
	public bool TryUpgradeSelectedWorldSpeed()
	{
		if (StashMoney < NextSelectedWorldSpeedUpgradeCost)
			return false;

		StashMoney -= NextSelectedWorldSpeedUpgradeCost;

		GetSelectedWorldProgress().UpgradeSpeed();

		SaveGame();

		return true;
	}
	public bool TryRemoveSelectedWorldLayer()
	{
		if (!CanRemoveSelectedWorldLayer)
			return false;

		if (StashMoney < NextSelectedWorldLayerRemovalCost)
			return false;

		StashMoney -= NextSelectedWorldLayerRemovalCost;
		GetSelectedWorldProgress().RemoveLayer();
		SaveGame();
		return true;
	}

	private WorldProgress GetSelectedWorldProgress()
	{
		if (!worldProgress.TryGetValue(
			SelectedWorld.WorldId,
			out WorldProgress progress))
		{
			progress = new WorldProgress();
			worldProgress.Add(SelectedWorld.WorldId, progress);
		}

		return progress;
	}
	public bool IsWorldUnlocked(WorldDefinition world)
	{
		return world.UnlockedByDefault ||
			unlockedWorldIds.Contains(world.WorldId);
	}

	public bool IsSelectedWorldUnlocked()
	{
		return IsWorldUnlocked(SelectedWorld);
	}

	public bool TryUnlockSelectedWorld()
	{
		if (IsSelectedWorldUnlocked())
			return true;

		if (StashMoney < SelectedWorld.UnlockCost)
			return false;

		StashMoney -= SelectedWorld.UnlockCost;

		unlockedWorldIds.Add(SelectedWorld.WorldId);

		SaveGame();

		return true;
	}
	private void SaveGame()
	{
		var saveData = new SaveData
		{
			StashMoney = StashMoney,
			StorageUpgradeLevel = StorageUpgradeLevel,
			UnlockedWorldIds = new List<string>(unlockedWorldIds),
		};

		foreach (KeyValuePair<string, WorldProgress> entry in worldProgress)
		{
			saveData.WorldProgressById[entry.Key] =
				new WorldProgressSaveData
				{
					RemovedLayers = entry.Value.RemovedLayers,
					SpeedUpgradeLevel = entry.Value.SpeedUpgradeLevel
				};
		}

		string json = JsonSerializer.Serialize(saveData);

		var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
		file.StoreString(json);
		file.Close();
	}

	private void LoadGame()
	{
		if (!FileAccess.FileExists(SavePath))
			return;

		try
		{
			string json = FileAccess.GetFileAsString(SavePath);

			SaveData saveData = JsonSerializer.Deserialize<SaveData>(json);

			if (saveData == null)
				return;

			StashMoney = saveData.StashMoney;
			StorageUpgradeLevel = saveData.StorageUpgradeLevel;
			if (saveData.UnlockedWorldIds != null)
			{
				foreach (string worldId in saveData.UnlockedWorldIds)
				{
					unlockedWorldIds.Add(worldId);
				}
			}

			foreach (
				KeyValuePair<string, WorldProgressSaveData>
				entry in saveData.WorldProgressById)
			{
				worldProgress[entry.Key] = new WorldProgress(
					entry.Value.RemovedLayers,
					entry.Value.SpeedUpgradeLevel
				);
			}
		}
		catch (Exception exception)
		{
			GD.PushWarning($"Could not load save data: {exception.Message}");
		}
	}
}
