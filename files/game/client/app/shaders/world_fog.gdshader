shader_type canvas_item;

// Centro en píxeles (jugador en pantalla).
uniform vec2 u_center = vec2(960.0, 540.0);
uniform vec2 u_viewport = vec2(1920.0, 1080.0);
// Radio del halo iluminado y hasta dónde llega la niebla.
uniform float u_inner_radius = 48.0;
uniform float u_outer_radius = 420.0;
uniform float u_darkness = 0.92;
// Curva de caída: mayor = niebla más concentrada en los bordes.
uniform float u_falloff_power = 2.6;
// Tinte de la niebla (gris azulado).
uniform vec3 u_fog_color = vec3(0.42, 0.48, 0.58);
// Brillo sutil en el centro (iluminación del personaje).
uniform float u_center_glow = 0.12;

void fragment() {
	vec2 pixel = UV * u_viewport;
	float dist = distance(pixel, u_center);

	float span = max(u_outer_radius - u_inner_radius, 1.0);
	float t = clamp((dist - u_inner_radius) / span, 0.0, 1.0);

	// Niebla densa en el perímetro; caída abrupta cerca del jugador.
	float fog = pow(t, u_falloff_power);

	// Halo claro alrededor del personaje (centro más iluminado).
	float glow = 1.0 - smoothstep(0.0, u_inner_radius * 1.15, dist);
	glow = glow * glow * u_center_glow;

	float alpha = fog * u_darkness;
	alpha = clamp(alpha - glow, 0.0, 1.0);

	vec3 rgb = u_fog_color * (0.55 + fog * 0.45);
	COLOR = vec4(rgb, alpha);
}
