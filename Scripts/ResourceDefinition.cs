using Godot;

[GlobalClass]
public partial class ResourceDefinition : Resource
{
	[Export] public string DisplayName = "Unnamed";

	[Export(PropertyHint.Range, "1, 3, 1")]
	public int Tier = 1;

	[Export] public int SellValue = 1;

	[Export] public Color DisplayColor = Colors.White;
}
