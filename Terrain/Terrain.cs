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

        readonly WeakLazy<TerrainTileData> _tileBuffer
            = WeakLazy.FromDefaultConstructor<TerrainTileData>();

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

            var tileData = _tileBuffer.Value;
            if (!TryLoadTileFile(tileX, tileY, tileData))
                TileGenerate(tileX, tileY, tileData);

            var texture = new ImageTexture();
            tileData.ExportTo(texture);

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
            var tileData = _tileBuffer.Value;
            tileData.ImportFrom(tile.DataTexture!);

            SaveTileFile(tile.TileX, tile.TileY, tileData);
        }

        void TileGenerate(int tileX, int tileY, TerrainTileData tileData)
        {
            for (var y = 0; y < TerrainTileData.Height; y++)
            {
                for (var x = 0; x < TerrainTileData.Width; x++)
                {
                    // TODO
                    tileData[x, y] = new TerrainCell(TerrainMaterial.Air, (byte)x, (byte)y);
                }
            }
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

        bool TryLoadTileFile(int tileX, int tileY, TerrainTileData tileData)
        {
            var fullPath = TileFilePath(tileX, tileY);
            if (!new File().FileExists(fullPath))
                return false;

            var materialImage = new Image();
            var loadError = materialImage.Load(fullPath);
            if (loadError != Error.Ok)
            {
                PrintErr($"Failed to load terrain tile at \"{fullPath}\": {loadError}");
                return false;
            }

            if (materialImage.GetWidth() != TerrainTileData.Width || materialImage.GetHeight() != TerrainTileData.Height)
            {
                PrintErr($"Failed to load terrain tile at \"{fullPath}\": image is not {TerrainTileData.Width}x{TerrainTileData.Height}");
                return false;
            }

            TileDataFromMaterialImage(materialImage, tileData);
            return true;
        }

        void SaveTileFile(int tileX, int tileY, TerrainTileData tileData)
        {
            Assert(!SaveChanges, $"Trying to save tile [{tileX}, {tileY}], but terrain is set as readonly");

            var materialImage = MaterialImageFromTileData(tileData);

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

        Image MaterialImageFromTileData(TerrainTileData tileData)
        {
            var data = new byte[TerrainTileData.Width * TerrainTileData.Height * 4];

            for (var y = 0; y < TerrainTileData.Height; y++)
            {
                for (var x = 0; x < TerrainTileData.Width; x++)
                {
                    var cell = tileData[x, y];

                    var i = (y * TerrainTileData.Width + x) * 4;
                    var color = cell.Material.GetColor();
                    data[i] = unchecked ((byte)color.r8);
                    data[i + 1] = unchecked ((byte)color.g8);
                    data[i + 2] = unchecked ((byte)color.b8);
                    data[i + 3] = unchecked ((byte)color.a8);
                }
            }

            var materialImage = new Image();
            materialImage.CreateFromData(TerrainTileData.Width, TerrainTileData.Height, false, Image.Format.Rgba8, data);

            return materialImage;
        }

        void TileDataFromMaterialImage(Image materialImage, TerrainTileData tileData)
        {
            materialImage.Lock();

            for (var y = 0; y < TerrainTileData.Height; y++)
            {
                for (var x = 0; x < TerrainTileData.Width; x++)
                {
                    var pixel = materialImage.GetPixel(x, y);
                    var material = pixel.AsTerrainMaterial();

                    tileData[x, y] = new TerrainCell(material, (byte)x, (byte)y);
                }
            }

            materialImage.Unlock();
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
