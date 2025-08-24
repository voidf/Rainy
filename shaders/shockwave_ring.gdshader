shader_type canvas_item;

uniform float inner_radius : hint_range(0.0, 1.0) = 0.0;
uniform float outer_radius : hint_range(0.0, 1.0) = 1.0;
uniform vec4 ring_color : source_color = vec4(1.0, 0.5, 0.0, 1.0);
uniform float ring_thickness : hint_range(0.0, 1.0) = 0.1;

void fragment() {
    // 计算从中心到当前像素的距离
    vec2 center = vec2(0.5, 0.5);
    vec2 uv_centered = UV - center;
    float distance_from_center = length(uv_centered);

    // 计算环的厚度
    float ring_start = inner_radius;
    float ring_end = outer_radius;

    // 检查是否在环的范围内
    bool in_ring = distance_from_center >= ring_start && distance_from_center <= ring_end;

    // 计算环内的渐变效果
    float ring_progress = 0.0;
    if (in_ring) {
        ring_progress = (distance_from_center - ring_start) / (ring_end - ring_start);

        // 创建渐变效果，边缘更亮
        float edge_fade = 1.0 - abs(ring_progress - 0.5) * 2.0;
        edge_fade = smoothstep(0.0, 1.0, edge_fade);

        // 应用颜色和透明度
        COLOR = ring_color;
        COLOR.a *= edge_fade;
    } else {
        // 环外透明
        COLOR = vec4(0.0, 0.0, 0.0, 0.0);
    }
}
