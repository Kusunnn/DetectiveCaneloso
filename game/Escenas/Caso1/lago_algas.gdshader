// Lago Espejo visto de lejos: agua oscura con manchas de algas que brillan azul y laten despacio.
shader_type spatial;
render_mode cull_disabled;

uniform vec3 color_agua : source_color = vec3(0.01, 0.025, 0.04);
uniform vec3 color_algas : source_color = vec3(0.1, 0.55, 1.0);
uniform float intensidad = 2.2;

varying vec3 pos_mundo;

void vertex() {
	pos_mundo = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	VERTEX.y += sin(pos_mundo.x * 0.4 + TIME * 0.6) * 0.04;
}

float hash(vec2 p) { return fract(sin(dot(p, vec2(41.3, 289.1))) * 43758.5453); }

float ruido(vec2 p) {
	vec2 i = floor(p), f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1, 0)), f.x), mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), f.x), f.y);
}

void fragment() {
	vec2 p = pos_mundo.xz * 0.12;
	float n = ruido(p + TIME * 0.03) * 0.6 + ruido(p * 2.3 - TIME * 0.05) * 0.4;
	float algas = smoothstep(0.55, 0.8, n);
	float latido = 0.75 + 0.25 * sin(TIME * 0.8 + n * 6.0);
	ALBEDO = color_agua;
	ROUGHNESS = 0.08;
	METALLIC = 0.2;
	EMISSION = color_algas * algas * latido * intensidad;
}
