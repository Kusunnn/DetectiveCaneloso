shader_type canvas_item;
// Viñeta de la cinemática: oscurece las orillas y le da un tono frío a las sombras.

uniform float fuerza = 0.75;
uniform float radio = 0.62;

void fragment() {
	vec2 centrado = (UV - 0.5) * vec2(1.0, 0.82);
	float d = length(centrado);
	float v = smoothstep(radio * 0.45, radio, d) * fuerza;
	COLOR = vec4(0.0, 0.01, 0.025, v);
}
