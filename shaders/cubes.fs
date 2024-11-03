/*{
  "CREDIT": "by mojovideotech",
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
  	    "NAME": "c1",
        "TYPE": "color",
        "DEFAULT": [
            1.0,
            0.0,
            0.0,
            1.0
        ]
    },
    {
  	    "NAME": "saturation",
        "TYPE": "float",
        "DEFAULT": 1,
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
    }

  ],
  "DESCRIPTION": "wip based on glslsandbox.com/e#31945.2"
}*/

///////////////////////////////////////////
// CubeCluster (wip)  by mojovideotech
//
// Creative Commons Attribution-NonCommercial-ShareAlike 3.0
//
// based on :
// glslsandbox.com/e#31945.2
///////////////////////////////////////////

#ifdef GL_ES
precision mediump float;
#endif

#define	phi	1.618033988749895
#define PI 3.14159265359
#define INFINITY 1e3


bool intersects(vec3 ro, vec3 rd, vec3 box_center, float box_size, out float t_intersection)
{
    vec3 t1 = (box_center - vec3(box_size) - ro)/rd;
    vec3 t2 = (box_center + vec3(box_size) - ro)/rd;
    vec3 t_min = min(t1, t2);
    vec3 t_max = max(t1, t2);
    float t_near = max(t_min.x, max(t_min.y, t_min.z));
    float t_far = min(t_max.x, min(t_max.y, t_max.z));
    if (t_near > t_far || t_far < 1.0) return false;
    t_intersection = t_near;
    return true;
}

mat3 camera(vec3 e, vec3 la) {
    vec3 roll = vec3(0, 1, 0);
    vec3 f = normalize(la - e);
    vec3 r = normalize(cross(roll, f));
    vec3 u = normalize(mix(cross(e, f),cross(f, r),0.75));
    return mat3(r, u, f);
}

vec4 rgbToHsv(vec4 c) {
    float maxC = max(c.r, max(c.g, c.b));
    float minC = min(c.r, min(c.g, c.b));
    float delta = maxC - minC;
    float hue = 0.0;
    float saturation = (maxC == 0.0) ? 0.0 : delta / maxC;
    float value = maxC;

    if (delta > 0.0) {
        if (maxC == c.r) {
            hue = (c.g - c.b) / delta + (c.g < c.b ? 6.0 : 0.0);
        } else if (maxC == c.g) {
            hue = (c.b - c.r) / delta + 2.0;
        } else {
            hue = (c.r - c.g) / delta + 4.0;
        }
        hue /= 6.0;
    }

    return vec4(hue, saturation, value, 1.0);
}

vec4 hsvToRgb(vec4 c) {
    float h = c.x * 6.0;
    float s = c.y;
    float v = c.z;

    int i = int(floor(h));
    float f = h - float(i);
    float p = v * (1.0 - s);
    float q = v * (1.0 - f * s);
    float t = v * (1.0 - (1.0 - f) * s);

    if (i == 0) return vec4(v, t, p, 1.0);
    else if (i == 1) return vec4(q, v, p, 1.0);
    else if (i == 2) return vec4(p, v, t, 1.0);
    else if (i == 3) return vec4(p, q, v, 1.0);
    else if (i == 4) return vec4(t, p, v, 1.0);
    else return vec4(v, p, q, 1.0);
}

vec4 rotateHue(vec4 hsvColor, float angle) {
    vec4 c = vec4(hsvColor);
    c.x = mod(hsvColor.x + angle / 360.0, 1.0);
    return c;
}

void main(void)
{
    float a = rate*TIME, t_intersection = INFINITY, inside = 0.0;
    vec2 uv = (2.5*gl_FragCoord.xy - RENDERSIZE)/min(RENDERSIZE.x, RENDERSIZE.y);
    vec3 ro = 8.0*vec3(cos(a), sin(0.5*TIME), -sin(a));
    vec3 rd = camera(ro, vec3(0))*normalize(vec3(uv, 2.));
    for (float i = 0.; i < 5.0; i++) {
        if(i == cluster_size) break;
        for (float j = 0.; j < 5.0; j++) {
            if(j == cluster_size) break;
            for (float k = 0.; k < 5.0; k++) {
                if(k == cluster_size) break;
                vec3 p = 2.0*(vec3(i, j, k) - 0.5*vec3(cluster_size-3.0,cluster_size,cluster_size-2.0));
				        float l = length(p), s = .1 + .6*pow(.5 + .5*sin(rate*rate*TIME*phi - j*l),k+0.5), t = 0.;
                if (intersects(ro, rd, p, s, t) && t < t_intersection) {
                    t_intersection = t;
                    vec3 n = ro + rd*t_intersection - p;
                    vec3 normal = smoothstep(vec3(s - border_width), vec3(s), n) + smoothstep(vec3(s - border_width), vec3(s), -n);
                    inside = smoothstep(1.01, 1.0, normal.x + normal.y + normal.z);
                }
            }
        }
    }

    if (t_intersection == INFINITY)
        gl_FragColor = vec4(0.0, 0.0,0.0,1.0);
    else{
        vec4 hsvColor = rgbToHsv(c1);

        hsvColor.y *= saturation;

        if (t_intersection < 15.0 && t_intersection > 10.0){
            hsvColor = rotateHue(hsvColor, 90.0);
        } else if (t_intersection <= 10.0 && t_intersection > 5.0){
            hsvColor = rotateHue(hsvColor, 180.0);
        } else if (t_intersection <= 5.0){
            hsvColor = rotateHue(hsvColor, -90.0);
        }


        gl_FragColor = inside * hsvToRgb(hsvColor);
    }

}
