using Godot;

public readonly struct MiningReward
{
	public ResourceDefinition Resource { get; }

	public string Name => Resource.DisplayName;
	public int Value => Resource.SellValue;

	public MiningReward(ResourceDefinition resource)
	{
		Resource = resource;
	}
}
public static class MiningRewardRoller
{
	public static MiningReward Roll(
		BlockKind baseBlock,
		int depth,
		MiningRewardCatalog catalog)
	{
		foreach (RewardTierDefinition tier in
				 catalog.GetRewardTiersHighestFirst())
		{
			if (!tier.CanSpawnAtDepth(depth))
				continue;

			if (GD.Randf() < tier.GetSpawnChance(depth))
			{
				return new MiningReward(
					tier.GetRandomResource()
				);
			}
		}

		return new MiningReward(
			catalog.GetBaseResource(baseBlock)
		);
	}
}
