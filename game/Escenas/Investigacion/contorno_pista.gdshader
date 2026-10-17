// Contorno ámbar alrededor de una pista que todavía no se ha examinado (ayuda visual).
// Solo se ve de cerca: aparece al acercarse a menos de "distancia_max" metros y se desvanece al alejarse.
//
// Segunda pasada (next_pass de contorno_pista_mascara.gdshader): dibuja la pieza un poco más grande,
// desde su centro y con grosor constante en pantalla, pero solo donde la máscara NO marcó la pieza.
// Así queda únicamente el borde exterior, aunque el modelo sea plano o traiga las caras invertidas.
shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never, blend_mix;
stencil_mode read, compare_not_equal, 1;

uniform vec4 color : source_color = vec4(0.937, 0.792, 0.031, 1.0);
uniform float grosor = 0.011;
uniform float distancia_max = 4.5;
// Centro de la pieza (en sus coordenadas locales); lo pone ContornoPistas
instance uniform vec3 centro = vec3(0.0);

varying float distancia;

void vertex() {
	vec3 en_vista = (MODELVIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	distancia = length(en_vista);
	VERTEX += normalize(VERTEX - centro + vec3(0.0001)) * grosor * distancia;
}

void fragment() {
	float cerca = 1.0 - smoothstep(distancia_max * 0.75, distancia_max, distancia);
	ALBEDO = color.rgb;
	ALPHA = color.a * cerca;
}
