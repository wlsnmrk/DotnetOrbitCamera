shader_type spatial;
render_mode cull_disabled;

uniform float object_width;
uniform float object_depth;

void fragment() {
	int even = int(floor(UV.x * object_width) + floor(UV.y * object_depth)) % 2;
	if (even == 0)
	{
		ALBEDO = vec3(.5, .5, .6);
	}
	else
	{
		ALBEDO = vec3(.8, .8, .9);
	}
}
