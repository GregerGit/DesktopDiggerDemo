using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class MiningRewardCatalog : Resource
{
	[Export]
	public Godot.Collections.Array<BlockResourceDefinition> BaseResources = new();

	[Export]
	public Godot.Collections.Array<RewardTierDefinition> RewardTiers = new();

	public ResourceDefinition GetBaseResource(BlockKind blockKind)
	{
		foreach (BlockResourceDefinition entry in BaseResources)
		{
			if (entry.BlockKind == blockKind)
				return entry.Resource;
		}

		throw new InvalidOperationException(
			$"No base resource is configured for {blockKind}."
		);
	}

	public List<RewardTierDefinition> GetRewardTiersHighestFirst()
	{
		var sortedTiers = new List<RewardTierDefinition>(RewardTiers);

		sortedTiers.Sort((first, second) =>
			second.Tier.CompareTo(first.Tier)
		);

		return sortedTiers;
	}
}
