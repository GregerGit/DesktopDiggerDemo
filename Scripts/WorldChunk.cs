public enum BlockKind
{
	Dirt,
	Stone,
	DeepStone,
	Bedrock
}

public sealed class WorldChunk
{
	public const int Width = 15;
	public const int TotalLayers = 64;

	private readonly BlockKind[,,] blocks = new BlockKind[Width, Width, TotalLayers];
	private readonly bool[,,] minedBlocks = new bool[Width, Width, TotalLayers];

	private readonly ResourceDefinition[,,] resources = new ResourceDefinition[Width, Width, TotalLayers];

	public int StartingDepth { get; }
	public int RemainingLayers => TotalLayers - StartingDepth;

	public WorldChunk(int startingDepth, MiningRewardCatalog miningRewardCatalog)
	{
		StartingDepth = startingDepth < 0
			? 0
			: startingDepth >= TotalLayers
				? TotalLayers - 1
				: startingDepth;

		for (int x = 0; x < Width; x++)
		{
			for (int z = 0; z < Width; z++)
			{
				for (int layer = 0; layer < TotalLayers; layer++)
				{
					int absoluteDepth = layer + StartingDepth + 1;

					BlockKind blockKind = GetBlockForLayer(layer + StartingDepth);

					blocks[x, z, layer] = blockKind;

					resources[x, z, layer] = MiningRewardRoller.Roll(
						blockKind,
						absoluteDepth,
						miningRewardCatalog
					).Resource;
				}
			}
		}
	}
	public ResourceDefinition GetResource(int x, int z, int layer)
	{
		return resources[x, z, layer];
	}
	public BlockKind GetBlock(int x, int z, int layer)
	{
		return blocks[x, z, layer];
	}
	public void MineBlock(int x, int z, int layer)
	{
		minedBlocks[x, z, layer] = true;
	}

	public bool IsMined(int x, int z, int layer)
	{
		return minedBlocks[x, z, layer];
	}
	private BlockKind GetBlockForLayer(int layer)
	{
		if (layer < 16)
			return BlockKind.Dirt;

		if (layer < 36)
			return BlockKind.Stone;

		if (layer < 56)
			return BlockKind.DeepStone;

		return BlockKind.Bedrock;
	}
}
