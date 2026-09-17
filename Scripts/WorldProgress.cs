public sealed class WorldProgress
{
	public int RemovedLayers { get; private set; }
	public int SpeedUpgradeLevel { get; private set; }
	public int AutoRunCharges { get; private set; }
	public bool AutoRunActive { get; private set; }

	public WorldProgress(
		int removedLayers = 0,
		int speedUpgradeLevel = 0,
		int autoRunCharges = 0,
		bool autoRunActive = false)
	{
		RemovedLayers = removedLayers;
		SpeedUpgradeLevel = speedUpgradeLevel;
		AutoRunCharges = autoRunCharges;
		AutoRunActive = autoRunActive;
	}

	public void RemoveLayer()
	{
		RemovedLayers++;
	}

	public void UpgradeSpeed()
	{
		SpeedUpgradeLevel++;
	}

	public void AddAutoRunCharge()
	{
		AutoRunCharges++;
	}

	public bool TryConsumeAutoRunCharge()
	{
		if (AutoRunCharges <= 0)
			return false;

		AutoRunCharges--;
		return true;
	}

	public void SetAutoRunActive(bool active)
	{
		AutoRunActive = active;
	}
}
