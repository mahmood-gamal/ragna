// Dims and blurs whatever is drawn below it: the world behind a dialogue scene.
shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;
// Mipmap level to sample. Higher is blurrier.
uniform float blur : hint_range(0.0, 6.0) = 2.5;
// 0 keeps the world as bright as it is, 1 makes it black.
uniform float dim : hint_range(0.0, 1.0) = 0.45;

void fragment() {
	vec3 world = textureLod(screen_texture, SCREEN_UV, blur).rgb;
	COLOR = vec4(world * (1.0 - dim), 1.0);
}
