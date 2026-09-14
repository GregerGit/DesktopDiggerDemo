using Godot;

[GlobalClass]
public partial class RewardTierDefinition : Resource
{
	[Export] public int Tier = 2;

	[Export(PropertyHint.Range, "0, 1, 0.0001")]
	public float ShallowSpawnChance = 0.02f;

	[Export(PropertyHint.Range, "0, 1, 0.0001")]
	public float DeepSpawnChance = 0.05f;

	private const int DeepDepthStartsAt = 32;

	[Export]
	public Godot.Collections.Array<ResourceDefinition> Resources = new();

	public bool CanSpawnAtDepth(int depth)
	{
		return Resources.Count > 0 && GetSpawnChance(depth) > 0f;
	}

	public float GetSpawnChance(int depth)
	{
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
