#ifndef RECONSTRUCTWORLDPOSITION_INCLUDED
#define RECONSTRUCTWORLDPOSITION_INCLUDED

float4x4 _CameraInverseProjection;

void GetCameraInvProjection_float(out float4x4 Out)
{
    Out = _CameraInverseProjection;
}

#endif // RECONSTRUCTWORLDPOSITION_INCLUDED