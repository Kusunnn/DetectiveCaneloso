// Destello del vidrio de la lupa del menú: una franja diagonal blanca que recorre la máscara del vidrio.
shader_type canvas_item;
render_mode blend_add;

// Posición de la franja (0 = arriba a la izquierda del vidrio, 1 = abajo a la derecha)
uniform float avance = -1.0;
uniform float intensidad = 0.0;
uniform float ancho = 0.08;

void fragment() {
	vec4 m = texture(TEXTURE, UV);
	float vidrio = m.r * m.a;
	float d = (UV.x + UV.y) * 0.5;
	float banda = 1.0 - smoothstep(0.0, ancho, abs(d - avance));
	COLOR = vec4(vec3(1.0), vidrio * banda * intensidad * 0.75);
}
