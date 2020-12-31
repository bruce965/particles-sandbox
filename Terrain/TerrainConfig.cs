using Godot;

namespace ParticlesSandbox
{
    public class TerrainConfig : Node
    {
        [Export]
        public PackedScene? TerrainTile { get; set; }
    }
}
