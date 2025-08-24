shader_type canvas_item;

// 0~1 控制透明圈的半径（从中心向外扩散）
uniform float progress : hint_range(0.0, 1.0) = 0.0;

// 可选：边缘柔化宽度（单位为UV距离，0=硬边）
uniform float softness : hint_range(0.0, 0.1) = 0.0;

void fragment() {
    vec4 base = texture(TEXTURE, UV);

    // 计算从UV中心(0.5,0.5)的距离
    float d = distance(UV, vec2(0.5));

    // 以能覆盖到纹理角落的最大半径作为 1.0 的参考
    // float max_r = length(vec2(0.5, 0.5)); // ≈ 0.7071
    float max_r = 0.5;
    float r = progress * max_r;

    // inside: d < r 透明；outside: 保留原像素
    // 用 smoothstep 让边缘可选地更柔和
    float keep_mask = smoothstep(r - softness, r + softness, d);

    // base *= tint_color; // ShaderMaterial 的颜色（可选）
    base *= COLOR;      // 节点的 Modulate / Self Modulate

    base.a *= keep_mask;

    COLOR = base;
}
