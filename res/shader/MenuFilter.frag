precision mediump float;

uniform sampler2D texture0;
uniform vec2 resolution;
uniform float time;

// RGB shift, noise, blur, and vignette settings
const float shiftAmount = 0.0007;
const float noiseStrength = 0.05;
const float blurSize = 0.0010;
const float vignetteStrength = 0.5;
const float vignetteSoftness = 0.80;

float rand(vec2 uv)
{
    return fract(sin(dot(uv, vec2(12.9898, 78.233))) * 43758.5453);
}

void main()
{
    vec2 texCoords = gl_FragCoord.xy / resolution.xy;

    // RGB shift effect
    vec4 shift = vec4(shiftAmount, 0.0, -shiftAmount, 0.0);
    float red = texture2D(texture0, texCoords + shift.xy).r;
    float green = texture2D(texture0, texCoords + shift.yw).g;
    float blue = texture2D(texture0, texCoords + shift.zw).b;

    // Noise effect
    float noise = rand(texCoords * resolution.xy + time) * noiseStrength;

    // Blur effect
    float blurTotal = 0.0;
    vec4 blurColor = vec4(0.0);
    for (float i = -2.0; i <= 2.0; i += 1.0) {
        blurColor += texture2D(texture0, texCoords + vec2(blurSize * i, 0.0)) * 0.2;
        blurTotal += 0.2;
    }
    blurColor /= blurTotal;

    // Vignette effect
    vec2 center = vec2(0.5, 0.5);
    float vignetteDistance = distance(texCoords, center);
    float vignetteMask = smoothstep(0.0, vignetteSoftness, vignetteDistance);
    float vignette = mix(1.0, vignetteStrength, vignetteMask);

    // Combine effects
    vec4 finalColor = mix(vec4(red + noise, green + noise, blue + noise, 1.0), blurColor, 0.5);
    finalColor.rgb *= vignette;

    gl_FragColor = finalColor;
}

