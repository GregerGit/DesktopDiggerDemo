public sealed class WorldProgress
{
	public int RemovedLayers { get; private set; }
	public int SpeedUpgradeLevel { get; private set; }

	public WorldProgress(
		int removedLayers = 0,
		int speedUpgradeLevel = 0)
	{
		RemovedLayers = removedLayers;
		SpeedUpgradeLevel = speedUpgradeLevel;
	}

	public void RemoveLayer()
	{
		RemovedLayers++;
	}

	public void UpgradeSpeed()
	{
		SpeedUpgradeLevel++;
	}
}
