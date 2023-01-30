#ifndef NAGULE_FRAGMENT
#define NAGULE_FRAGMENT

// From http://www.thetenthplanet.de/archives/1180
mat3 GetTBN(vec3 N, vec3 P, vec2 UV)
{
    // get edge vectors of the pixel triangle
    vec3 dp1  = dFdx(P);
    vec3 dp2  = dFdy(P);
    vec2 duv1 = dFdx(UV);
    vec2 duv2 = dFdy(UV);

    // solve the linear system
    vec3 dp2perp = cross(dp2, N);
    vec3 dp1perp = cross(N, dp1);
    vec3 T = dp2perp * duv1.x + dp1perp * duv2.x;
    vec3 B = dp2perp * duv1.y + dp1perp * duv2.y;

    // construct a scale-invariant frame
    float invmax = inversesqrt(max(dot(T,T), dot(B,B)));
    mat3 TBN = mat3(T * invmax, B * invmax, N);

    return TBN;
}

#endif