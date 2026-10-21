// Suelo del Lago y el Bosque (HU-20.6): tierra del bosque con agujas de pino, senderos de tierra, arena húmeda en la
// orilla y pasto en la colina. La mezcla viene en el color de cada vértice del terreno (R camino, G arena, B pasto).
shader_type spatial;

uniform sampler2D bosque_color : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D bosque_normal : hint_normal, filter_linear_mipmap, repeat_enable;
uniform sampler2D tierra_color : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D tierra_normal : hint_normal, filter_linear_mipmap, repeat_enable;
uniform sampler2D pasto_color : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D pasto_normal : hint_normal, filter_linear_mipmap, repeat_enable;
uniform float metros_por_textura = 3.0;
uniform vec3 color_arena : source_color = vec3(0.62, 0.55, 0.42);
uniform float nivel_agua = -0.25;
uniform vec3 tinte = vec3(0.78, 0.82, 0.8);
uniform float saturacion = 0.65;

varying vec3 pos_mundo;
varying vec3 mezcla;

void vertex() {
	pos_mundo = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	mezcla = COLOR.rgb;
}

float ruido(vec2 p) {
	return fract(sin(dot(floor(p), vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
	vec2 uv = pos_mundo.xz / metros_por_textura;
	vec2 uv2 = uv * 0.37 + vec2(0.31, 0.17);
	float var = mix(ruido(pos_mundo.xz * 0.11), ruido(pos_mundo.xz * 0.11 + 0.5), 0.5);

	vec3 bosque = mix(texture(bosque_color, uv).rgb, texture(bosque_color, uv2).rgb, 0.35);
	vec3 tierra = texture(tierra_color, uv * 1.3).rgb;
	vec3 pasto = texture(pasto_color, uv * 0.8).rgb;
	// La arena es la tierra aclarada; más oscura y lisa donde la moja el agua
	float gris_t = dot(tierra, vec3(0.333));
	vec3 arena = color_arena * (0.75 + gris_t * 0.6);
	float mojado = 1.0 - smoothstep(nivel_agua, nivel_agua + 0.18, pos_mundo.y);
	arena = mix(arena, arena * 0.45, mojado);

	vec3 color = mix(bosque, pasto, mezcla.b);
	color = mix(color, arena, mezcla.g);
	color = mix(color, tierra, smoothstep(0.15, 0.75, mezcla.r));
	color *= 0.85 + var * 0.25;
	// Bajo el agua: lodo oscuro del fondo del lago
	float fondo = 1.0 - smoothstep(nivel_agua - 0.5, nivel_agua - 0.05, pos_mundo.y);
	color = mix(color, vec3(0.05, 0.06, 0.05) * (0.8 + var * 0.4), fondo);
	float gris = dot(color, vec3(0.299, 0.587, 0.114));
	ALBEDO = mix(vec3(gris), color, saturacion) * tinte;

	vec3 n_bosque = texture(bosque_normal, uv).rgb;
	vec3 n_tierra = texture(tierra_normal, uv * 1.3).rgb;
	vec3 n_pasto = texture(pasto_normal, uv * 0.8).rgb;
	vec3 n = mix(mix(n_bosque, n_pasto, mezcla.b), n_tierra, max(smoothstep(0.15, 0.75, mezcla.r), mezcla.g * 0.6));
	NORMAL_MAP = mix(n, vec3(0.5, 0.5, 1.0), mojado * 0.7);
	NORMAL_MAP_DEPTH = 0.8;
	ROUGHNESS = mix(0.95, 0.35, mojado);
}
