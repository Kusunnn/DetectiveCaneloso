shader_type canvas_item;
// Fondo de la vista de examen (HU-07): la foto del juego al abrir, desenfocada, oscurecida y con viñeta.
uniform sampler2D fondo : source_color, filter_linear_mipmap;
uniform float mezcla : hint_range(0.0, 1.0) = 1.0;     // Entrada y salida de la vista
uniform float oscuridad : hint_range(0.0, 1.0) = 0.88;
uniform float desenfoque : hint_range(0.0, 6.0) = 3.5;

void fragment() {
	vec3 color = textureLod(fondo, UV, desenfoque * mezcla).rgb;
	float distancia = distance(UV, vec2(0.5));
	float vineta = smoothstep(0.2, 0.8, distancia);
	color *= 1.0 - oscuridad * mezcla;
	color *= 1.0 - vineta * 0.7 * mezcla;
	// Un leve tono sepia, como el papel del HUD
	color = mix(color, vec3(dot(color, vec3(0.3, 0.59, 0.11))) * vec3(1.05, 0.97, 0.85), 0.5 * mezcla);
	COLOR = vec4(color, 1.0);
}
