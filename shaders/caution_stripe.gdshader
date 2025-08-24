shader_type canvas_item;
render_mode unshaded;

// 颜色
uniform vec4 color_yellow : source_color = vec4(1.00, 0.85, 0.00, 1.0);
uniform vec4 color_black  : source_color = vec4(0.08, 0.08, 0.08, 1.0);

// 斜纹角度（度）与水平滚动速度（UV/秒；>0 向右）
uniform float angle_deg : hint_range(-90.0, 90.0, 0.1) = 45.0;
uniform float speed     : hint_range(-2.0, 2.0, 0.001) = 0.10;

// 以“像素”为单位的单色条带宽度（黄或黑各自的宽度）
uniform float stripe_px : hint_range(1.0, 256.0, 1.0) = 8.0;

// 可选的边缘柔化
uniform float edge_softness : hint_range(0.0, 0.01, 0.0005) = 0.0;

void fragment() {
    // 1) 使用屏幕坐标（0..1），并做“屏幕水平”滚动
    vec2 suv = SCREEN_UV;
    suv.x += TIME * speed;

    // 2) 旋转坐标系来得到斜纹（以屏幕中心为旋转中心）
    float a = radians(angle_deg);
    float c = cos(a);
    float s = sin(a);
    // Godot 矩阵按“列”构造
    mat2 R = mat2(vec2(c, s), vec2(-s, c));
    vec2 p = R * (suv - vec2(0.5));

    // 3) 把像素宽度换算为屏幕UV宽度：px * (1/屏幕宽)
    float stripe_uv = stripe_px * SCREEN_PIXEL_SIZE.x;

    // 4) 以等宽产生 50/50 黄黑条带（每跨过 stripe_uv，相位加 PI）
    float phase = (p.x / max(stripe_uv, 1e-5)) * PI;
    float wave = sin(phase);

    // 5) 基于导数的抗锯齿 + 可选柔化
    float aa = max(fwidth(phase), edge_softness);
    float mask = smoothstep(-aa, aa, wave); // wave<0 黑，>0 黄

    vec3  rgb   = mix(color_black.rgb, color_yellow.rgb, mask);
    float alpha = mix(color_black.a,   color_yellow.a,   mask);
    COLOR = vec4(rgb, alpha);
}
