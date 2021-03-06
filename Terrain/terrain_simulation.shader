shader_type canvas_item;
render_mode blend_disabled, unshaded;

uniform sampler2D TerrainTopLeft: hint_black;
uniform sampler2D TerrainTop: hint_black;
uniform sampler2D TerrainTopRight: hint_black;
uniform sampler2D TerrainLeft: hint_black;
//uniform sampler2D Terrain: hint_black;  // TEXTURE
uniform sampler2D TerrainRight: hint_black;
uniform sampler2D TerrainBottomLeft: hint_black;
uniform sampler2D TerrainBottom: hint_black;
uniform sampler2D TerrainBottomRight: hint_black;

uniform int RandomSeed;

const ivec2 TILE_SIZE = ivec2(256, 256);

void _getPixelData(sampler2D terrain, vec2 coordsUv, out int material, out ivec2 originalCoords, out int extraData) {
	ivec4 data = ivec4(texture(terrain, coordsUv) * 255.);
	material = data.x + ((data.y & 0x0F) << 8);  // little endian
	originalCoords = ivec2(data.z & 0x3F, data.a & 0x3F);
	extraData = (data.y & 0x0F) | ((data.z & 0xC0) >> 4) | ((data.a & 0xC0) >> 6);
}

void getPixelData(sampler2D terrain, ivec2 coords, out int material, out ivec2 originalCoords, out int extraData) {
	vec2 fTile;
	vec2 coordsUv = modf((vec2(coords) / vec2(TILE_SIZE)) + vec2(1), fTile);
	ivec2 tile = ivec2(fTile) - ivec2(1);

	if (tile.y == -1) {
		if (tile.x == -1) {
			_getPixelData(TerrainTopLeft, coordsUv, material, originalCoords, extraData);
		}
		else if (tile.x == +1) {
			_getPixelData(TerrainTopRight, coordsUv, material, originalCoords, extraData);
		}
		else {
			_getPixelData(TerrainTop, coordsUv, material, originalCoords, extraData);
		}
	}
	else if (tile.y == +1) {
		if (tile.x == -1) {
			_getPixelData(TerrainBottomLeft, coordsUv, material, originalCoords, extraData);
		}
		else if (tile.x == +1) {
			_getPixelData(TerrainBottomRight, coordsUv, material, originalCoords, extraData);
		}
		else {
			_getPixelData(TerrainBottom, coordsUv, material, originalCoords, extraData);
		}
	}
	else {
		if (tile.x == -1) {
			_getPixelData(TerrainLeft, coordsUv, material, originalCoords, extraData);
		}
		else if (tile.x == +1) {
			_getPixelData(TerrainRight, coordsUv, material, originalCoords, extraData);
		}
		else {
			_getPixelData(terrain, coordsUv, material, originalCoords, extraData);
		}
	}
}

vec4 makePixelData(int material, ivec2 originalCoords) {
	ivec4 data = ivec4(material & 0xFF, (material >> 8) & 0xFF, originalCoords);
	return vec4(data) / 255.;
}

int materialAt(sampler2D terrain, ivec2 coords) {
	int material;
	ivec2 originalCoords;
	int extraData;
	getPixelData(terrain, coords, material, originalCoords, extraData);
	return material;
}

bool rand(int seed, ivec2 coords) {
	return fract(sin(dot(vec2(coords), vec2(12.9898,78.233)) + float(seed)) * 43758.5453) > .5;
}

/*
 * Compute the movement performed by the cell at the specified coordinates.
 * Customize this function to alter material physics.
 */
bool isMovingFrom(sampler2D terrain, ivec2 coords, int material, out ivec2 newCoords) {
	if (material == 6) {  // sand
		ivec2 coordsBelow = coords + ivec2(0, +1);
		if (materialAt(terrain, coordsBelow) == 1) {  // air
			newCoords = coordsBelow;
			return true;
		}

		// move randomly either left or right
		bool moveRight = rand(RandomSeed, coords);

		ivec2 coordsMoveDirection = coords + ivec2(moveRight ? +1 : -1, +1);
		if (materialAt(terrain, coordsMoveDirection) == 1) {  // air
			newCoords = coordsMoveDirection;
			return true;
		}
	}
	else if (material == 7) {  // water
		ivec2 coordsBelow = coords + ivec2(0, +1);
		if (materialAt(terrain, coordsBelow) == 1) {  // air
			newCoords = coordsBelow;
			return true;
		}

		// move randomly either left or right, but prefer one direction for each cell
		bool moveRight = (coords.y & 1) != 0;

		ivec2 coordsMoveDirection = coords + ivec2(moveRight ? +1 : -1, +1);
		if (materialAt(terrain, coordsMoveDirection) == 1) {  // air
			newCoords = coordsMoveDirection;
			return true;
		}

		ivec2 coordsMoveDirection2 = coords + ivec2(moveRight ? +1 : -1, 0);
		if (materialAt(terrain, coordsMoveDirection2) == 1) {  // air
			newCoords = coordsMoveDirection2;
			return true;
		}
	}

	return false;
}

/*
 * Check if any of the neighbors of the cell at the specified coordinates wants to occupy the cell.
 * Modify this function to customize the radius of interaction between cells.
 */
bool isMovingTo(sampler2D terrain, ivec2 coords, int material, out ivec2 sourceCoords) {
	if (material == 1) {  // air
		ivec2 rand = ivec2(
			rand(RandomSeed + 1, coords) ? +1 : -1,
			rand(RandomSeed + 2, coords) ? +1 : -1
		);

		// check if any of the neighbors is moving to occupy this cell
		for (int y = -1; y <= +1; y++) {
			for (int x = -1; x <= +1; x++) {
				if (ivec2(x, y) == ivec2(0, 0))
					continue;

				int neighborMaterial;
				ivec2 originalNeighborCoords;
				int neighborExtraData;
				ivec2 neighborCoords = coords + ivec2(x, y) * rand;
				getPixelData(terrain, neighborCoords, neighborMaterial, originalNeighborCoords, neighborExtraData);

				ivec2 newCoords;
				if (isMovingFrom(terrain, neighborCoords, neighborMaterial, newCoords) && newCoords == coords) {
					// neighbor moved to this cell
					sourceCoords = neighborCoords;
					return true;
				}
			}
		}
	}

	return false;
}

void fragment() {
	int material;
	ivec2 originalCoords;
	int extraData;
	ivec2 coords = ivec2(UV * vec2(TILE_SIZE));
	getPixelData(TEXTURE, coords, material, originalCoords, extraData);

	COLOR = makePixelData(material, originalCoords);

	ivec2 targetCoords;
	if (isMovingFrom(TEXTURE, coords, material, targetCoords)) {
		// the occupant is trying to move away...

		int targetMaterial;
		ivec2 targetOriginalCoords;
		int targetExtraData;
		getPixelData(TEXTURE, targetCoords, targetMaterial, targetOriginalCoords, targetExtraData);

		ivec2 sourceCoords;
		if (isMovingTo(TEXTURE, targetCoords, targetMaterial, sourceCoords) && sourceCoords == coords) {
			// the occupant moved away, clear this cell
			COLOR = makePixelData(1, coords);  // air
		}
	}
	else {
		ivec2 sourceCoords;
		if (isMovingTo(TEXTURE, coords, material, sourceCoords)) {
			// something is moving to this cell...

			int sourceMaterial;
			ivec2 sourceOriginalCoords;
			int sourceExtraData;
			getPixelData(TEXTURE, sourceCoords, sourceMaterial, sourceOriginalCoords, sourceExtraData);

			COLOR = makePixelData(sourceMaterial, sourceOriginalCoords);
		}
	}
}
