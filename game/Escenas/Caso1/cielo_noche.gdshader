// Cielo de madrugada: degradado frío, estrellas que titilan y la luna en la dirección de la luz principal.
shader_type sky;

uniform vec3 color_cenit : source_color = vec3(0.012, 0.02, 0.05);
uniform vec3 color_horizonte : source_color = vec3(0.06, 0.09, 0.15);
uniform vec3 color_luna : source_color = vec3(0.85, 0.9, 1.0);
uniform float tamano_luna = 0.035;
uniform float densidad_estrellas = 0.0035;

float azar(vec3 p) {
	return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453);
}

void sky() {
	float alto = clamp(EYEDIR.y, 0.0, 1.0);
	vec3 color = mix(color_horizonte, color_cenit, pow(alto, 0.45));

	// Estrellas: celdas en una esfera, solo arriba del horizonte
	vec3 celda = floor(EYEDIR * 180.0);
	float e = azar(celda);
	if (e < densidad_estrellas * 100.0 * 0.01 && EYEDIR.y > 0.05) {
		float brillo = azar(celda + 7.0);
		float titileo = 0.7 + 0.3 * sin(TIME * (1.0 + brillo * 3.0) + brillo * 40.0);
		color += vec3(0.75, 0.8, 1.0) * brillo * titileo * smoothstep(0.05, 0.3, EYEDIR.y);
	}

	// Luna con halo, opuesta a la dirección en la que viaja su luz
	if (LIGHT0_ENABLED) {
		vec3 hacia_luna = LIGHT0_DIRECTION;
		float d = distance(EYEDIR, hacia_luna);
		color += color_luna * smoothstep(tamano_luna, tamano_luna * 0.85, d);
		color += color_luna * 0.12 * exp(-d * 9.0);
	}
	COLOR = color;
}
