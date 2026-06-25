shader_type canvas_item;
render_mode blend_add, unshaded;

uniform sampler2D color_gradient;
uniform sampler2D noiseTexture;
uniform float speed: hint_range(0.0, 5.0) = 1.5;
uniform float variation: hint_range(0.0, 1.0) = 0.24;
uniform float width: hint_range(0.0, 1.0) = 0.22;
uniform float alpha_strength: hint_range(0.0, 4.0) = 1.0;

void fragment(){
	vec2 noise_uv = vec2(UV.x + TIME * speed, UV.y - TIME * speed);
	float noise_sample = texture(noiseTexture, noise_uv).r;
	float x_offset = noise_sample * variation - variation / 2.0;
	vec4 arc = texture(color_gradient, vec2((UV.x - 0.5) / width + 0.5 + x_offset / width, UV.y));
	float mask = texture(TEXTURE, UV).a;
	COLOR = vec4(arc.rgb, arc.a * mask * alpha_strength);
}
