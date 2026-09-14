using System.Collections.Generic;

public sealed class RunInventory
{
	private readonly Dictionary<string, int> resources = new();

	public int Capacity { get; }
	public int UsedCapacity { get; private set; }
	public int TemporaryValue { get; private set; }
	public bool IsFull => UsedCapacity >= Capacity;

	public IEnumerable<KeyValuePair<string, int>> GetResources()
	{
		return resources;
	}

	public RunInventory(int capacity)
	{
		Capacity = capacity;
	}

	public bool TryAdd(string resourceName, int value)
	{
		if (IsFull)
			return false;

		resources.TryAdd(resourceName, 0);
		resources[resourceName]++;

		UsedCapacity++;
		TemporaryValue += value;

		return true;
	}

	public int GetAmount(string resourceName)
	{
		return resources.TryGetValue(resourceName, out int amount)
			? amount
			: 0;
	}
}
