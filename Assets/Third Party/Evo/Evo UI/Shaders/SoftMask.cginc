#ifndef EVO_UI_SOFTMASK_CGINC
#define EVO_UI_SOFTMASK_CGINC

#define EVO_UI_SOFTMASK_MAX 4

sampler2D _SoftMaskTex0;
sampler2D _SoftMaskTex1;
sampler2D _SoftMaskTex2;
sampler2D _SoftMaskTex3;

float _SoftMask_Count;
float4 _SoftMask_CanvasToLocalX[EVO_UI_SOFTMASK_MAX];
float4 _SoftMask_CanvasToLocalY[EVO_UI_SOFTMASK_MAX];
float4 _SoftMask_Rect[EVO_UI_SOFTMASK_MAX];
float4 _SoftMask_Data[EVO_UI_SOFTMASK_MAX];
float4 _SoftMask_PRRect[EVO_UI_SOFTMASK_MAX];
float4 _SoftMask_PRRadii[EVO_UI_SOFTMASK_MAX];
float4 _SoftMask_PRFillData[EVO_UI_SOFTMASK_MAX];
float4 _SoftMask_BorderData[EVO_UI_SOFTMASK_MAX];
float4 _SoftMask_UVOuter[EVO_UI_SOFTMASK_MAX];
float4 _SoftMask_UVInner[EVO_UI_SOFTMASK_MAX];

#define EVO_UI_PI     3.14159265
#define EVO_UI_TWO_PI 6.28318530

float Evo_UI_SdRoundedRect(float2 p, float2 halfSize, float4 radii, float isSquircle)
{
    radii.xy = (p.x > 0.0) ? radii.xy : radii.zw;
    radii.x = (p.y > 0.0) ? radii.x : radii.y;

    float2 q = abs(p) - halfSize + radii.x;
    float2 outside = max(q, 0.0);
    float normalLength = length(outside);
    float2 squared = outside * outside;
    float squircleLength = sqrt(sqrt(squared.x * squared.x + squared.y * squared.y));
    float cornerLength = lerp(normalLength, squircleLength, isSquircle);

    return min(max(q.x, q.y), 0.0) + cornerLength - radii.x;
}

float Evo_UI_GetClipDistance(float2 p, float2 halfSize, float fillAmount, float fillPacked)
{
    float packed = round(fillPacked);
    float method = fmod(packed, 8.0);
    float origin = fmod(floor(packed * 0.125), 8.0);
    float cw = fmod(floor(packed * 0.015625), 2.0);

    if (method < 1.5)
    {
        float fromMin = (origin < 0.5) ? cw : 1.0 - cw;
        float boundary = (fromMin > 0.5) ? lerp(-halfSize.x, halfSize.x, fillAmount) : lerp(halfSize.x, -halfSize.x, fillAmount);
        return (fromMin > 0.5) ? p.x - boundary : boundary - p.x;
    }

    if (method < 2.5)
    {
        float fromMin = (origin < 0.5) ? cw : 1.0 - cw;
        float boundary = (fromMin > 0.5) ? lerp(-halfSize.y, halfSize.y, fillAmount) : lerp(halfSize.y, -halfSize.y, fillAmount);
        return (fromMin > 0.5) ? p.y - boundary : boundary - p.y;
    }

    float startAngle = (origin < 0.5) ? -EVO_UI_PI * 0.5 :
                       (origin < 1.5) ? 0.0 :
                       (origin < 2.5) ? EVO_UI_PI * 0.5 : EVO_UI_PI;

    float angle = atan2(p.y, p.x) - startAngle;
    angle = (cw > 0.5) ? -angle : angle;
    float radial = frac(angle / EVO_UI_TWO_PI + 1.0);
    float signedFraction = (radial <= fillAmount) ? -min(radial, fillAmount - radial) : min(radial - fillAmount, 1.0 - radial);
    float angleDistance = min(abs(signedFraction) * EVO_UI_TWO_PI, EVO_UI_PI * 0.5);
    return ((signedFraction < 0.0) ? -1.0 : 1.0) * sin(angleDistance) * length(p);
}

float Evo_UI_ComputeFillMask(float2 p, float2 halfSize, float fillAmount, float fillPacked)
{
    float method = fmod(round(fillPacked), 8.0);
    if (method < 0.5 || fillAmount >= 1.0)
        return 1.0;
    if (fillAmount <= 0.0)
        return 0.0;

    float clipDist = Evo_UI_GetClipDistance(p, halfSize, fillAmount, fillPacked);
    float clipAA = max(fwidth(clipDist) * 0.5, 0.00001);
    return 1.0 - smoothstep(-clipAA, clipAA, clipDist);
}

float Evo_UI_Map1D(float x, float left, float right, float extent, float uvMin, float uvLeft, float uvRight, float uvMax)
{
    if (x < left)
    {
        return lerp(uvMin, uvLeft, (left > 0.001) ? (x / left) : 0.0);
    }
    else if (x > right)
    {
        return lerp(uvRight, uvMax, (extent > right + 0.001) ? ((x - right) / (extent - right)) : 0.0);
    }

    return lerp(uvLeft, uvRight, (right > left + 0.001) ? ((x - left) / (right - left)) : 0.0);
}

float Evo_UI_MapTiled1D(float x, float left, float right, float extent, float tileExtent, float uvMin, float uvLeft, float uvRight, float uvMax)
{
    if (x < left)
    {
        return lerp(uvMin, uvLeft, (left > 0.001) ? (x / left) : 0.0);
    }
    else if (x >= right)
    {
        return lerp(uvRight, uvMax, (extent > right + 0.001) ? ((x - right) / (extent - right)) : 0.0);
    }

    float repeat = frac((x - left) / max(tileExtent, 0.001));
    return lerp(uvLeft, uvRight, repeat);
}

float Evo_UI_ComputeImageFillMask(float2 p, float2 size, float fillAmount, float fillPacked)
{
    if (fillAmount >= 1.0)
        return 1.0;
    if (fillAmount <= 0.0)
        return 0.0;

    float packed = round(fillPacked);
    float method = fmod(packed, 8.0);
    float origin = fmod(floor(packed * 0.125), 8.0);
    float cw = fmod(floor(packed * 0.015625), 2.0);
    float clipDist;

    if (method < 0.5)
    {
        float boundary = (origin < 0.5) ? size.x * fillAmount : size.x * (1.0 - fillAmount);
        clipDist = (origin < 0.5) ? p.x - boundary : boundary - p.x;
        float clipAA = max(fwidth(clipDist) * 0.5, 0.00001);
        return 1.0 - smoothstep(-clipAA, clipAA, clipDist);
    }

    if (method < 1.5)
    {
        float boundary = (origin < 0.5) ? size.y * fillAmount : size.y * (1.0 - fillAmount);
        clipDist = (origin < 0.5) ? p.y - boundary : boundary - p.y;
        float clipAA = max(fwidth(clipDist) * 0.5, 0.00001);
        return 1.0 - smoothstep(-clipAA, clipAA, clipDist);
    }

    float2 normalized = p / size;
    float2 pivot;
    float startAngle;
    float progressScale;

    if (method < 2.5)
    {
        pivot = (origin < 0.5) ? float2(0.0, 0.0) :
                (origin < 1.5) ? float2(0.0, 1.0) :
                (origin < 2.5) ? float2(1.0, 1.0) : float2(1.0, 0.0);
        startAngle = ((cw > 0.5) ? EVO_UI_PI * 0.5 : 0.0) - origin * EVO_UI_PI * 0.5;
        progressScale = 4.0;
    }
    else if (method < 3.5)
    {
        pivot = (origin < 0.5) ? float2(0.5, 0.0) :
                (origin < 1.5) ? float2(0.0, 0.5) :
                (origin < 2.5) ? float2(0.5, 1.0) : float2(1.0, 0.5);
        startAngle = ((cw > 0.5) ? EVO_UI_PI : 0.0) - origin * EVO_UI_PI * 0.5;
        progressScale = 2.0;
    }
    else
    {
        pivot = float2(0.5, 0.5);
        startAngle = -EVO_UI_PI * 0.5 + origin * EVO_UI_PI * 0.5;
        progressScale = 1.0;
    }

    float2 radial = normalized - pivot;
    if (method > 2.5 && method < 3.5)
    {
        if (origin < 0.5 || (origin > 1.5 && origin < 2.5))
            radial.x *= 2.0;
        else
            radial.y *= 2.0;
    }

    if (dot(radial, radial) < 0.0000001)
        return 1.0;

    float angle = atan2(radial.y, radial.x);
    float progress = (cw > 0.5) ? startAngle - angle : angle - startAngle;
    progress = frac(progress / EVO_UI_TWO_PI + 1.0) * progressScale;

    float progressAA = max(fwidth(progress) * 0.5, 0.00001);
    return 1.0 - smoothstep(fillAmount - progressAA, fillAmount + progressAA, progress);
}

inline float Evo_UI_EvaluateSoftMask(float4 canvasPos, float4 canvasToLocalX, float4 canvasToLocalY, float4 rect, float4 data, float4 proceduralRect, float4 proceduralRadii, float4 proceduralFillData, float4 borderData, float4 uvOuter, float4 uvInner, sampler2D maskTexture)
{
    if (rect.z <= 0.0 || rect.w <= 0.0)
        return 0.0;

    float2 localPos = float2(dot(canvasToLocalX, canvasPos), dot(canvasToLocalY, canvasPos));
    float maskAlpha = 1.0;
    float inBounds = 1.0;

    if (data.x > 3.5)
    {
        float2 p = localPos - rect.xy;
        float2 size = rect.zw;
        float2 maskUV;

        maskUV.x = Evo_UI_MapTiled1D(p.x, borderData.x, borderData.z, size.x, data.z, uvOuter.x, uvInner.x, uvInner.z, uvOuter.z);
        maskUV.y = Evo_UI_MapTiled1D(p.y, borderData.y, borderData.w, size.y, data.w, uvOuter.y, uvInner.y, uvInner.w, uvOuter.w);
        maskAlpha = tex2D(maskTexture, maskUV).a;

        float2 center = step(borderData.xy, p) * step(p, borderData.zw);
        maskAlpha *= lerp(1.0 - center.x * center.y, 1.0, data.y);

        float2 bounds = step(0.0, p) * step(p, size);
        inBounds = bounds.x * bounds.y;
    }
    else if (data.x > 2.5)
    {
        float2 p = localPos - rect.xy;
        float2 size = rect.zw;
        float2 maskUV = lerp(uvOuter.xy, uvOuter.zw, p / size);
        maskAlpha = tex2D(maskTexture, maskUV).a;
        maskAlpha *= Evo_UI_ComputeImageFillMask(p, size, proceduralFillData.x, proceduralFillData.y);

        float2 bounds = step(0.0, p) * step(p, size);
        inBounds = bounds.x * bounds.y;
    }
    else if (data.x > 1.5)
    {
        float2 sdfCoord = localPos - proceduralRect.xy;
        float2 halfSize = proceduralRect.zw;
        float4 radii = min(proceduralRadii, min(halfSize.x, halfSize.y));
        float isSquircle = round(proceduralFillData.w);

        float maskDist = Evo_UI_SdRoundedRect(sdfCoord, halfSize, radii, isSquircle);
        float shapeAA = max(fwidth(maskDist) * 0.5 + data.y, 0.00001);
        float shapeMask = 1.0 - smoothstep(-shapeAA, shapeAA, maskDist);
        float clipMask = Evo_UI_ComputeFillMask(sdfCoord, halfSize, proceduralFillData.x, proceduralFillData.y);
        maskAlpha = shapeMask * clipMask;
    }
    else if (data.x > 0.5)
    {
        float2 p = localPos - rect.xy;
        float2 size = rect.zw;
        float2 maskUV;

        maskUV.x = Evo_UI_Map1D(p.x, borderData.x, borderData.z, size.x, uvOuter.x, uvInner.x, uvInner.z, uvOuter.z);
        maskUV.y = Evo_UI_Map1D(p.y, borderData.y, borderData.w, size.y, uvOuter.y, uvInner.y, uvInner.w, uvOuter.w);
        maskAlpha = tex2D(maskTexture, maskUV).a;

        float2 center = step(borderData.xy, p) * step(p, borderData.zw);
        maskAlpha *= lerp(1.0 - center.x * center.y, 1.0, data.y);

        float2 bounds = step(0.0, p) * step(p, size);
        inBounds = bounds.x * bounds.y;
    }
    else
    {
        float2 p = localPos - rect.xy;
        float2 size = rect.zw;
        float2 maskUV = lerp(uvOuter.xy, uvOuter.zw, p / size);
        maskAlpha = tex2D(maskTexture, maskUV).a;

        float2 bounds = step(0.0, p) * step(p, size);
        inBounds = bounds.x * bounds.y;
    }

    return saturate(maskAlpha * inBounds);
}

inline float SoftMask_GetAlpha(float4 canvasPos)
{
    float alpha = 1.0;

    if (_SoftMask_Count > 0.5)
        alpha *= Evo_UI_EvaluateSoftMask(canvasPos, _SoftMask_CanvasToLocalX[0], _SoftMask_CanvasToLocalY[0], _SoftMask_Rect[0], _SoftMask_Data[0], _SoftMask_PRRect[0], _SoftMask_PRRadii[0], _SoftMask_PRFillData[0], _SoftMask_BorderData[0], _SoftMask_UVOuter[0], _SoftMask_UVInner[0], _SoftMaskTex0);
    if (_SoftMask_Count > 1.5)
        alpha *= Evo_UI_EvaluateSoftMask(canvasPos, _SoftMask_CanvasToLocalX[1], _SoftMask_CanvasToLocalY[1], _SoftMask_Rect[1], _SoftMask_Data[1], _SoftMask_PRRect[1], _SoftMask_PRRadii[1], _SoftMask_PRFillData[1], _SoftMask_BorderData[1], _SoftMask_UVOuter[1], _SoftMask_UVInner[1], _SoftMaskTex1);
    if (_SoftMask_Count > 2.5)
        alpha *= Evo_UI_EvaluateSoftMask(canvasPos, _SoftMask_CanvasToLocalX[2], _SoftMask_CanvasToLocalY[2], _SoftMask_Rect[2], _SoftMask_Data[2], _SoftMask_PRRect[2], _SoftMask_PRRadii[2], _SoftMask_PRFillData[2], _SoftMask_BorderData[2], _SoftMask_UVOuter[2], _SoftMask_UVInner[2], _SoftMaskTex2);
    if (_SoftMask_Count > 3.5)
        alpha *= Evo_UI_EvaluateSoftMask(canvasPos, _SoftMask_CanvasToLocalX[3], _SoftMask_CanvasToLocalY[3], _SoftMask_Rect[3], _SoftMask_Data[3], _SoftMask_PRRect[3], _SoftMask_PRRadii[3], _SoftMask_PRFillData[3], _SoftMask_BorderData[3], _SoftMask_UVOuter[3], _SoftMask_UVInner[3], _SoftMaskTex3);

    return alpha;
}

inline void SoftMask_Apply(inout half4 color, float4 canvasPos)
{
    color.a *= SoftMask_GetAlpha(canvasPos);
}

inline void SoftMask_ApplyPremultiplied(inout half4 color, float4 canvasPos)
{
    color *= SoftMask_GetAlpha(canvasPos);
}

#undef EVO_UI_PI
#undef EVO_UI_TWO_PI

#endif