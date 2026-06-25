shader_type canvas_item;
render_mode blend_mix, unshaded;

uniform sampler2D noise : repeat_enable;
uniform float blood_coef : hint_range(0.0, 1.0) = 0.5;
uniform vec3 color : source_color = vec3(0.75, 0.02, 0.04);
uniform vec2 flow_dir = vec2(0.06, -0.10);
uniform float flow_speed = 0.45;
uniform float alpha_strength : hint_range(0.0, 2.0) = 1.0;

void fragment() {
	vec4 curr_texture = texture(TEXTURE, UV);
	vec4 noise_texture = texture(noise, UV + flow_dir * TIME * flow_speed);
	float is_blood = step(noise_texture.r, blood_coef);
	noise_texture.r = mix(noise_texture.r, curr_texture.r, is_blood);
	vec4 blood_output = vec4(noise_texture.rgb * color, curr_texture.a * alpha_strength);
	COLOR = mix(curr_texture, blood_output, is_blood);
}
