using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

using static Godot.GD;
using static ParticlesSandbox.Util;

namespace ParticlesSandbox
{
    public class Terrain : Node
    {
        [Export]
        public string SavePath { get; set; } = "user://terrain/";

        [Export]
        public string WorldName { get; set; } = "world";

        [Export]
        public bool SaveChanges { get; set; }

        [Export]
        public Texture? Materials { get; set; }

        readonly WeakLazy<byte[]> buffer = new WeakLazy<byte[]>(
            () => new byte[TerrainTile.Width * TerrainTile.Height * 4]
        );

        readonly Lazy<TerrainConfig> _config;
        internal TerrainConfig Config => _config.Value;

        public Terrain()
        {
            _config = new Lazy<TerrainConfig>(() => GetNode<TerrainConfig>("Config"));
        }

        public override void _Ready()
        {
            // TEST
            GetTilesRange(-1, +1, -1, +1, forceLoad: true);

            //foreach (var tile in LoadedTiles)
            //{
            //    TileSave(tile);
            //}
        }

        public override void _Process(float delta)
        {
            StepSimulation();
        }

        #region Tile management

        IEnumerable<TerrainTile> LoadedTiles
            => GetChildren().OfType<TerrainTile>().Where(t => !t.IsQueuedForDeletion());

        TerrainTile TileLoad(int tileX, int tileY)
        {
            Assert(!HasNode(TileNodeName(tileX, tileY)), $"Tile [{tileX}, {tileY}] already loaded");

            var texture = TileFileLoad(tileX, tileY);
            if (texture == null)
                texture = TileGenerate(tileX, tileY);

            var tile = (TerrainTile)Config.TerrainTile!.Instance();
            tile.Name = TileNodeName(tileX, tileY);
            tile.TileX = tileX;
            tile.TileY = tileY;
            tile.DataTexture = texture;
            ((ShaderMaterial)tile.RenderSprite.Material).SetShaderParam("Materials", Materials);

            var neighbors = GetTilesRange(tileX - 1, tileX + 1, tileY - 1, tileY + 1);
            foreach (var neighbor in neighbors)
            {
                neighbor.SetDataTexture(tileX, tileY, texture);
                tile.SetDataTexture(neighbor.TileX, neighbor.TileY, neighbor.DataTexture);
            }

            AddChild(tile);

            return tile;
        }

        void TileUnload(TerrainTile tile)
        {
            if (SaveChanges)
                TileSave(tile);

            tile.Name += " (unloading)";
            tile.QueueFree();
        }

        void TileSave(TerrainTile tile)
        {
            TileFileSave(tile.TileX, tile.TileY, tile!.DataTexture!);
        }

        Texture TileGenerate(int tileX, int tileY)
        {
            var data = buffer.Value;

            for (var y = 0; y < TerrainTile.Height; y++)
            {
                for (var x = 0; x < TerrainTile.Height; x++)
                {
                    // TODO
                    var i = (y * TerrainTile.Width + x) * 4;
                    data[i] = 0;
                    data[i + 1] = 0;
                    data[i + 2] = 0;
                    data[i + 3] = 0;
                }
            }

            var dataImage = new Image();
            dataImage.CreateFromData(TerrainTile.Width, TerrainTile.Height, false, Image.Format.Rgba8, data);

            var dataTexture = new ImageTexture();
            dataTexture.CreateFromImage(dataImage, 0);

            return dataTexture;
        }

        IEnumerable<TerrainTile> GetTilesRange(int minX, int maxX, int minY, int maxY, bool forceLoad = false)
        {
            var list = new List<TerrainTile>((maxX - minX) * (maxY - minY));

            for (var tileY = minY; tileY <= maxY; tileY++)
            {
                for (var tileX = minX; tileX <= maxX; tileX++)
                {
                    var tile = GetNodeOrNull<TerrainTile>(TileNodeName(tileX, tileY));

                    if (forceLoad && tile == null)
                        tile = TileLoad(tileX, tileY);

                    if (tile != null)
                        list.Add(tile);
                }
            }

            return list;
        }

        static string TileNodeName(int tileX, int tileY)
            => $"Chunk [{tileX}, {tileY}]";

        #endregion

        #region File saving/loading

        Texture? TileFileLoad(int tileX, int tileY)
        {
            var fullPath = TileFilePath(tileX, tileY);
            if (!new File().FileExists(fullPath))
                return null;

            var materialImage = new Image();
            var loadError = materialImage.Load(fullPath);
            if (loadError != Error.Ok)
            {
                PrintErr($"Failed to load terrain tile at \"{fullPath}\": {loadError}");
                return null;
            }

            if (materialImage.GetWidth() != TerrainTile.Width || materialImage.GetHeight() != TerrainTile.Height)
            {
                PrintErr($"Failed to load terrain tile at \"{fullPath}\": image is not {TerrainTile.Width}x{TerrainTile.Height}");
                return null;
            }

            var dataImage = DataImageFromMaterial(materialImage);

            var dataTexture = new ImageTexture();
            dataTexture.CreateFromImage(dataImage, 0);

            return dataTexture;
        }

        void TileFileSave(int tileX, int tileY, Texture texture)
        {
            Assert(!SaveChanges, $"Trying to save tile [{tileX}, {tileY}], but terrain is set as readonly");

            var dataImage = texture.GetData();
            var materialImage = MaterialImageFromData(dataImage);

            var fullPath = TileFilePath(tileX, tileY);
            if (!new File().FileExists($"{fullPath}/.."))
            {
                var mkdirError = new Directory().MakeDirRecursive($"{fullPath}/..");
                if (mkdirError != Error.Ok)
                    PrintErr($"Failed to save terrain tile at \"{fullPath}\": failed to create directory, error {mkdirError}");

                var saveError = materialImage.SavePng(fullPath);
                if (mkdirError != Error.Ok)
                    PrintErr($"Failed to save terrain tile at \"{fullPath}\": {saveError}");
            }
        }

        string TileFilePath(int tileX, int tileY)
            => $"{SavePath}{WorldName}/{tileX}_{tileY}.png";

        #endregion

        #region Materials

        Image MaterialImageFromData(Image image)
        {
            var data = buffer.Value;

            image.Lock();

            for (var y = 0; y < TerrainTile.Height; y++)
            {
                for (var x = 0; x < TerrainTile.Height; x++)
                {
                    var pixel = image.GetPixel(x, y);
                    var material = (TerrainMaterial)(pixel.r8 | pixel.g8 << 8);
                    //var startX = pixel.b8;
                    //var startY = pixel.a8;

                    var i = (y * TerrainTile.Width + x) * 4;
                    var color = material.GetColor();
                    data[i] = unchecked ((byte)color.r8);
                    data[i + 1] = unchecked ((byte)color.g8);
                    data[i + 2] = unchecked ((byte)color.b8);
                    data[i + 3] = unchecked ((byte)color.a8);
                }
            }

            image.Unlock();

            var materialImage = new Image();
            materialImage.CreateFromData(TerrainTile.Width, TerrainTile.Height, false, Image.Format.Rgba8, data);

            return materialImage;
        }

        Image DataImageFromMaterial(Image image)
        {
            var data = buffer.Value;

            image.Lock();

            for (var y = 0; y < TerrainTile.Height; y++)
            {
                for (var x = 0; x < TerrainTile.Height; x++)
                {
                    var pixel = image.GetPixel(x, y);
                    var material = pixel.AsTerrainMaterial();

                    var i = (y * TerrainTile.Width + x) * 4;
                    data[i] = unchecked ((byte)material);
                    data[i + 1] = unchecked ((byte)((int)material >> 8));
                    data[i + 2] = unchecked ((byte)x);
                    data[i + 3] = unchecked ((byte)y);
                }
            }

            image.Unlock();

            var dataImage = new Image();
            dataImage.CreateFromData(TerrainTile.Width, TerrainTile.Height, false, Image.Format.Rgba8, data);

            return dataImage;
        }

        #endregion

        #region Simulation

        void StepSimulation()
        {
            // TODO: find a way to retrieve all active viewports, not just the main one.
            var mainViewport = GetViewport().GetViewportRid();

            // disable main viewport
            VisualServer.ViewportSetActive(mainViewport, false);

            try
            {
                var loadedTilesList = LoadedTiles.ToList();

                foreach (var tile in loadedTilesList)
                {
                    tile.RunSimulationOnNextDraw();
                }

                VisualServer.ForceDraw(false);

                foreach (var tile in loadedTilesList)
                {
                    if (!tile.SwapBuffer())
                        continue;

                    var neighbors = GetTilesRange(tile.TileX - 1, tile.TileX + 1, tile.TileY - 1, tile.TileY + 1).Where(n => n != tile);
                    foreach (var neighbor in neighbors)
                    {
                        neighbor.SetDataTexture(tile.TileX, tile.TileY, tile.DataTexture);
                    }
                }
            }
            finally
            {
                // restore main viewport
                VisualServer.ViewportSetActive(mainViewport, true);
            }
        }

        #endregion
    }
}
