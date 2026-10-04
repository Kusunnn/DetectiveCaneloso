// Brillo de borde (fresnel) para marcar el objeto al que apunta el detective.
// Se aplica como material_overlay, así no toca los materiales originales.
shader_type spatial;
render_mode unshaded, blend_add, cull_back, depth_draw_never;

uniform vec4 color : source_color = vec4(1.0, 0.8, 0.2, 1.0);
uniform float intensidad = 0.6;

void fragment() {
	float borde = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 2.5);
	float pulso = 0.75 + 0.25 * sin(TIME * 5.0);
	ALBEDO = color.rgb;
	ALPHA = borde * intensidad * pulso + 0.02;
}
