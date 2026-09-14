using Godot;

[GlobalClass]
public partial class WorldDefinition : Resource
{
	[Export] public string WorldId = "first-world";
	[Export] public string DisplayName = "First World";
	[Export] public MiningRewardCatalog RewardCatalog;

	[Export] public Color BackgroundColor = new Color("#8DC9E8");
	[Export] public Color AmbientLightColor = Colors.White;

	[Export(PropertyHint.Range, "0, 2, 0.05")]
	public float AmbientLightEnergy = 0.65f;

	[Export] public Color SunlightColor = Colors.White;

	[Export] public bool UnlockedByDefault = false;
	[Export] public int UnlockCost = 0;
}
