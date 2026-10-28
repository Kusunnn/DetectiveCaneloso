// Lago Espejo de cerca (HU-20.6): agua oscura y transparente en la orilla (se ve el fondo y lo que hay en él),
// con manchas de algas que brillan azul y laten despacio. Junto al embarcadero el brillo es más fuerte.
shader_type spatial;
render_mode blend_mix, depth_draw_always, cull_disabled, specular_schlick_ggx;

uniform sampler2D textura_profundidad : hint_depth_texture, filter_linear_mipmap;
uniform vec3 color_agua : source_color = vec3(0.012, 0.03, 0.045);
uniform vec3 color_algas : source_color = vec3(0.1, 0.55, 1.0);
uniform float intensidad = 3.0;
// Metros de agua hasta que ya no se ve el fondo
uniform float claridad = 2.6;
uniform float opacidad_minima = 0.25;
uniform vec2 centro_brillo = vec2(8.0, -22.0);
uniform float radio_brillo = 16.0;
uniform float extra_brillo = 2.5;

varying vec3 pos_mundo;

void vertex() {
	pos_mundo = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	VERTEX.y += sin(pos_mundo.x * 0.4 + TIME * 0.6) * 0.025 + sin(pos_mundo.z * 0.55 - TIME * 0.45) * 0.02;
}

float hash(vec2 p) { return fract(sin(dot(p, vec2(41.3, 289.1))) * 43758.5453); }

float ruido(vec2 p) {
	vec2 i = floor(p), f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1, 0)), f.x), mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), f.x), f.y);
}

void fragment() {
	// Cuánta agua hay entre la superficie y lo que está detrás
	float prof = texture(textura_profundidad, SCREEN_UV).r;
	vec4 fondo = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, prof, 1.0);
	fondo.xyz /= fondo.w;
	float espesor = max(0.0, VERTEX.z - fondo.z);
	float densidad = clamp(espesor / claridad, 0.0, 1.0);

	vec2 p = pos_mundo.xz * 0.12;
	float n = ruido(p + TIME * 0.03) * 0.6 + ruido(p * 2.3 - TIME * 0.05) * 0.4;
	float cerca = 1.0 - smoothstep(radio_brillo * 0.3, radio_brillo, distance(pos_mundo.xz, centro_brillo));
	float algas = smoothstep(0.55 - cerca * 0.07, 0.8, n);
	float latido = 0.75 + 0.25 * sin(TIME * 0.8 + n * 6.0);
	// Ondas finas para el reflejo de la luna
	float onda = ruido(pos_mundo.xz * 1.7 + TIME * 0.4) - ruido(pos_mundo.xz * 1.9 - TIME * 0.35);
	NORMAL = normalize(NORMAL + (VIEW_MATRIX * vec4(onda * 0.08, 0.0, onda * 0.08, 0.0)).xyz);

	ALBEDO = color_agua;
	ROUGHNESS = 0.06;
	METALLIC = 0.15;
	SPECULAR = 0.6;
	EMISSION = color_algas * algas * latido * intensidad * (1.0 + cerca * extra_brillo) * mix(0.3, 1.0, densidad);
	ALPHA = mix(opacidad_minima, 0.97, densidad);
}
