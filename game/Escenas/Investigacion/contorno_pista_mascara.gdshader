// Primera pasada del contorno de pistas (ver contorno_pista.gdshader): no pinta nada visible,
// solo marca en el búfer de esténcil los píxeles que ocupa la pieza. El contorno se dibuja después
// únicamente fuera de esa marca, así nunca tapa el objeto.
shader_type spatial;
render_mode unshaded, depth_draw_never, cull_disabled, blend_mix;
stencil_mode write, compare_always, 1;

void fragment() {
	ALBEDO = vec3(0.0);
	ALPHA = 0.0;
}
