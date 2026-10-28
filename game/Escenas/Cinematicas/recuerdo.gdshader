shader_type canvas_item;
// Recuerdo de la cinemática de la cinta: la imagen del juego, fría y deslavada, como una reconstrucción.
// El rojo intenso (la luz de REC, el contador) es lo único que conserva su color.

uniform sampler2D pantalla : hint_screen_texture, filter_linear_mipmap;
uniform float fuerza = 0.85;
uniform float contraste = 1.12;
// Cuánto más rojo que verde y azul debe ser un píxel para quedarse con su color
uniform float rojo_desde = 0.4;
uniform float rojo_hasta = 0.6;

void fragment() {
	vec3 color = texture(pantalla, SCREEN_UV).rgb;
	float luz = dot(color, vec3(0.299, 0.587, 0.114));
	vec3 frio = vec3(luz * 0.8, luz * 0.95, luz * 1.18) + vec3(0.008, 0.012, 0.03);
	float rojo = smoothstep(rojo_desde, rojo_hasta, color.r - max(color.g, color.b)) * step(max(color.g, color.b), color.r * 0.4) * smoothstep(0.7, 0.9, color.r);
	vec3 final_ = mix(color, frio, fuerza * (1.0 - rojo));
	final_ = (final_ - 0.5) * contraste + 0.5;
	COLOR = vec4(clamp(final_, 0.0, 1.0), 1.0);
}
