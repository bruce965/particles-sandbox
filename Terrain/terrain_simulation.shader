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

const ivec2 TILE_SIZE = ivec2(256, 256);

void _getPixelData(sampler2D terrain, vec2 coordsUv, out int material, out ivec2 originalCoords) {
	ivec4 data = ivec4(texture(terrain, coordsUv) * 255.);
	material = data.x + ((data.y & 0xFF) << 8);
	originalCoords = ivec2(data.z & 0xFF, data.a & 0xFF);
}

void getPixelData(sampler2D terrain, ivec2 coords, out int material, out ivec2 originalCoords) {
	vec2 fTile;
	vec2 coordsUv = modf((vec2(coords) / vec2(TILE_SIZE)) + vec2(1), fTile);
	ivec2 tile = ivec2(fTile) - ivec2(1);

	if (tile.y == -1) {
		if (tile.x == -1) {
			_getPixelData(TerrainTopLeft, coordsUv, material, originalCoords);
		}
		else if (tile.x == +1) {
			_getPixelData(TerrainTopRight, coordsUv, material, originalCoords);
		}
		else {
			_getPixelData(TerrainTop, coordsUv, material, originalCoords);
		}
	}
	else if (tile.y == +1) {
		if (tile.x == -1) {
			_getPixelData(TerrainBottomLeft, coordsUv, material, originalCoords);
		}
		else if (tile.x == +1) {
			_getPixelData(TerrainBottomRight, coordsUv, material, originalCoords);
		}
		else {
			_getPixelData(TerrainBottom, coordsUv, material, originalCoords);
		}
	}
	else {
		if (tile.x == -1) {
			_getPixelData(TerrainLeft, coordsUv, material, originalCoords);
		}
		else if (tile.x == +1) {
			_getPixelData(TerrainRight, coordsUv, material, originalCoords);
		}
		else {
			_getPixelData(terrain, coordsUv, material, originalCoords);
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
	getPixelData(terrain, coords, material, originalCoords);
	return material;
}

bool randCheckerboard(float seed, ivec2 coords) {
	// A checkerboard, `true` for a cell and `false` for its neighbors,
	// or `false for a cell and `true` for its neighbors.
	// The result is randomized depending on the seed.
	return ((coords.x + coords.y + int(seed * 2351.)) & 1) != 0;
}

/*
 * Compute the movement performed by the cell at the specified coordinates.
 * Customize this function to alter material physics.
 */
bool isMovingFrom(sampler2D terrain, float time, ivec2 coords, int material, out ivec2 newCoords) {
	if (material == 6) {  // sand
		ivec2 coordsBelow = coords + ivec2(0, +1);
		if (materialAt(terrain, coordsBelow) == 1) {  // air
			newCoords = coordsBelow;
			return true;
		}

		// move randomly either left or right (doesn't really need to be a checkerboard)
		bool moveRight = randCheckerboard(time, coords);

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
bool isMovingTo(sampler2D terrain, float time, ivec2 coords, int material, out ivec2 sourceCoords) {
	if (material == 1) {  // air
		ivec2 rand = ivec2(
			randCheckerboard(time + 1., coords) ? +1 : -1,
			randCheckerboard(time + 2., coords) ? +1 : -1
		);

		// check if any of the neighbors is moving to occupy this cell
		for (int y = -1; y <= +1; y++) {
			for (int x = -1; x <= +1; x++) {
				if (ivec2(x, y) == ivec2(0, 0))
					continue;

				int neighborMaterial;
				ivec2 originalNeighborCoords;
				ivec2 neighborCoords = coords + ivec2(x, y) * rand;
				getPixelData(terrain, neighborCoords, neighborMaterial, originalNeighborCoords);

				ivec2 newCoords;
				if (isMovingFrom(terrain, time, neighborCoords, neighborMaterial, newCoords) && newCoords == coords) {
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
	ivec2 coords = ivec2(UV * vec2(TILE_SIZE));
	getPixelData(TEXTURE, coords, material, originalCoords);

	COLOR = makePixelData(material, originalCoords);

	ivec2 targetCoords;
	if (isMovingFrom(TEXTURE, TIME, coords, material, targetCoords)) {
		// the occupant is trying to move away...

		int targetMaterial;
		ivec2 targetOriginalCoords;
		getPixelData(TEXTURE, targetCoords, targetMaterial, targetOriginalCoords);

		ivec2 sourceCoords;
		if (isMovingTo(TEXTURE, TIME, targetCoords, targetMaterial, sourceCoords) && sourceCoords == coords) {
			// the occupant moved away, clear this cell
			COLOR = makePixelData(1, coords);  // air
		}
	}
	else {
		ivec2 sourceCoords;
		if (isMovingTo(TEXTURE, TIME, coords, material, sourceCoords)) {
			// something is moving to this cell...

			int sourceMaterial;
			ivec2 sourceOriginalCoords;
			getPixelData(TEXTURE, sourceCoords, sourceMaterial, sourceOriginalCoords);

			COLOR = makePixelData(sourceMaterial, sourceOriginalCoords);
		}
	}
}
