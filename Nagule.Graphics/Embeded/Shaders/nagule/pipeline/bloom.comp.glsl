#ifndef BLOOM
#define BLOOM

// from https://www.shadertoy.com/view/tttGzj
vec2 Bloom_RandomizeUV(in vec2 uv)
{
    uv = uv + 0.5;
    vec2 iuv = floor(uv);
    vec2 fuv = fract(uv);
    
	//uv = iuv + fuv*fuv*(3.0f-2.0f*fuv); // smoothstep
    uv = iuv + fuv * fuv * fuv * (fuv * (fuv * 6.0 - 15.0) + 10.0); // quintic
	return uv - 0.5;
}

vec3 Bloom(vec3 color)
{
    float r = Bloom_Radius;
    vec2 grad = Bloom_RandomizeUV(vec2(r));
    vec4 bloom = 
        textureGrad(Bloom_BrightnessTex, TexCoord + .5 * vec2(r, r), grad, grad) + 
        textureGrad(Bloom_BrightnessTex, TexCoord + .5 * vec2(r, -r), grad, grad) + 
        textureGrad(Bloom_BrightnessTex, TexCoord + .5 * vec2(-r, r), grad, grad) + 
        textureGrad(Bloom_BrightnessTex, TexCoord + .5 * vec2(-r, -r), grad, grad);

    bloom = 0.25 * Bloom_Intensity * bloom;
    
#ifdef _Bloom_DirtTex
    bloom = bloom + Bloom_DirtIntensity * bloom * texture(Bloom_DirtTex);
#endif

    //return color + bloom.rgb;
    return texture(Bloom_BrightnessTex, TexCoord).rgb;
}

#endif