shader_type canvas_item;
// Grano de película animado sobre el fondo del menú.

uniform float intensidad = 0.09;
uniform float velocidad = 24.0;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
	float n = hash(floor(FRAGCOORD.xy) + floor(TIME * velocidad));
	COLOR = vec4(vec3(n), intensidad);
}
