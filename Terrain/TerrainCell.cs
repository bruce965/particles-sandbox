using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace ParticlesSandbox
{
    [StructLayout(LayoutKind.Explicit)]
    public struct TerrainCell
    {
        // Color    = rrrrrrrr gggggggg bbbbbbbb aaaaaaaa
        // Material = mmmmmmmm mmmmmmmm ________ ________ (litte endian)
        // Coords   = ________ ________ xxxxxxxx yyyyyyyy
        [FieldOffset(0)] byte _r;
        [FieldOffset(1)] byte _g;
        [FieldOffset(2)] byte _b;
        [FieldOffset(3)] byte _a;

        [FieldOffset(0)] ushort _material;

        public TerrainCell(byte r, byte g, byte b, byte a) : this()
        {
            _r = r;
            _g = g;
            _b = b;
            _a = a;
        }

        public TerrainCell(TerrainMaterial material, byte originalCoordX, byte originalCoordY) : this()
        {
            _material = (ushort)material;

            if (!BitConverter.IsLittleEndian)
            {
                var buff = _g;
                _g = _r;
                _r = buff;
            }

            _b = originalCoordX;
            _a = originalCoordY;
        }

        /// <summary>
        /// Material in this cell.
        /// </summary>
        /// <value>Material.</value>
        public TerrainMaterial Material
        {
            get
            {
                var material = _material;

                if (!BitConverter.IsLittleEndian)
                {
                    material = BinaryPrimitives.ReverseEndianness(material);
                }

                return (TerrainMaterial)material;
            }
            set
            {
                _material = (ushort)value;

                if (!BitConverter.IsLittleEndian)
                {
                    var buff = _g;
                    _g = _r;
                    _r = buff;
                }
            }
        }

        /// <summary>
        /// X coordinate on spawn of the particle occupying this cell, before moving.
        /// </summary>
        /// <value>X coordinate.</value>
        public byte OriginalCoordX
        {
            get => _b;
            set => _b = value;
        }

        /// <summary>
        /// Y coordinate on spawn of the particle occupying this cell, before moving.
        /// </summary>
        /// <value>Y coordinate.</value>
        public byte OriginalCoordY
        {
            get => _a;
            set => _a = value;
        }

        public byte R
        {
            get => _r;
            set => _r = value;
        }

        public byte G
        {
            get => _g;
            set => _g = value;
        }

        public byte B
        {
            get => _b;
            set => _b = value;
        }

        public byte A
        {
            get => _a;
            set => _a = value;
        }
    }
}
