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
	[Export] private Button unlockAutorunButton = null!;
	[Export] private CheckButton autoRunCheckButton = null!;
	[Export] private Label autoRunChargesLabel = null!;

	[Export] private Godot.Collections.Array<WorldDefinition> availableWorlds = new();

	private bool autoRunPending;
	private float autoRunTimer;
	private const float AutoRunCountdownSeconds = 5f;

	public override void _Ready()
	{
		upgradeStorageButton.Pressed += UpgradeStorage;
		upgradeSpeedButton.Pressed += UpgradeSpeed;
		removeWorldLayerButton.Pressed += RemoveWorldLayer;
		startMissionButton.Pressed += OnStartMissionPressed;
		unlockWorldButton.Pressed += UnlockSelectedWorld;
		worldSelector.ItemSelected += SelectWorld;
		autoRunCheckButton.Toggled += OnAutoRunToggled;
		unlockAutorunButton.Pressed += UnlockSelectedWorldAutoRun;
		PopulateWorldSelector();
		TryBeginAutoRunCountdown();
	}
	public override void _Process(double delta)
	{
		if (!autoRunPending)
			return;

		autoRunTimer -= (float)delta;

		if (autoRunTimer <= 0f)
		{
			autoRunPending = false;
			FireAutoRun();
		}
	}

	private void OnStartMissionPressed()
	{
		GetNode<GameState>("/root/GameState").PrepareManualRun();
		StartMission();
	}

	private void TryBeginAutoRunCountdown()
	{
		var gameState = GetNode<GameState>("/root/GameState");

		if (!gameState.IsSelectedWorldAutoUnlocked() || !gameState.IsAutoRunActive)
		{
			autoRunPending = false;
			return;
		}

		if (gameState.AutoRunCharges <= 0)
		{
			gameState.SetAutoRunActive(false); // out of charges -> turn autorun off for real
			UpdateMenu(); // reflect the toggle turning off immediately
			autoRunPending = false;
			return;
		}

		autoRunPending = true;
		autoRunTimer = AutoRunCountdownSeconds;
	}

	private void FireAutoRun()
	{
		var gameState = GetNode<GameState>("/root/GameState");

		if (!gameState.TryConsumeAutoRunCharge())
			return;

		gameState.PrepareAutoRun();
		StartMission();
	}

	private void OnAutoRunToggled(bool toggledOn)
	{
		var gameState = GetNode<GameState>("/root/GameState");

		if (toggledOn && gameState.AutoRunCharges <= 0)
		{
			autoRunCheckButton.SetPressedNoSignal(false);
			return;
		}

		gameState.SetAutoRunActive(toggledOn);
		TryBeginAutoRunCountdown();
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

		int selectedIndex = 0;

		for (int index = 0; index < availableWorlds.Count; index++)
		{
			if (availableWorlds[index].WorldId ==
				GetNode<GameState>("/root/GameState").SelectedWorldId)
			{
				selectedIndex = index;
				break;
			}
		}

		worldSelector.Select(selectedIndex);
		SelectWorld(selectedIndex);
	}
	private void UnlockSelectedWorld()
	{
		var gameState = GetNode<GameState>("/root/GameState");

		gameState.TryUnlockSelectedWorld();

		UpdateMenu();
	}
	private void UnlockSelectedWorldAutoRun()
	{
		var gameState = GetNode<GameState>("/root/GameState");

		gameState.TryUnlockSelectedWorldAutoRun();

		UpdateMenu();
		TryBeginAutoRunCountdown();
	}
	private void SelectWorld(long selectedIndex)
	{
		var gameState = GetNode<GameState>("/root/GameState");

		gameState.SelectWorld(availableWorlds[(int)selectedIndex]);

		UpdateMenu();
		TryBeginAutoRunCountdown();
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
		bool autoRunIsUnlocked = gameState.IsSelectedWorldAutoUnlocked();
		bool hasCharges = gameState.AutoRunCharges > 0;

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
			$"Increase miner speed by 25% (${gameState.NextSelectedWorldSpeedUpgradeCost})";

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

		unlockAutorunButton.Visible = !autoRunIsUnlocked;

		unlockAutorunButton.Text =
			$"Unlock autorun for {gameState.SelectedWorld.DisplayName} " +
			$"(${gameState.SelectedWorld.AutorunUnlockCost})";

		unlockAutorunButton.Disabled =
			autoRunIsUnlocked ||
			gameState.StashMoney < gameState.SelectedWorld.AutorunUnlockCost;

		autoRunCheckButton.Visible = autoRunIsUnlocked;
		autoRunChargesLabel.Visible = autoRunIsUnlocked;
		
		autoRunCheckButton.SetPressedNoSignal(gameState.IsAutoRunActive);
		autoRunCheckButton.Disabled = !autoRunIsUnlocked || gameState.AutoRunCharges <= 0;

		autoRunChargesLabel.Text = $"Autorun charges: {gameState.AutoRunCharges}";
	}
}
