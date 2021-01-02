using System;
using System.Runtime.InteropServices;
using Godot;

namespace ParticlesSandbox
{
    public class TerrainTileData
    {
        public const int Width = 256;
        public const int Height = 256;
        public const Image.Format Format = Image.Format.Rgba8;
        public const int BytesPerTexel = 4;  // depends on the format

        public const int DataLength = Width * Height * BytesPerTexel;

        /// <summary>
        /// Raw data in this tile.
        /// </summary>
        /// <value>Raw data.</value>
        public byte[] Data { get; }

        public ref TerrainCell this[int x, int y]
            => ref MemoryMarshal.Cast<byte, TerrainCell>(Data.AsSpan())[y * Width + x];

        public TerrainTileData()
            : this(new byte[DataLength]) { }

        public TerrainTileData(byte[] data)
        {
            if (data.Length != DataLength)
                throw new ArgumentException($"Data length is {data.Length}, expecting {DataLength}.", nameof(data));

            Data = data;
        }

        public static TerrainTileData FromTexture(Texture texture)
            => FromImage(texture.GetData());

        public static TerrainTileData FromImage(Image image)
            => new TerrainTileData(image.GetData());

        public void ImportFrom(Image image)
        {
            var data = image.GetData();
            if (data.Length != DataLength)
                throw new ArgumentException($"Data length is {data.Length}, expecting {DataLength}.", nameof(data));

            data.CopyTo(Data, 0);
        }

        public void ImportFrom(Texture texture)
            => ImportFrom(texture.GetData());

        public void ExportTo(Image image)
            => image.CreateFromData(Width, Height, false, Format, Data);

        public void ExportTo(ImageTexture texture)
        {
            var image = texture.GetData() ?? new Image();
            ExportTo(image);

            texture.CreateFromImage(image, 0);
        }
    }
}
