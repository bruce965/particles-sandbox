using System;
using Godot;

namespace ParticlesSandbox
{
    public class TerrainTile : Node2D
    {
        readonly Lazy<Sprite> _renderSprite;
        internal Sprite RenderSprite => _renderSprite.Value;

        readonly Lazy<Viewport> _simulationViewport;
        internal Viewport SimulationViewport => _simulationViewport.Value;

        readonly Lazy<Viewport> _simulationViewport2;
        internal Viewport SimulationViewport2 => _simulationViewport2.Value;

        readonly Lazy<Sprite> _simulationSprite;
        internal Sprite SimulationSprite => _simulationSprite.Value;

        [Export]
        public int TileX
        {
            get => (int)(Position.x / TerrainTileData.Width);
            set => Position = new Vector2(value * TerrainTileData.Width, Position.y);
        }

        [Export]
        public int TileY
        {
            get => (int)(Position.y / TerrainTileData.Height);
            set => Position = new Vector2(Position.x, value * TerrainTileData.Height);
        }

        [Export]
        public Texture? DataTexture
        {
            get => RenderSprite.Texture;
            set
            {
                RenderSprite.Texture = value;
                SimulationSprite.Texture = value;
            }
        }

        [Export]
        public bool DoubleBuffering { get; set; }

        public TerrainTile()
        {
            _renderSprite = new Lazy<Sprite>(() => GetNode<Sprite>("RenderSprite"));
            _simulationViewport = new Lazy<Viewport>(() => GetNode<Viewport>("SimulationViewport"));
            _simulationSprite = new Lazy<Sprite>(() => SimulationViewport.GetNode<Sprite>("SimulationSprite"));

            _simulationViewport2 = new Lazy<Viewport>(() => {
                SimulationViewport.RemoveChild(SimulationSprite);

                var viewportCopy = (Viewport)SimulationViewport.Duplicate();
                viewportCopy.Name += "2";
                AddChild(viewportCopy);

                SimulationViewport.AddChild(SimulationSprite);

                return viewportCopy;
            });
        }

        public override void _Ready()
        {
            SimulationViewport.Size = new Vector2(TerrainTileData.Width, TerrainTileData.Height);

            if (DoubleBuffering)
            {
                SimulationViewport2.Size = new Vector2(TerrainTileData.Width, TerrainTileData.Height);
            }
        }

        public void SetDataTexture(int tileX, int tileY, Texture? dataTexture)
        {
            var offsetX = tileX - TileX;
            var offsetY = tileY - TileY;

            if (offsetX == 0 && offsetY == 0)
            {
                DataTexture = dataTexture;
            }
            else
            {
                var param = $"Terrain{(offsetY == -1 ? "Top" : offsetY == +1 ? "Bottom" : "")}{(offsetX == -1 ? "Left" : offsetX == +1 ? "Right" : "")}";
                ((ShaderMaterial)RenderSprite.Material).SetShaderParam(param, dataTexture);
                ((ShaderMaterial)SimulationSprite.Material).SetShaderParam(param, dataTexture);
            }
        }

        public void RunSimulationOnNextDraw(int randomSeed)
        {
            ((ShaderMaterial)SimulationSprite.Material).SetShaderParam("RandomSeed", randomSeed);

            Viewport nextDrawViewport;
            var lastDrawViewport = (Viewport)SimulationSprite.GetParent();

            if (DoubleBuffering)
            {
                nextDrawViewport = lastDrawViewport == SimulationViewport ? SimulationViewport2 : SimulationViewport;

                lastDrawViewport.RemoveChild(SimulationSprite);
                nextDrawViewport.AddChild(SimulationSprite);
            }
            else
            {
                nextDrawViewport = lastDrawViewport;
            }

            nextDrawViewport.RenderTargetUpdateMode = Viewport.UpdateMode.Once;
        }

        public bool SwapBuffer()
        {
            var lastDrawViewport = (Viewport)SimulationSprite.GetParent();
            var texture = lastDrawViewport.GetTexture();

            if (DataTexture != texture)
            {
                DataTexture = texture;
                return true;
            }

            return false;
        }
    }
}
