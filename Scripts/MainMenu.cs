using Godot;

public partial class MainMenu : Control
{
	[Export] private Label stashLabel = null!;
	[Export] private OptionButton worldSelector = null!;
	[Export] private Button upgradeStorageButton = null!;
	[Export] private Button upgradeSpeedButton = null!;
	[Export] private Button removeWorldLayerButton = null!;
	[Export] private Button startMissionButton = null!;
	[Export] private Button unlockWorldButton = null!;

	[Export] private Godot.Collections.Array<WorldDefinition> availableWorlds = new();

	public override void _Ready()
	{
		upgradeStorageButton.Pressed += UpgradeStorage;
		upgradeSpeedButton.Pressed += UpgradeSpeed;
		removeWorldLayerButton.Pressed += RemoveWorldLayer;
		startMissionButton.Pressed += StartMission;
		unlockWorldButton.Pressed += UnlockSelectedWorld;
		worldSelector.ItemSelected += SelectWorld;

		PopulateWorldSelector();
	}

	private void PopulateWorldSelector()
	{
		worldSelector.Clear();

		for (int index = 0; index < availableWorlds.Count; index++)
		{
			WorldDefinition world = availableWorlds[index];

			var gameState = GetNode<GameState>("/root/GameState");

			string label = gameState.IsWorldUnlocked(world)
				? world.DisplayName
				: $"Locked: {world.DisplayName}";

			worldSelector.AddItem(label);
		}

		if (availableWorlds.Count == 0)
		{
			startMissionButton.Disabled = true;
			return;
		}

		worldSelector.Select(0);
		SelectWorld(0);
	}
	private void UnlockSelectedWorld()
	{
		var gameState = GetNode<GameState>("/root/GameState");

		gameState.TryUnlockSelectedWorld();

		UpdateMenu();
	}
	private void SelectWorld(long selectedIndex)
	{
		var gameState = GetNode<GameState>("/root/GameState");

		gameState.SelectWorld(availableWorlds[(int)selectedIndex]);

		UpdateMenu();
	}

	private void StartMission()
	{
		GetTree().ChangeSceneToFile("res://Scenes/Missions/WorldRun.tscn");
	}

	private void UpgradeStorage()
	{
		var gameState = GetNode<GameState>("/root/GameState");

		gameState.TryUpgradeStorage();
		UpdateMenu();
	}

	private void UpgradeSpeed()
	{
		var gameState = GetNode<GameState>("/root/GameState");

		gameState.TryUpgradeSelectedWorldSpeed();
		UpdateMenu();
	}

	private void RemoveWorldLayer()
	{
		var gameState = GetNode<GameState>("/root/GameState");

		gameState.TryRemoveSelectedWorldLayer();
		UpdateMenu();
	}

	private void UpdateMenu()
	{
		var gameState = GetNode<GameState>("/root/GameState");
		bool worldIsUnlocked = gameState.IsSelectedWorldUnlocked();

		stashLabel.Text =
			$"Stash: ${gameState.StashMoney}\n" +
			$"Miner storage: {gameState.MinerStorageCapacity}\n" +
			$"Miner speed: {gameState.SelectedWorldMinerSpeedMultiplier:0.0}x";

		upgradeStorageButton.Text =
			$"Increase storage by 25 (${gameState.NextStorageUpgradeCost})";

		upgradeStorageButton.Disabled =
			!gameState.CanUpgradeStorage ||
			gameState.StashMoney <
			gameState.NextStorageUpgradeCost;

		upgradeSpeedButton.Text =
			$"Increase miner speed by 20% (${gameState.NextSelectedWorldSpeedUpgradeCost})";

		upgradeSpeedButton.Disabled =
			!gameState.CanUpgradeSelectedWorldSpeed ||
			gameState.StashMoney <
			gameState.NextSelectedWorldSpeedUpgradeCost;

		removeWorldLayerButton.Text =
			$"Excavate {gameState.SelectedWorld.DisplayName} by 1 layer " +
			$"(${gameState.NextSelectedWorldLayerRemovalCost})";

		removeWorldLayerButton.Disabled =
			!worldIsUnlocked ||
			!gameState.CanRemoveSelectedWorldLayer ||
			gameState.StashMoney <
			gameState.NextSelectedWorldLayerRemovalCost;
		
		startMissionButton.Text =
		$"Start {gameState.SelectedWorld.DisplayName}";

		

		startMissionButton.Text = worldIsUnlocked
			? $"Start {gameState.SelectedWorld.DisplayName}"
			: $"{gameState.SelectedWorld.DisplayName} is locked";

		startMissionButton.Disabled = !worldIsUnlocked;

		unlockWorldButton.Visible = !worldIsUnlocked;

		unlockWorldButton.Text =
			$"Unlock {gameState.SelectedWorld.DisplayName} " +
			$"(${gameState.SelectedWorld.UnlockCost})";

		unlockWorldButton.Disabled =
			worldIsUnlocked ||
			gameState.StashMoney < gameState.SelectedWorld.UnlockCost;
	}
}
