using Godot;

[GlobalClass]
public partial class RewardTierDefinition : Resource
{
	[Export] public int Tier = 2;

	[Export(PropertyHint.Range, "0, 1, 0.00001")]
	public float ShallowSpawnChance = 0.02f;

	[Export(PropertyHint.Range, "0, 1, 0.00001")]
	public float DeepSpawnChance = 0.05f;

	// New: gates this tier entirely below a certain depth.
	// Set to 0 for tiers that can always spawn (e.g. Tier 1-3),
	// and to something like 64/96 for Tier 4/5 in the inspector.
	[Export(PropertyHint.Range, "0, 500, 1")]
	public int MinimumDepth = 0;

	private const int DeepDepthStartsAt = 32;

	[Export]
	public Godot.Collections.Array<ResourceDefinition> Resources = new();

	public bool CanSpawnAtDepth(int depth)
	{
		return Resources.Count > 0 && GetSpawnChance(depth) > 0f;
	}

	public float GetSpawnChance(int depth)
	{
		if (depth < MinimumDepth)
			return 0f;

		return depth < DeepDepthStartsAt
			? ShallowSpawnChance
			: DeepSpawnChance;
	}

	public ResourceDefinition GetRandomResource()
	{
		int index = (int)(GD.Randi() % (uint)Resources.Count);
		return Resources[index];
	}
}
