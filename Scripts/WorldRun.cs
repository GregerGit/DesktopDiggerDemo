using Godot;
using System.Collections.Generic;

public partial class WorldRun : Node3D
{
	[Export(PropertyHint.Range, "1, 50, 1")]
	private float simulationSpeed = 1f;
	private Button abortButton = null!;
	
	private MiningRewardCatalog miningRewardCatalog = null!;	

	private const int RenderedLayerCount = 5;
	private const float MinerWalkSpeed = 1.5f;
	private const float MiningDuration = 0.7f;
	private const float MinerHeight = 1.05f;
	
	private Camera3D camera = null!;
	private int currentMiningLayer;
	private float cameraDistanceScale = 0.6f; 

	private RunInventory inventory = null!;
	private Label statusLabel;

	private WorldChunk worldChunk = null!;
	private readonly Dictionary<Vector3I, Node3D> blockVisuals = new();
	private readonly BoxMesh blockMesh = new() {Size = new Vector3(0.96f, 0.96f, 0.96f)};
	private readonly BoxMesh oreMarkerMesh = new()	{Size = new Vector3(0.45f, 0.06f, 0.45f)};
	private readonly Dictionary<ResourceDefinition, StandardMaterial3D> blockMaterials = new();

	private GameState gameState;
	private bool runFinished;
	private string lastFindName = "None";

	private MeshInstance3D miner;
	private float minerAnimationTime;
	private int nextBlockIndex;
	private int targetX;
	private int targetZ;
	private bool hasTarget;
	private bool isMining;
	private float miningProgress;

	private bool runWasManual;

	private float CurrentMinerHeight => MinerHeight - currentMiningLayer;


	private static readonly PackedScene MinerScene = GD.Load<PackedScene>("res://Assets/SceneAssets/shovel.tscn"); //WIP
	private Vector3 minerBaseScale = new Vector3(0.5f, 0.5f, 0.5f); //WIP

	public override void _Ready()
	{
		gameState = GetNode<GameState>("/root/GameState");
		miningRewardCatalog = gameState.SelectedWorld.RewardCatalog;
		inventory = new RunInventory(gameState.MinerStorageCapacity);
		worldChunk = new WorldChunk(gameState.SelectedWorldRemovedLayers,miningRewardCatalog);
		CreateEnvironment();
		CreateLighting();
		CreateCamera();
		CreateChunkPreview();
		CreateMinerPreview();
		CreateHud();
		ChooseNextBlock();

		runWasManual = gameState.NextRunIsManual;
	}

	public override void _Process(double delta)
	{
		
		if (miner == null)
			return;
		
		float timeStep = (float)delta * simulationSpeed;
		
		minerAnimationTime += timeStep; 

		if (!hasTarget)
		{
			AnimateIdle();
			return;
		}
			

		if (isMining)
		{
			AnimateMining();
			miningProgress += timeStep * gameState.SelectedWorldMinerSpeedMultiplier;
			
			if (miningProgress >= MiningDuration)
				MineCurrentBlock();

			return;
		}

		Vector3 targetPosition = new Vector3(
			targetX - WorldChunk.Width / 2,
			CurrentMinerHeight,
			targetZ - WorldChunk.Width / 2
		);

		Vector3 horizontalTarget = new Vector3(
			targetPosition.X,
			miner.Position.Y,
			targetPosition.Z
		);

		miner.Position = miner.Position.MoveToward(
			horizontalTarget,
			MinerWalkSpeed * gameState.SelectedWorldMinerSpeedMultiplier * timeStep
		);

		AnimateWalking();

		Vector2 minerHorizontalPosition = new Vector2(
			miner.Position.X,
			miner.Position.Z
		);

		Vector2 targetHorizontalPosition = new Vector2(
			targetPosition.X,
			targetPosition.Z
		);

		if (minerHorizontalPosition.DistanceTo(targetHorizontalPosition) < 0.02f)
		{
			miner.Position = targetPosition;
			isMining = true;
			miningProgress = 0f;
		}
	}
	private void CreateHud()
	{
		var canvasLayer = new CanvasLayer();
		AddChild(canvasLayer);

		statusLabel = new Label
		{
			Position = new Vector2(24f, 24f)
		};

		statusLabel.AddThemeFontSizeOverride("font_size", 20);
		canvasLayer.AddChild(statusLabel);
		abortButton = new Button
		{
			Text = "Abort Run",
			AnchorLeft = 1f,
			AnchorRight = 1f,
			OffsetLeft = -160f,
			OffsetRight = -24f,
			OffsetTop = 24f,
			OffsetBottom = 64f
		};

		abortButton.Pressed += AbortRun;
		canvasLayer.AddChild(abortButton);
		UpdateHud();
	}

	private async void AbortRun()
	{
		if (runFinished)
			return;

		runFinished = true;
		hasTarget = false;
		isMining = false;

		AnimateIdle();

		abortButton.Disabled = true;

		statusLabel.Text +=
			"\n\nRun aborted." +
			"\nTemporary cargo and run value were lost.";

		await ToSignal(
			GetTree().CreateTimer(2f),
			SceneTreeTimer.SignalName.Timeout
		);

		GetTree().ChangeSceneToFile("res://Scenes/Menu/MainMenu.tscn");
	}
	
	private void UpdateHud()
	{
		string cargoText = "Cargo:";

		foreach (KeyValuePair<string, int> resource in inventory.GetResources())
		{
			cargoText += $"\n- {resource.Key}: {resource.Value}";
		}

		statusLabel.Text =
			$"World depth: {worldChunk.StartingDepth + currentMiningLayer + 1} / {WorldChunk.TotalLayers}\n" +
			$"Storage: {inventory.UsedCapacity} / {inventory.Capacity}\n" +
			$"Temporary run value: ${inventory.TemporaryValue}\n\n" + //extra string space
			// $"Last find: {lastFindName}\n\n" +
			cargoText;
	}
	private void ChooseNextBlock()
	{
		if (nextBlockIndex >= WorldChunk.Width * WorldChunk.Width)
		{
			currentMiningLayer++;
			nextBlockIndex = 0;

			if (currentMiningLayer >= worldChunk.RemainingLayers)
			{
				CompleteRun("Entire world chunk completely mined");
				return;
			}

			int nextVisibleLayer =
				currentMiningLayer + RenderedLayerCount - 1;

			if (nextVisibleLayer < worldChunk.RemainingLayers)
			{
				RenderLayer(nextVisibleLayer);
			}

			UpdateCameraForCurrentLayer();
		}

		int row = nextBlockIndex / WorldChunk.Width;
		int positionInRow = nextBlockIndex % WorldChunk.Width;

		targetZ = row;
		targetX = row % 2 == 0
			? positionInRow
			: WorldChunk.Width - 1 - positionInRow;

		hasTarget = true;
		isMining = false;
	}

	private void MineCurrentBlock()
	{
		int topLayer = currentMiningLayer;

		BlockKind minedBlock = worldChunk.GetBlock(targetX, targetZ, topLayer);

		worldChunk.MineBlock(targetX, targetZ, topLayer);

		var blockKey = new Vector3I(targetX, topLayer, targetZ);

		if (blockVisuals.TryGetValue(blockKey, out Node3D blockVisual))
		{
			blockVisual.QueueFree();
			blockVisuals.Remove(blockKey);
		}

		int absoluteDepth = worldChunk.StartingDepth + topLayer + 1;

		MiningReward reward = new MiningReward(worldChunk.GetResource(targetX, targetZ, topLayer));

		inventory.TryAdd(reward.Name, reward.Value);
		lastFindName = $"{reward.Name} (+${reward.Value})";
		UpdateHud();

		if (inventory.IsFull)
		{
			CompleteRun("Storage full");
			return;
		}
		
		nextBlockIndex++;
		ChooseNextBlock();
	}

	private void CreateEnvironment()
	{
		var world = gameState.SelectedWorld;

		var environment = new Environment
		{
			BackgroundMode = Environment.BGMode.Color,
			BackgroundColor = world.BackgroundColor,
			AmbientLightSource = Environment.AmbientSource.Color,
			AmbientLightColor = world.AmbientLightColor,
			AmbientLightEnergy = world.AmbientLightEnergy
		};
				
		AddChild(new WorldEnvironment { Environment = environment });
	}

	private void CreateLighting()
	{
		var sunlight = new DirectionalLight3D
		{
			LightEnergy = 1.2f,
			ShadowEnabled = true,
			RotationDegrees = new Vector3(-55f, -35f, 0f),
			LightColor = gameState.SelectedWorld.SunlightColor,
		};

		AddChild(sunlight);
	}

	private void CreateCamera()
	{
		camera = new Camera3D
		{
			Projection = Camera3D.ProjectionType.Orthogonal,
			Size = 21f,
			Position = new Vector3(16f, 16f, 16f) 
		};

		AddChild(camera);
		UpdateCameraForCurrentLayer();
	}
	private void UpdateCameraForCurrentLayer()
	{
		Vector3 target = new Vector3(0f, -currentMiningLayer, 0f);
		Vector3 baseOffset = new Vector3(14f, 8.5f, 14f); // your current offset: (16, 11-3, 16)

		camera.Position = target + baseOffset * cameraDistanceScale;
		camera.LookAt(target);
		
		
	}
	private void CreateChunkPreview()
	{
		for (int layer = 0; layer < RenderedLayerCount; layer++)
		{
			if (layer >= worldChunk.RemainingLayers)
				break;

			RenderLayer(layer);
		}
	}
	private void RenderLayer(int layer)
	{
		for (int x = 0; x < WorldChunk.Width; x++)
		{
			for (int z = 0; z < WorldChunk.Width; z++)
			{
				BlockKind baseBlock = worldChunk.GetBlock(x, z, layer);
				
				ResourceDefinition foundResource =
					worldChunk.GetResource(x, z, layer);

				ResourceDefinition baseResource =
					miningRewardCatalog.GetBaseResource(baseBlock);

				AddBlock(
					x,
					z,
					layer,
					baseResource,
					foundResource
				);
			}
		}
	}
	private async void CompleteRun(string reason)
	{
		
		if (runFinished)
			return;

		runFinished = true;
		abortButton.Disabled = true;
		hasTarget = false;
		AnimateIdle();

		//Play Sound WIP
		PlayRunCompleteSound();

		gameState.BankRun(inventory.TemporaryValue);
		if (runWasManual)
		gameState.OnMissionCompletedManually();

		statusLabel.Text +=
			$"\n\n{reason}." +
			$"\n${inventory.TemporaryValue} transferred to stash.";

		await ToSignal(
			GetTree().CreateTimer(3.5f),
			SceneTreeTimer.SignalName.Timeout
		);

		GetTree().ChangeSceneToFile("res://Scenes/Menu/MainMenu.tscn");
	}

	private void PlayRunCompleteSound()
	{
		var Aplayer = new AudioStreamPlayer();
		AddChild(Aplayer);
		Aplayer.Stream = GD.Load<AudioStream>("res://Assets/Sounds/DigComplete/Cash Register.wav");
		Aplayer.VolumeDb = -25f;
		Aplayer.Play();

		// Clean up once the sound finishes playing
		Aplayer.Finished += () => Aplayer.QueueFree();
	}
	private void CreateMinerPreview()
	{
		miner = MinerScene.Instantiate<MeshInstance3D>();
		miner.Name = "Miner";
		
		AddChild(miner);
		miner.Position = new Vector3(0f, CurrentMinerHeight - 2f, 0f);
		miner.Scale = minerBaseScale;
		
	}

	private void AddBlock(
	int x,
	int z,
	int layer,
	ResourceDefinition baseResource,
	ResourceDefinition foundResource)
	{
		var blockRoot = new Node3D
		{
			Position = new Vector3(
				x - WorldChunk.Width / 2,
				-layer,
				z - WorldChunk.Width / 2
			)
		};

		var baseBlock = new MeshInstance3D
		{
			Mesh = blockMesh,
			MaterialOverride = GetMaterialFor(baseResource)
		};

		blockRoot.AddChild(baseBlock);

		if (foundResource.Tier > 1)
		{
			var oreMarker = new MeshInstance3D
			{
				Mesh = oreMarkerMesh,
				MaterialOverride = GetMaterialFor(foundResource),
				Position = new Vector3(0f, 0.51f, 0f)
			};

			blockRoot.AddChild(oreMarker);
		}

		blockVisuals.Add(new Vector3I(x, layer, z), blockRoot);
		AddChild(blockRoot);
	}

	private StandardMaterial3D GetMaterialFor(ResourceDefinition resource)
	{
		if (blockMaterials.TryGetValue(
			resource,
			out StandardMaterial3D material))
		{
			return material;
		}

		material = CreateMaterial(resource.DisplayColor);
		blockMaterials.Add(resource, material);

		return material;
	}

	private StandardMaterial3D CreateMaterial(Color color)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = color,
			Roughness = 0.9f
		};
	}
	private void AnimateWalking()
	{
		float hop = Mathf.Abs(Mathf.Sin(minerAnimationTime * 10f)) * 0.10f;
		float squash = Mathf.Abs(Mathf.Sin(minerAnimationTime * 10f)) * 0.08f;

		miner.Position = new Vector3(
			miner.Position.X,
			CurrentMinerHeight + hop,
			miner.Position.Z
		);

		miner.Scale = new Vector3(
			minerBaseScale.X * (1f + squash),
			minerBaseScale.Y * (1f - squash),
			minerBaseScale.Z * (1f + squash)
		);
		
	}

	private void AnimateMining()
	{
		float impact = Mathf.Abs(Mathf.Sin(minerAnimationTime * 14f));

		miner.Position = new Vector3(
			miner.Position.X,
			CurrentMinerHeight - impact * 0.12f,
			miner.Position.Z
		);

		miner.Scale = new Vector3(
			minerBaseScale.X * (1f + impact * 0.12f),
			minerBaseScale.Y * (1f - impact * 0.18f),
			minerBaseScale.Z * (1f + impact * 0.12f)
		);
	}

	private void AnimateIdle()
	{
		miner.Position = new Vector3(
			miner.Position.X,
			CurrentMinerHeight,
			miner.Position.Z
		);

		miner.Scale = minerBaseScale;
	}
}
