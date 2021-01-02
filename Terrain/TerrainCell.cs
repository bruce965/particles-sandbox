using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

using static ParticlesSandbox.Util;

namespace ParticlesSandbox
{
    [StructLayout(LayoutKind.Explicit)]
    public struct TerrainCell
    {
        // Color    = rrrrrrrr gggggggg bbbbbbbb aaaaaaaa
        // Material = mmmmmmmm ____mmmm ________ ________ (litte endian)
        // Coords   = ________ ________ __xxxxxx __yyyyyy
        // Extra    = ________ zzzz____ zz______ zz______
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

        public TerrainCell(TerrainMaterial material, byte originalCoordX, byte originalCoordY, byte extra) : this()
        {
            Assert((int)material == ((int)material & 0b00001111_11111111), $"Invalid {nameof(TerrainCell)}.{nameof(Material)}: out of range");
            Assert(originalCoordX == (originalCoordX & 0b00111111), $"Invalid {nameof(TerrainCell)}.{nameof(OriginalCoordX)}: out of range");
            Assert(originalCoordY == (originalCoordY & 0b00111111), $"Invalid {nameof(TerrainCell)}.{nameof(OriginalCoordY)}: out of range");

            _material = (ushort)material;

            if (!BitConverter.IsLittleEndian)
            {
                var buff = _g;
                _g = _r;
                _r = buff;
            }

            _g |= (byte)(extra & 0b11110000);
            _b = (byte)(originalCoordX | ((extra & 0b00001100) << 4));
            _a = (byte)(originalCoordY | ( extra               << 6));
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

                return (TerrainMaterial)(material & 0b00001111_11111111);
            }
            set
            {
                Assert((int)value == ((int)value & 0b00001111_11111111), $"Invalid {nameof(TerrainCell)}.{nameof(Material)}: out of range");

                _material = (ushort)((_material & 0b11110000_00000000) | (int)value);

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
            get => (byte)(_b & 0b00111111);
            set
            {
                Assert(value == (value & 0b00111111), $"Invalid {nameof(TerrainCell)}.{nameof(OriginalCoordX)}: out of range");
                _b = (byte)((_b & 0b11000000) | value);
            }
        }

        /// <summary>
        /// Y coordinate on spawn of the particle occupying this cell, before moving.
        /// </summary>
        /// <value>Y coordinate.</value>
        public byte OriginalCoordY
        {
            get => (byte)(_a & 0b00111111);
            set
            {
                Assert(value == (value & 0b00111111), $"Invalid {nameof(TerrainCell)}.{nameof(OriginalCoordY)}: out of range");
                _a = (byte)((_a & 0b11000000) | value);
            }
        }

        /// <summary>
        /// Extra data associated to the particle occupying this cell.
        /// </summary>
        /// <value></value>
        public byte Extra
        {
            get => (byte)((_g & 0b11110000) | ((_b >> 4) & 0b00001100) | (_a >> 6));
            set
            {
                _g = (byte)((_g & 0x00001111) |  (value & 0b11110000)      );
                _b = (byte)((_b & 0x00111111) | ((value & 0b00001100) << 4));
                _a = (byte)((_a & 0x00111111) | ((value & 0b00000011) << 6));
            }
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
