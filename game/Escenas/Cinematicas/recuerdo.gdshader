shader_type canvas_item;
// Recuerdo de la cinemática de la cinta: la imagen del juego, fría y deslavada, como una reconstrucción.

uniform sampler2D pantalla : hint_screen_texture, filter_linear_mipmap;
uniform float fuerza = 0.85;
uniform float contraste = 1.12;

void fragment() {
	vec3 color = texture(pantalla, SCREEN_UV).rgb;
	float luz = dot(color, vec3(0.299, 0.587, 0.114));
	vec3 frio = vec3(luz * 0.8, luz * 0.95, luz * 1.18) + vec3(0.008, 0.012, 0.03);
	vec3 final_ = mix(color, frio, fuerza);
	final_ = (final_ - 0.5) * contraste + 0.5;
	COLOR = vec4(clamp(final_, 0.0, 1.0), 1.0);
}
