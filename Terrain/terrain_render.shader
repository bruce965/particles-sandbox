shader_type canvas_item;
render_mode unshaded;

uniform sampler2D Materials: hint_albedo;

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

int materialAt(sampler2D terrain, ivec2 coords) {
	int material;
	ivec2 originalCoords;
	int extraData;
	getPixelData(terrain, coords, material, originalCoords, extraData);
	return material;
}

vec4 materialColor(int material, ivec2 originalCoords) {
	if (material == 1)  // air
		return vec4(0, 0, 0, 0);

	if (material == 0)
		material = 1;

	int row = (material - 1) % 4;
	int column = ((material - 1) / 4) * 2;
	return texture(
		Materials,
		vec2(
			float(column) * .25 + mod(float(originalCoords.x) / 256., .25),
			float(row)    * .25 + mod(float(originalCoords.y) / 256., .25)
		)
	);
}

vec4 materialTop1Color(int material, vec2 uv) {
	if (material == 1)  // air
		return vec4(0, 0, 0, 0);

	if (material == 0)
		material = 1;

	int row = (material - 1) % 4;
	int column = ((material - 1) / 4) * 2 + 1;
	return texture(Materials, vec2(float(column) * .25 + mod(uv.x, .03125), float(row) * .25));
}

vec4 materialTop2Color(int material, vec2 uv) {
	if (material == 1)  // air
		return vec4(0, 0, 0, 0);

	if (material == 0)
		material = 1;

	int row = (material - 1) % 4;
	int column = ((material - 1) / 4) * 2 + 1;
	return texture(Materials, vec2(float(column) * .25 + mod(uv.x, .03125), float(row) * .25 + .00390625));
}

vec4 materialBottom1Color(int material, vec2 uv) {
	if (material == 1)  // air
		return vec4(0, 0, 0, 0);

	if (material == 0)
		material = 1;

	int row = (material - 1) % 4;
	int column = ((material - 1) / 4) * 2 + 1;
	return texture(Materials, vec2(float(column) * .25 + mod(uv.x, .03125), float(row) * .25 + .01171875));
}

vec4 materialBottom2Color(int material, vec2 uv) {
	if (material == 1)  // air
		return vec4(0, 0, 0, 0);

	if (material == 0)
		material = 1;

	int row = (material - 1) % 4;
	int column = ((material - 1) / 4) * 2 + 1;
	return texture(Materials, vec2(float(column) * .25 + mod(uv.x, .03125), float(row) * .25 + .0078125));
}

vec4 materialLeft1Color(int material, vec2 uv) {
	if (material == 1)  // air
		return vec4(0, 0, 0, 0);

	if (material == 0)
		material = 1;

	int row = (material - 1) % 4;
	int column = ((material - 1) / 4) * 2 + 1;
	return texture(Materials, vec2(float(column) * .25, float(row) * .25 + .015625 + mod(uv.y, .03125)));
}

vec4 materialLeft2Color(int material, vec2 uv) {
	if (material == 1)  // air
		return vec4(0, 0, 0, 0);

	if (material == 0)
		material = 1;

	int row = (material - 1) % 4;
	int column = ((material - 1) / 4) * 2 + 1;
	return texture(Materials, vec2(float(column) * .25 + .00390625, float(row) * .25 + .015625 + mod(uv.y, .03125)));
}

vec4 materialRight1Color(int material, vec2 uv) {
	if (material == 1)  // air
		return vec4(0, 0, 0, 0);

	if (material == 0)
		material = 1;

	int row = (material - 1) % 4;
	int column = ((material - 1) / 4) * 2 + 1;
	return texture(Materials, vec2(float(column) * .25 + .01171875, float(row) * .25 + .015625 + mod(uv.y, .03125)));
}

vec4 materialRight2Color(int material, vec2 uv) {
	if (material == 1)  // air
		return vec4(0, 0, 0, 0);

	if (material == 0)
		material = 1;

	int row = (material - 1) % 4;
	int column = ((material - 1) / 4) * 2 + 1;
	return texture(Materials, vec2(float(column) * .25 + .0078125, float(row) * .25 + .015625 + mod(uv.y, .03125)));
}

vec4 blend(vec4 background, vec4 foreground) {
	float outA = background.a * (1. - foreground.a) + foreground.a;
	return vec4(mix(background.rgb, foreground.rgb, foreground.a), outA);
	/*
	// https://en.wikipedia.org/wiki/Alpha_compositing#Alpha_blending
	float outA = foreground.a + background.a * (1. - foreground.a);

	if (outA == 0.)
		return vec4(0.);

	return vec4(
		(foreground.rgb * foreground.a + background.rgb * background.a * (1. - foreground.a)) / outA,
		outA
	);*/
}

void fragment() {
	int material;
	ivec2 originalCoords;
	int extraData;
	ivec2 coords = ivec2(UV * vec2(TILE_SIZE));
	getPixelData(TEXTURE, coords, material, originalCoords, extraData);

	COLOR = materialColor(material, originalCoords);

	if (materialAt(TEXTURE, coords + ivec2(0, -1)) != material)
		COLOR = blend(COLOR, materialTop1Color(material, UV));
	else if (materialAt(TEXTURE, coords + ivec2(0, -2)) != material)
		COLOR = blend(COLOR, materialTop2Color(material, UV));
	else if (materialAt(TEXTURE, coords + ivec2(0, +1)) != material)
		COLOR = blend(COLOR, materialBottom1Color(material, UV));
	else if (materialAt(TEXTURE, coords + ivec2(0, +2)) != material)
		COLOR = blend(COLOR, materialBottom2Color(material, UV));
	else if (materialAt(TEXTURE, coords + ivec2(-1, 0)) != material)
		COLOR = blend(COLOR, materialLeft1Color(material, UV));
	else if (materialAt(TEXTURE, coords + ivec2(-2, 0)) != material)
		COLOR = blend(COLOR, materialLeft2Color(material, UV));
	else if (materialAt(TEXTURE, coords + ivec2(+1, 0)) != material)
		COLOR = blend(COLOR, materialRight1Color(material, UV));
	else if (materialAt(TEXTURE, coords + ivec2(+2, 0)) != material)
		COLOR = blend(COLOR, materialRight2Color(material, UV));
}
