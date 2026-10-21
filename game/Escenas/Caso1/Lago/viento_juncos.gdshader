// Juncos y pasto alto que se mecen con el viento (HU-20.6). Para copias de SembradoMultiMesh: la base queda fija
// y la punta se mueve; cada copia va desfasada según su posición en el mundo.
shader_type spatial;
render_mode cull_disabled;

uniform vec3 color_base : source_color = vec3(0.16, 0.2, 0.08);
uniform vec3 color_punta : source_color = vec3(0.42, 0.42, 0.2);
uniform float alto_modelo = 0.26;
uniform float fuerza = 0.09;
uniform float velocidad = 1.3;

varying float altura;

void vertex() {
	altura = clamp(VERTEX.y / alto_modelo, 0.0, 1.0);
	vec3 mundo = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
	float fase = mundo.x * 0.35 + mundo.z * 0.21;
	float rafaga = 0.6 + 0.4 * sin(TIME * 0.31 + mundo.x * 0.05);
	float mov = altura * altura * fuerza * rafaga;
	VERTEX.x += sin(TIME * velocidad + fase) * mov;
	VERTEX.z += cos(TIME * velocidad * 0.8 + fase * 1.3) * mov * 0.6;
}

void fragment() {
	ALBEDO = mix(color_base, color_punta, altura);
	ROUGHNESS = 0.9;
	BACKLIGHT = vec3(0.12, 0.14, 0.05);
}
