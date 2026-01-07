#ifndef RAIN_PARTICLES_INCLUDED
#define RAIN_PARTICLES_INCLUDED

float Hash(float x){
    return frac(sin(x * 18.34) * 51.78);
}

float Hash2(float x){
    return frac(sin(x * 25.42) * 21.24);
}

float LineSDF(float2 p, float2 s)
{
    float2 d = abs(p) - s; //Rectangle dimensions
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0)); //if >0, outside rectangle, if <0, inside rectangle (clamped to 0)
}

void CalculateRain_float(float Time, float Count, float2 UV, float2 S, float Slant, float Speed, float Blur, out float Output) 
{

    Output = 0.0;
    for(int i = 1; i <= Count; i++){

        float h1 = Hash(i);
        float h2 = Hash2(i);
        float s1 = h1 * UV.y * -Slant; //Lines move on the direction of Slant
        float posModX = h1 * 1.2; //Randomise X
        float posModY = max(h2 * Speed, posModX * Speed); //Prevents some lines from moving too slow

        float2 position = float2(posModX + s1, -fmod(-posModY * Time * 0.1, -1.)); 
        //Loops animation in X-Y axis
        float sdf = LineSDF(UV - position, S);
        Output += clamp(- sdf / Blur, 0.0, 1.0); //Negative values are inside the line, positive values are outside

    }

}

#endif
