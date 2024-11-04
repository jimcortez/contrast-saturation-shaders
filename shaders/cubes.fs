/*{
  "CREDIT": "by Jim Cortez",
  "CATEGORIES": [
    "cubes"
  ],
  "INPUTS": [
    {
        "NAME": "rate",
        "TYPE": "float",
        "DEFAULT": 0.5,
        "MIN": 0,
        "MAX": 1
    },
    {
        "NAME": "main_color",
        "TYPE": "color",
        "DEFAULT": [
            1.0,
            0.0,
            0.0,
            1.0
        ]
    },
    {
        "NAME": "color_rotation",
        "TYPE": "float",
        "DEFAULT": 0.5,
         "MIN": 0,
         "MAX": 360
    },
    {
        "NAME": "saturation",
        "TYPE": "float",
        "DEFAULT": 1.0,
        "MIN": 0,
        "MAX": 1
    },
    {
        "NAME": "saturation_1",
        "TYPE": "float",
        "DEFAULT": 1.0,
        "MIN": 0,
        "MAX": 1
    },
    {
        "NAME": "saturation_2",
        "TYPE": "float",
        "DEFAULT": 1.0,
        "MIN": 0.0,
        "MAX": 1.0
    },
    {
        "NAME": "saturation_3",
        "TYPE": "float",
        "DEFAULT": 1.0,
        "MIN": 0,
        "MAX": 1
    },
    {
        "NAME": "saturation_4",
        "TYPE": "float",
        "DEFAULT": 1.0,
        "MIN": 0,
        "MAX": 1
    },
    {
        "NAME": "cluster_size",
        "TYPE": "float",
        "DEFAULT": 5.0,
        "MIN": 1.0,
        "MAX": 10.0
    },
    {
        "NAME": "border_width",
        "TYPE": "float",
        "DEFAULT": 0.01,
        "MIN": 0,
        "MAX": 0.1
    },
    {
        "NAME": "max_box_size",
        "TYPE": "float",
        "DEFAULT": 0.6,
        "MIN": 0.1,
        "MAX": 1.0
    },
    {
        "NAME": "background_color",
        "TYPE": "color",
        "DEFAULT": [
            0.0,
            0.0,
            0.0,
            1.0
        ]
    },
    {
        "NAME": "zoom",
        "TYPE": "float",
        "DEFAULT": 2.0,
        "MIN": 0.0,
        "MAX": 10.0
    },
    {
        "NAME": "color_steps",
        "TYPE": "float",
        "DEFAULT": 4.0,
        "MIN": 0.0,
        "MAX": 10.0
    }
  ],
  "DESCRIPTION": "Cubes to demonstrate saturation and harmonious tetrad colors"
}*/

///////////////////////////////////////////
// CubeCluster by Jim Cortez
//
// Creative Commons Attribution-NonCommercial-ShareAlike 3.0
//
// based on :
// * CubeCluster by mojovideotech https://editor.isf.video/shaders/5e7a80227c113618206deb5e
// * glslsandbox.com/e#31945.2
///////////////////////////////////////////

#ifdef GL_ES
precision mediump float;
#endif

#define phi 1.618033988749895
#define PI 3.14159265359
#define INFINITY 1e3

bool intersects(vec3 ro, vec3 rd, vec3 box_center, float box_size, out float t_intersection)
{
    vec3 t1 = (box_center - vec3(box_size) - ro) / rd;
    vec3 t2 = (box_center + vec3(box_size) - ro) / rd;
    vec3 t_min = min(t1, t2);
    vec3 t_max = max(t1, t2);
    float t_near = max(t_min.x, max(t_min.y, t_min.z));
    float t_far = min(t_max.x, min(t_max.y, t_max.z));
    if (t_near > t_far || t_far < 1.0)
        return false;
    t_intersection = t_near;
    return true;
}

mat3 camera(vec3 e, vec3 la)
{
    vec3 roll = vec3(0, 1, 0);
    vec3 f = normalize(la - e);
    vec3 r = normalize(cross(roll, f));
    vec3 u = normalize(mix(cross(e, f), cross(f, r), 0.75));
    return mat3(r, u, f);
}

vec4 rgbToHsv(vec4 rgbColor) {
    float r = rgbColor.r;
    float g = rgbColor.g;
    float b = rgbColor.b;
    float alpha = rgbColor.a;  // Preserve the alpha channel

    float maxC = max(r, max(g, b));
    float minC = min(r, min(g, b));
    float delta = maxC - minC;

    float hue = 0.0;
    if (delta > 0.0) {
        if (maxC == r) {
            hue = mod((g - b) / delta, 6.0);
        } else if (maxC == g) {
            hue = ((b - r) / delta) + 2.0;
        } else {
            hue = ((r - g) / delta) + 4.0;
        }
        hue /= 6.0;  // Normalize to [0, 1]
    }

    float saturation = (maxC == 0.0) ? 0.0 : delta / maxC;
    float value = maxC;

    // Ensure the hue is non-negative
    if (hue < 0.0) {
        hue += 1.0;
    }

    return vec4(hue, saturation, value, alpha);
}

vec4 hsvToRgb(vec4 c)
{
    float h = c.x * 6.0;
    float s = c.y;
    float v = c.z;

    int i = int(floor(h));
    float f = h - float(i);
    float p = v * (1.0 - s);
    float q = v * (1.0 - f * s);
    float t = v * (1.0 - (1.0 - f) * s);

    if (i == 0)
        return vec4(v, t, p, c.a);
    else if (i == 1)
        return vec4(q, v, p, c.a);
    else if (i == 2)
        return vec4(p, v, t, c.a);
    else if (i == 3)
        return vec4(p, q, v, c.a);
    else if (i == 4)
        return vec4(t, p, v, c.a);
    else
        return vec4(v, p, q, c.a);
}

vec4 rotateHue(vec4 hsvColor, float angle)
{
    hsvColor.x = mod(hsvColor.x + angle / 360.0, 1.0);
    return hsvColor;
}

vec4 scaleSaturation(vec4 hsvColor, float saturation_perc)
{
    hsvColor.y = clamp(hsvColor.y*saturation_perc, 0.0, 1.0);
    return hsvColor;
}

void main(void)
{
    float a = rate * TIME, t_intersection = INFINITY, inside = 0.0;
    vec2 uv = (2.5 * gl_FragCoord.xy - RENDERSIZE) / min(RENDERSIZE.x, RENDERSIZE.y);
    vec3 ro = 8.0 * vec3(cos(a), sin(0.5 * TIME), -sin(a));
    vec3 rd = camera(ro, vec3(0)) * normalize(vec3(uv, 2.));
    for (float i = 0.; i < 10.0; i++)
    {
        if (i == cluster_size)
            break;

        for (float j = 0.; j < 10.0; j++)
        {
            if (j == cluster_size)
                break;

            for (float k = 0.; k < 10.0; k++)
            {
                if (k == cluster_size)
                    break;

                vec3 p = 2.0 * (vec3(i, j, k) - 0.5 * vec3(cluster_size - 3.0, cluster_size, cluster_size - 2.0));
                float l = length(p);
                float s = clamp(max_box_size * pow(.5 + .5 * sin(rate * rate * TIME * phi - j * l), k + 0.5), 0.1, 1.0);
                float t = 0.;

                if (intersects(ro, rd, p, s, t) && t < t_intersection)
                {
                    t_intersection = t;
                    vec3 n = ro + rd * t_intersection - p;
                    vec3 normal = smoothstep(vec3(s - border_width), vec3(s), n) + smoothstep(vec3(s - border_width), vec3(s), -n);
                    inside = smoothstep(1.05, 1.0, normal.x + normal.y + normal.z);
                }
            }
        }
    }

    if (t_intersection == INFINITY) //background
    {
        gl_FragColor = background_color;
    }
    else if (inside < 1.0) // box border
    {
        gl_FragColor = vec4(0.0,0.0,0.0,0.5);
    }
    else
    {
        vec4 hsvColor = scaleSaturation(rotateHue(rgbToHsv(main_color), color_rotation), saturation);

        float max_intersection = 15.0;
        float step_size = 3.0;

        // float angle_step = 360.0/color_steps;
        // for(float color_plane=0.0; color_plane<10.0; color_plane++){
        //     if(color_plane >= color_steps) break;

        //     if(color_plane == 0.0 && t_intersection >= max_intersection){
        //          hsvColor = scaleSaturation(hsvColor, saturation_1);
        //          break;
        //     } else{
        //         if (t_intersection < (max_intersection-(step_size*color_plane)) && (max_intersection-(step_size*(color_plane+1.0))) < t_intersection){
        //             hsvColor = scaleSaturation(rotateHue(hsvColor, (angle_step*color_plane)), saturation_2);
        //         }
        //     }
        // }

        if (t_intersection >= max_intersection)
        {
            hsvColor = scaleSaturation(hsvColor, saturation_1);
        }
        else if (t_intersection < max_intersection && (max_intersection-step_size) < t_intersection)
        {
            hsvColor = scaleSaturation(rotateHue(hsvColor, 90.0), saturation_2);
        }
        else if (t_intersection < (max_intersection-(step_size)) && (max_intersection-(step_size*2.0)) < t_intersection)
        {
            hsvColor = scaleSaturation(rotateHue(hsvColor, 180.0), saturation_3);
        }
        else if (t_intersection <= (max_intersection-(step_size*2.0)))
        {
            hsvColor = scaleSaturation(rotateHue(hsvColor, -90.0), saturation_4);
        }

        gl_FragColor = inside * hsvToRgb(hsvColor);
    }
}
