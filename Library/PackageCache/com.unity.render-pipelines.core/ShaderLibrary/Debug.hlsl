#ifndef UNITY_DEBUG_INCLUDED
#define UNITY_DEBUG_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

// UX-verified colorblind-optimized debug colors, listed in order of increasing perceived "hotness"
#define DEBUG_COLORS_COUNT 12
#define kDebugColorBlack        float4(0.0   / 255.0, 0.0   / 255.0, 0.0   / 255.0, 1.0) // #000000
#define kDebugColorLightPurple  float4(166.0 / 255.0, 70.0  / 255.0, 242.0 / 255.0, 1.0) // #A646F2
#define kDebugColorDeepBlue     float4(0.0   / 255.0, 26.0  / 255.0, 221.0 / 255.0, 1.0) // #001ADD
#define kDebugColorSkyBlue      float4(65.0  / 255.0, 152.0 / 255.0, 224.0 / 255.0, 1.0) // #4198E0
#define kDebugColorLightBlue    float4(158.0 / 255.0, 228.0 / 255.0, 251.0 / 255.0, 1.0) // #1A1D21
#define kDebugColorTeal         float4(56.0  / 255.0, 243.0 / 255.0, 176.0 / 255.0, 1.0) // #38F3B0
#define kDebugColorBrightGreen  float4(168.0 / 255.0, 238.0 / 255.0, 46.0  / 255.0, 1.0) // #A8EE2E
#define kDebugColorBrightYellow float4(255.0 / 255.0, 253.0 / 255.0, 76.0  / 255.0, 1.0) // #FFFD4C
#define kDebugColorDarkYellow   float4(255.0 / 255.0, 214.0 / 255.0, 0.0   / 255.0, 1.0) // #FFD600
#define kDebugColorOrange       float4(253.0 / 255.0, 152.0 / 255.0, 0.0   / 255.0, 1.0) // #FD9800
#define kDebugColorBrightRed    float4(255.0 / 255.0, 67.0  / 255.0, 51.0  / 255.0, 1.0) // #FF4333
#define kDebugColorDarkRed      float4(132.0 / 255.0, 10.0  / 255.0, 54.0  / 255.0, 1.0) // #840A36

// Shadow cascade debug colors. Keep in sync with the ones in ShadowCascadeGUI.cs.
// Note: These colors are not 1:1 match to editor UI, in order to provide better contrast in the viewport.
#define kDebugColorShadowCascade0   float4(0.4, 0.4, 0.9, 1.0)
#define kDebugColorShadowCascade1   float4(0.4, 0.9, 0.4, 1.0)
#define kDebugColorShadowCascade2   float4(0.9, 0.9, 0.4, 1.0)
#define kDebugColorShadowCascade3   float4(0.9, 0.4, 0.4, 1.0)

// UX-verified colorblind-optimized "heat color gradient"
static const float4 kDebugColorGradient[DEBUG_COLORS_COUNT] = { kDebugColorBlack, kDebugColorLightPurple, kDebugColorDeepBlue,
    kDebugColorSkyBlue, kDebugColorLightBlue, kDebugColorTeal, kDebugColorBrightGreen, kDebugColorBrightYellow,
    kDebugColorDarkYellow, kDebugColorOrange, kDebugColorBrightRed, kDebugColorDarkRed };

#define TRANSPARENCY_OVERDRAW_COST 1.0
#define TRANSPARENCY_OVERDRAW_A 1.0

// Given an enum (represented by an int here), return a color.
// Use for DebugView of enum
real3 GetIndexColor(int index)
{
    real3 outColor = real3(1.0, 0.0, 0.0);

    if (index == 0)
        outColor = real3(1.0, 0.5, 0.5);
    else if (index == 1)
        outColor = real3(0.5, 1.0, 0.5);
    else if (index == 2)
        outColor = real3(0.5, 0.5, 1.0);
    else if (index == 3)
        outColor = real3(1.0, 1.0, 0.5);
    else if (index == 4)
        outColor = real3(1.0, 0.5, 1.0);
    else if (index == 5)
        outColor = real3(0.5, 1.0, 1.0);
    else if (index == 6)
        outColor = real3(0.25, 0.75, 1.0);
    else if (index == 7)
        outColor = real3(1.0, 0.75, 0.25);
    else if (index == 8)
        outColor = real3(0.75, 1.0, 0.25);
    else if (index == 9)
        outColor = real3(0.75, 0.25, 1.0);
    else if (index == 10)
        outColor = real3(0.25, 1.0, 0.75);
    else if (index == 11)
        outColor = real3(0.75, 0.75, 0.25);
    else if (index == 12)
        outColor = real3(0.75, 0.25, 0.75);
    else if (index == 13)
        outColor = real3(0.25, 0.75, 0.75);
    else if (index == 14)
        outColor = real3(0.25, 0.25, 0.75);
    else if (index == 15)
        outColor = real3(0.75, 0.25, 0.25);

    return outColor;
}

#define PACK_BITS25(_x0,_x1,_x2,_x3,_x4,_x5,_x6,_x7,_x8,_x9,_x10,_x11,_x12,_x13,_x14,_x15,_x16,_x17,_x18,_x19,_x20,_x21,_x22,_x23,_x24) (_x0|(_x1<<1)|(_x2<<2)|(_x3<<3)|(_x4<<4)|(_x5<<5)|(_x6<<6)|(_x7<<7)|(_x8<<8)|(_x9<<9)|(_x10<<10)|(_x11<<11)|(_x12<<12)|(_x13<<13)|(_x14<<14)|(_x15<<15)|(_x16<<16)|(_x17<<17)|(_x18<<18)|(_x19<<19)|(_x20<<20)|(_x21<<21)|(_x22<<22)|(_x23<<23)|(_x24<<24))
#define _ 0
#define x 1
const static uint kFontData[9][2] = {
    { PACK_BITS25(_,_,x,_,_,        _,_,x,_,_,      _,x,x,x,_,      x,x,x,x,x,      _,_,_,x,_), PACK_BITS25(x,x,x,x,x,      _,x,x,x,_,      x,x,x,x,x,      _,x,x,x,_,      _,x,x,x,_) },
    { PACK_BITS25(_,x,_,x,_,        _,x,x,_,_,      x,_,_,_,x,      _,_,_,_,x,      _,_,_,x,_), PACK_BITS25(x,_,_,_,_,      x,_,_,_,x,      _,_,_,_,x,      x,_,_,_,x,      x,_,_,_,x) },
    { PACK_BITS25(x,_,_,_,x,        x,_,x,_,_,      x,_,_,_,x,      _,_,_,x,_,      _,_,x,x,_), PACK_BITS25(x,_,_,_,_,      x,_,_,_,_,      _,_,_,x,_,      x,_,_,_,x,      x,_,_,_,x) },
    { PACK_BITS25(x,_,_,_,x,        _,_,x,_,_,      _,_,_,_,x,      _,_,x,_,_,      _,x,_,x,_), PACK_BITS25(x,_,x,x,_,      x,_,_,_,_,      _,_,_,x,_,      x,_,_,_,x,      x,_,_,_,x) },
    { PACK_BITS25(x,_,_,_,x,        _,_,x,_,_,      _,_,_,x,_,      _,x,x,x,_,      _,x,_,x,_), PACK_BITS25(x,x,_,_,x,      x,x,x,x,_,      _,_,x,_,_,      _,x,x,x,_,      _,x,x,x,x) },
    { PACK_BITS25(x,_,_,_,x,        _,_,x,_,_,      _,_,x,_,_,      _,_,_,_,x,      x,_,_,x,_), PACK_BITS25(_,_,_,_,x,      x,_,_,_,x,      _,_,x,_,_,      x,_,_,_,x,      _,_,_,_,x) },
    { PACK_BITS25(x,_,_,_,x,        _,_,x,_,_,      _,x,_,_,_,      _,_,_,_,x,      x,x,x,x,x), PACK_BITS25(_,_,_,_,x,      x,_,_,_,x,      _,x,_,_,_,      x,_,_,_,x,      _,_,_,_,x) },
    { PACK_BITS25(_,x,_,x,_,        _,_,x,_,_,      x,_,_,_,_,      x,_,_,_,x,      _,_,_,x,_), PACK_BITS25(x,_,_,_,x,      x,_,_,_,x,      _,x,_,_,_,      x,_,_,_,x,      x,_,_,_,x) },
    { PACK_BITS25(_,_,x,_,_,        x,x,x,x,x,      x,x,x,x,x,      _,x,x,x,_,      _,_,_,x,_), PACK_BITS25(_,x,x,x,_,      _,x,x,x,_,      _,x,_,_,_,      _,x,x,x,_,      _,x,x,x,_) }
};
#undef _
#undef x
#undef PACK_BITS25

bool SampleDebugFont(int2 pixCoord, uint digit)
{
    if (pixCoord.x < 0 || pixCoord.y < 0 || pixCoord.x >= 5 || pixCoord.y >= 9 || digit > 9)
        return false;

    return (kFontData[8 - pixCoord.y][digit >= 5] >> ((digit % 5) * 5 + pixCoord.x)) & 1;
}

/*
 * Sample up to 2 digits of a number. (Excluding leading zeroes)
 *
 * Note: Digit have a size of 5x8 pixels and spaced by 1 pixel
 * See SampleDebugFontNumberAllDigits to sample all digits.
 *
 * @param pixCoord: pixel coordinate of the number sample
 * @param number: number to sample
 * @return true when the pixel is a pixel of a digit.
 */
bool SampleDebugFontNumber2Digits(int2 pixCoord, uint number)
{
    pixCoord.y -= 4;
    if (number <= 9)
    {
        return SampleDebugFont(pixCoord - int2(6, 0), number);
    }
    else
    {
        return (SampleDebugFont(pixCoord, number / 10) | SampleDebugFont(pixCoord - int2(6, 0), number % 10));
    }
}

/*
 * Sample up to 3 digits of a number. (Excluding leading zeroes)
 *
 * Note: Digit have a size of 5x8 pixels and spaced by 1 pixel
 * See SampleDebugFontNumberAllDigits to sample all digits.
 *
 * @param pixCoord: pixel coordinate of the number sample
 * @param number: number to sample
 * @return true when the pixel is a pixel of a digit.
 */
bool SampleDebugFontNumber3Digits(int2 pixCoord, uint number)
{
    pixCoord.y -= 4;
    if (number <= 9)
    {
        return SampleDebugFont(pixCoord - int2(6, 0), number);
    }
    else if (number <= 99)
    {
        return (SampleDebugFont(pixCoord, (number / 10) % 10) | SampleDebugFont(pixCoord - int2(6, 0), number % 10));
    }
    else
    {
        return (SampleDebugFont(pixCoord, (number / 100)) | SampleDebugFont(pixCoord - int2(4, 0),(number / 10) % 10) | SampleDebugFont(pixCoord - int2(8, 0),(number / 10) % 10) );
    }
}

/*
 * Sample all digits of a number. (Excluding leading zeroes)
 *
 * Note: Digit have a size of 5x8 pixels and spaced by 1 pixel
 * See SampleDebugFontNumber2Digits for a faster version supporting only 2 digits.
 *
 * @param pixCoord: pixel coordinate of the number sample
 * @param number: number to sample
 * @return true when the pixel is a pixel of a digit.
 */
bool SampleDebugFontNumberAllDigits(int2 pixCoord, uint number)
{
    const int digitCount = (int)max(1u, uint(log10(number)) + 1u);

    pixCoord.y -= 4;
    int2 offset = int2(6 * digitCount, 0);
    uint current = number;
    for (int i = 0; i < digitCount; ++i)
    {
        if (SampleDebugFont(pixCoord - offset, current % 10))
            return true;

        current /= 10;
        offset -= int2(6, 0);
    }
    return false;
}

TEXTURE2D(_DebugFont); // Debug font to write string in shader

// DebugFont code assume black and white font with texture size 256x128 with bloc of 16x16
#define DEBUG_FONT_TEXT_WIDTH   16
#define DEBUG_FONT_TEXT_HEIGHT  16
#define DEBUG_FONT_TEXT_COUNT_X 16
#define DEBUG_FONT_TEXT_COUNT_Y 8
#define DEBUG_FONT_TEXT_ASCII_START 32

#define DEBUG_FONT_TEXT_SCALE_WIDTH 10 // This control the spacing between characters (if a character fill the text block it will overlap).

/*
 * Draw a character
 *
 * Note: Only supports ASCII symbols from DEBUG_FONT_TEXT_ASCII_START to 126
 *
 * @param asciiValue: actual character we want to draw
 * @param fontColor: color of the font to use
 * @param currentUnormCoord: current unnormalized screen position
 * @param fixedUnormCoord: position where we want to draw a character (will be incremented by the provided `fontTextScaleWidth` in provided `direction`)
 * @param color: current screen color
 * @param direction: direction to draw a string (1 = left to right, -1 = right to left), so it determines the direction in which `fixedUnormCoord` will shift
 * @param fontTextScaleWidth: spacing between characters, so the amount by which `fixedUnormCoord` will shift
 * @return void, blends in `fontColor` into the `color` parameter if we hit font character
 */
void DrawCharacter(uint asciiValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, inout float3 color, int direction, int fontTextScaleWidth)
{
    // Are we inside a font display block on the screen ?
    uint2 localCharCoord = currentUnormCoord - fixedUnormCoord;
    if (localCharCoord.x >= 0 && localCharCoord.x < DEBUG_FONT_TEXT_WIDTH && localCharCoord.y >= 0 && localCharCoord.y < DEBUG_FONT_TEXT_HEIGHT)
    {
        localCharCoord.y = DEBUG_FONT_TEXT_HEIGHT - localCharCoord.y;

        asciiValue -= DEBUG_FONT_TEXT_ASCII_START; // Our font start at ASCII table 32;
        uint2 asciiCoord = uint2(asciiValue % DEBUG_FONT_TEXT_COUNT_X, asciiValue / DEBUG_FONT_TEXT_COUNT_X);
        // Unorm coordinate inside the font texture
        uint2 unormTexCoord = asciiCoord * uint2(DEBUG_FONT_TEXT_WIDTH, DEBUG_FONT_TEXT_HEIGHT) + localCharCoord;
        // normalized coordinate
        float2 normTexCoord = float2(unormTexCoord) / float2(DEBUG_FONT_TEXT_WIDTH * DEBUG_FONT_TEXT_COUNT_X, DEBUG_FONT_TEXT_HEIGHT * DEBUG_FONT_TEXT_COUNT_Y);
        normTexCoord.y = 1.0 - normTexCoord.y;

        float charColor = SAMPLE_TEXTURE2D_LOD(_DebugFont, sampler_PointClamp, normTexCoord, 0).r;
        color = color * (1.0 - charColor) + charColor * fontColor;
    }

    fixedUnormCoord.x += fontTextScaleWidth * direction;
}

void DrawCharacter(uint asciiValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, inout float3 color, int direction)
{
    DrawCharacter(asciiValue, fontColor, currentUnormCoord, fixedUnormCoord, color, direction, DEBUG_FONT_TEXT_SCALE_WIDTH);
}

// Shortcut to not have to file direction
void DrawCharacter(uint asciiValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, inout float3 color)
{
    DrawCharacter(asciiValue, fontColor, currentUnormCoord, fixedUnormCoord, color, 1);
}

// Draws a heatmap with numbered tiles, with increasingly "hot" background colors depending on n,
// where values at or above maxN receive strong red background color.
float4 OverlayHeatMap(uint2 pixCoord, uint2 tileSize, uint n, uint maxN, float opacity)
{
    int colorIndex = 1 + (int)floor(10 * (log2((float)n + 0.1f) / log2(float(maxN))));
    colorIndex = clamp(colorIndex, 0, DEBUG_COLORS_COUNT-1);
    float4 col = kDebugColorGradient[colorIndex];

    int2 coord = (pixCoord & (tileSize - 1)) - int2(tileSize.x/4+1, tileSize.y/3-3);

    float4 color = float4(PositivePow(col.rgb, 2.2), opacity * col.a);
    if (n >= 0)
    {
        if (SampleDebugFontNumber3Digits(coord, n))        // Shadow
            color = float4(0, 0, 0, 1);
        if (SampleDebugFontNumber3Digits(coord + 1, n))    // Text
            color = float4(1, 1, 1, 1);
    }
    return color;
}

// Draws a heatmap with numbered tiles, with increasingly "hot" background colors depending on n,
// where values at or above maxN receive strong red background color.
float4 OverlayHeatMapNoNumber(uint2 pixCoord, uint2 tileSize, uint n, uint maxN, float opacity)
{
    int colorIndex = 1 + (int)floor(10 * (log2((float)n + 0.1f) / log2(float(maxN))));
    colorIndex = clamp(colorIndex, 0, DEBUG_COLORS_COUNT-1);
    float4 col = kDebugColorGradient[colorIndex];

    int2 coord = (pixCoord & (tileSize - 1)) - int2(tileSize.x/4+1, tileSize.y/3-3);

    return float4(PositivePow(col.rgb, 2.2), opacity * col.a);
}

// Convert an arbitrary range to color base on threshold provide to the function, threshold must be in growing order
real3 GetColorCodeFunction(real value, real4 threshold)
{
    const real3 red = { 1.0, 0.0, 0.0 };
    const real3 lightGreen = { 0.5, 1.0, 0.5 };
    const real3 darkGreen = { 0.1, 1.0, 0.1 };
    const real3 yellow = { 1.0, 1.0, 0.0 };

    real3 outColor = red;
    if (value < threshold[0])
    {
        outColor = red;
    }
    else if (value >= threshold[0] && value < threshold[1])
    {
        real scale = (value - threshold[0]) / (threshold[1] - threshold[0]);
        outColor = lerp(red, darkGreen, scale);
    }
    else if (value >= threshold[1] && value < threshold[2])
    {
        real scale = (value - threshold[1]) / (threshold[2] - threshold[1]);
        outColor = lerp(darkGreen, lightGreen, scale);
    }
    else if (value >= threshold[2] && value < threshold[3])
    {
        real scale = (value - threshold[2]) / (threshold[2] - threshold[2]);
        outColor = lerp(lightGreen, yellow, scale);
    }
    else
    {
        outColor = yellow;
    }

    return outColor;
}

/// Return the color of the overdraw debug.
///
/// The color will go from
/// (cheap) dark blue -> red -> violet -> white (expensive)
///
/// * overdrawCount: the number of overdraw
/// * maxOverdrawCount: the maximum number of overdraw.
///   if the overdrawCount is above, the most expensive color is returned.
real3 GetOverdrawColor(real overdrawCount, real maxOverdrawCount)
{
    if (overdrawCount < 0.01)
        return real3(0, 0, 0);

    // cheapest hue
    const float initialHue = 240;
    // most expensive hue is initialHue - deltaHue
    const float deltaHue = 20;
    // the value in % of budget where we start to remove saturation
    const float xLight = 0.95;
    // minimum hue
    const float minHue = deltaHue - 360 + initialHue;
    // budget value of a single draw
    const float xCostOne = 1.0 / maxOverdrawCount;
    // current budget value
    const float x = saturate(overdrawCount / maxOverdrawCount);


    float hue = fmod(max(min((x - xCostOne) * (deltaHue - 360) * (1.0 / (xLight - xCostOne)) + initialHue, initialHue), minHue), 360)/360.0;
    float saturation = min(max((-1.0/(1 - xLight)) * (x - xLight), 0), 1);
    return HsvToRgb(real3(hue, saturation, 1.0));
}

uint OverdrawLegendBucketInterval(uint maxOverdrawCount)
{
    if (maxOverdrawCount <= 10)
        return 1;
    if (maxOverdrawCount <= 50)
        return 5;
    if (maxOverdrawCount <= 100)
        return 10;

    const uint digitCount = floor(log10(maxOverdrawCount));
    const uint digitMultiplier = pow(10, digitCount);
    const uint biggestDigit = floor(maxOverdrawCount/digitMultiplier);
    if (biggestDigit < 5)
        return pow(10, digitCount - 1) * 5;

    return digitMultiplier;
}

/// Return the color of the overdraw debug legend.
///
/// It will draw a bar with all the color buckets of the overdraw debug
///
/// * texcoord: the texture coordinate of the pixel to draw
/// * maxOverdrawCount: the maximum number of overdraw.
/// * screenSize: screen size (w, h, 1/w, 1/h).
/// * defaultColor: the default color used for other areas
void DrawOverdrawLegend(real2 texCoord, real maxOverdrawCount, real4 screenSize, inout real3 color)
{
    // Band parameters
    // Position of the band (fixed x, fixed y, rel x, rel y)
    const real4 bandPosition = real4(20, 20, 0, 0);
    // Position of the band labels (fixed x, fixed y, rel x, rel y)
    const real4 bandLabelPosition = real4(20, 50, 0, 0);
    // Size of the band (fixed x, fixed y, rel x, rel y)
    const real4 bandSize = real4(-bandPosition.x * 2, 20, 1, 0);
    // Thickness of the band (fixed x, fixed y, rel x, rel y)
    const real4 bandBorderThickness = real4(4, 4, 0, 0);

    // Compute UVs
    const real2 bandPositionUV = bandPosition.xy * screenSize.zw + bandPosition.zw;
    const real2 bandLabelPositionUV = bandLabelPosition.xy * screenSize.zw + bandLabelPosition.zw;
    const real2 bandSizeUV = bandSize.xy * screenSize.zw + bandSize.zw;
    const real4 bandBorderPosition = bandPosition - bandBorderThickness;
    const real4 bandBorderSize = bandSize + 2 * bandBorderThickness;
    const real2 bandBorderPositionUV = bandBorderPosition.xy * screenSize.zw + bandBorderPosition.zw;
    const real2 bandBorderSizeUV = bandBorderSize.xy * screenSize.zw + bandBorderSize.zw;

    // Transform coordinate
    const real2 bandBorderCoord =  (texCoord - bandBorderPositionUV) / bandBorderSizeUV;
    const real2 bandCoord =  (texCoord - bandPositionUV) / bandSizeUV;

    // Compute bucket index
    const real bucket = ceil(bandCoord.x * maxOverdrawCount);

    // Assign color when relevant
    // Band border
    if (all(bandBorderCoord >= 0) && all(bandBorderCoord <= 1))
        color = real3(0.1, 0.1, 0.1);

    // Band color
    if (all(bandCoord >= 0) && all(bandCoord <= 1))
        color = GetOverdrawColor(bucket, maxOverdrawCount);

    // Bucket label
    if (0 < bucket && bucket <= maxOverdrawCount)
    {
        const uint bucketInterval = OverdrawLegendBucketInterval(maxOverdrawCount);
        const uint bucketLabelIndex = (uint(bucket) / bucketInterval) * bucketInterval;
        const real2 labelStartCoord = real2(
            bandLabelPositionUV.x + (bucketLabelIndex - 1) * (bandSizeUV.x / maxOverdrawCount),
            bandLabelPositionUV.y
        );

        const uint2 pixCoord = uint2((texCoord - labelStartCoord) * screenSize.xy);
        if (SampleDebugFontNumberAllDigits(pixCoord, bucketLabelIndex))
            color = real3(1, 1, 1);
    }
}

// Returns the barycentric coordinates of a point p in a triangle defined by the vertices a, b, and c
float3 GetBarycentricCoord(float2 p, float2 a, float2 b, float2 c)
{
    float2 v0 = b - a;
    float2 v1 = c - a;
    float2 v2 = p - a;
    float d00 = dot(v0, v0);
    float d01 = dot(v0, v1);
    float d11 = dot(v1, v1);
    float d20 = dot(v2, v0);
    float d21 = dot(v2, v1);
    float denom = d00 * d11 - d01 * d01;
    float3 bary = 0;
    bary.y = (d11 * d20 - d01 * d21) / denom;
    bary.z = (d00 * d21 - d01 * d20) / denom;
    bary.x = 1.0f - bary.y - bary.z;
    return bary;
}

// Returns whether a point p is part of a triangle defined by the vertices a, b, and c
bool IsPointInTriangle(float2 p, float2 a, float2 b, float2 c)
{
    float3 bar = GetBarycentricCoord(p, a, b, c);
    return (bar.x >= 0 && bar.x <= 1 && bar.y >= 0 && bar.y <= 1 && (bar.x + bar.y) <= 1);
}

/// Return the color of the segment.
///
/// It will draw a line between the given points with the given appearance (thickness and color).
///
/// * texcoord: the texture coordinate of the pixel to draw
/// * p1: coordinates of the line start
/// * p2: coordinates of the line end
/// * thickness: how thick the line should be
/// * color: color of the line
float4 DrawSegment(float2 texcoord, float2 p1, float2 p2, float thickness, float3 color)
{
    float a = abs(distance(p1, texcoord));
    float b = abs(distance(p2, texcoord));
    float c = abs(distance(p1, p2));

    if (a >= c || b >= c) return 0;

    float p = (a + b + c) * 0.5;
    float h = 2 / c * sqrt(p * (p - a) * (p - b) * (p - c));

    float lineAlpha = lerp(1.0, 0.0, smoothstep(0.5 * thickness, 1.5 * thickness, h));
    return float4(color * lineAlpha, lineAlpha);
}

#endif // UNITY_DEBUG_INCLUDED
