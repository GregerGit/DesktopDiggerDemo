using Godot;

[GlobalClass]
public partial class BlockResourceDefinition : Resource
{
	[Export] public BlockKind BlockKind { get; set; }
	[Export] public ResourceDefinition Resource { get; set; }
}
