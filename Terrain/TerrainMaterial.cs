using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ParticlesSandbox
{
    public enum TerrainMaterial
    {
        /// <summary>Unloaded tiles or invalid/corrupted data.</summary>
        Invalid = 0,
        /// <summary>Nothing, vacant space, air.</summary>
        Air,
        Dirt,
        Stone,
        Grass,
        Wood,
        Sand,
        Water,
    }

    public static class TerrainMaterialExtensions
    {
        static readonly IReadOnlyDictionary<TerrainMaterial, Color> _color = new Dictionary<TerrainMaterial, Color>{
            [TerrainMaterial.Air] = Color.Color8(0, 0, 0, 0),
            [TerrainMaterial.Invalid] = Color.Color8(0, 0, 0),
            [TerrainMaterial.Dirt] = Color.Color8(156, 68, 0),
            [TerrainMaterial.Stone] = Color.Color8(134, 134, 134),
            [TerrainMaterial.Grass] = Color.Color8(0, 95, 0),
            [TerrainMaterial.Wood] = Color.Color8(95, 20, 0),
            [TerrainMaterial.Sand] = Color.Color8(207, 156, 110),
            [TerrainMaterial.Water] = Color.Color8(32, 125, 253),
        };

        static readonly IReadOnlyDictionary<Color, TerrainMaterial> _material
            = _color.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        public static TerrainMaterial AsTerrainMaterial(this Color color)
        {
            if (color.a8 == 254)
            {
                // unknown material
                return (TerrainMaterial)(color.r8 | color.g8 << 8 | color.b8 << 16);
            }

            if (!_material.TryGetValue(color, out var material))
            {
                // invalid material
                material = TerrainMaterial.Invalid;
            }

            return material;
        }

        public static Color GetColor(this TerrainMaterial material)
        {
            if (!_color.TryGetValue(material, out var color))
            {
                // unknown material
                color = unchecked (Color.Color8((byte)material, (byte)((int)material >> 8), (byte)((int)material >> 16), 254));
            }

            return color;
        }
    }
}
