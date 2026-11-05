shader_type canvas_item;
// Líneas de tracking de una cinta de video: bandas horizontales que corren, ruido y un leve corrimiento de la
// imagen. Solo al entrar y al salir de la cinta (intensidad de 0 a 1).

uniform sampler2D pantalla : hint_screen_texture, filter_linear_mipmap;
uniform float intensidad = 0.0;
uniform float tiempo = 0.0;

float azar(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
	vec2 uv = SCREEN_UV;
	// Una banda ancha que baja y otras finas que tiemblan
	float banda = smoothstep(0.06, 0.0, abs(fract(uv.y * 1.3 - tiempo * 0.9) - 0.5) - 0.42);
	float lineas = step(0.985 - 0.04 * intensidad, azar(vec2(floor(uv.y * 240.0), floor(tiempo * 30.0))));
	float corrimiento = (banda * 0.02 + lineas * 0.012 + (azar(vec2(floor(uv.y * 60.0), floor(tiempo * 24.0))) - 0.5) * 0.004) * intensidad;
	vec3 color = texture(pantalla, uv + vec2(corrimiento, 0.0)).rgb;
	// El color se separa un poco, como en un VHS
	color.r = mix(color.r, texture(pantalla, uv + vec2(corrimiento + 0.004 * intensidad, 0.0)).r, intensidad);
	float ruido = azar(uv * 400.0 + tiempo) * (banda * 0.35 + lineas * 0.5) * intensidad;
	COLOR = vec4(color + ruido, 1.0);
}
