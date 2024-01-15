#ifndef NAGULE_RANDOM
#define NAGULE_RANDOM

float Rand(vec2 co) { return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453); }
float Rand(vec2 co, float l) { return Rand(vec2(Rand(co), l));}
float Rand(vec2 co, float l, float t) { return Rand(vec2(Rand(co, l), t)); }

#endif