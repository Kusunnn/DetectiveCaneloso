// Suelo del campamento: tierra del bosque con agujas de pino, manchas de pasto y caminos de tierra.
// La máscara (R = caminos, G = pasto) cubre 128 × 128 m centrados en el origen; fuera de ella, bosque.
shader_type spatial;

uniform sampler2D bosque_color : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D bosque_normal : hint_normal, filter_linear_mipmap, repeat_enable;
uniform sampler2D tierra_color : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D tierra_normal : hint_normal, filter_linear_mipmap, repeat_enable;
uniform sampler2D pasto_color : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D pasto_normal : hint_normal, filter_linear_mipmap, repeat_enable;
uniform sampler2D mascara : filter_linear, repeat_disable;
uniform float extension_mascara = 128.0;
uniform float metros_por_textura = 3.0;
// Tono general: el suelo low-poly no debe verse fotográfico
uniform vec3 tinte = vec3(0.78, 0.82, 0.8);
uniform float saturacion = 0.65;

varying vec3 pos_mundo;

void vertex() {
	pos_mundo = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

float ruido(vec2 p) {
	return fract(sin(dot(floor(p), vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
	vec2 uv = pos_mundo.xz / metros_por_textura;
	vec2 uv_m = pos_mundo.xz / extension_mascara + 0.5;
	vec2 m = texture(mascara, uv_m).rg;
	// Variación a gran escala para que no se note la repetición
	float var = mix(ruido(pos_mundo.xz * 0.11), ruido(pos_mundo.xz * 0.11 + 0.5), 0.5);
	vec2 uv2 = uv * 0.37 + vec2(0.31, 0.17);

	vec3 bosque = mix(texture(bosque_color, uv).rgb, texture(bosque_color, uv2).rgb, 0.35);
	vec3 tierra = texture(tierra_color, uv * 1.3).rgb;
	vec3 pasto = texture(pasto_color, uv * 0.8).rgb;
	vec3 color = mix(bosque, pasto, m.g * 0.8);
	color = mix(color, tierra, smoothstep(0.15, 0.75, m.r));
	color *= 0.85 + var * 0.25;

	float gris = dot(color, vec3(0.299, 0.587, 0.114));
	ALBEDO = mix(vec3(gris), color, saturacion) * tinte;

	vec3 n_bosque = texture(bosque_normal, uv).rgb;
	vec3 n_tierra = texture(tierra_normal, uv * 1.3).rgb;
	vec3 n_pasto = texture(pasto_normal, uv * 0.8).rgb;
	NORMAL_MAP = mix(mix(n_bosque, n_pasto, m.g * 0.8), n_tierra, smoothstep(0.15, 0.75, m.r));
	NORMAL_MAP_DEPTH = 0.8;
	ROUGHNESS = 0.95;
}
