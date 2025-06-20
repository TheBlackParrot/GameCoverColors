﻿using UnityEngine;

namespace GameCoverColors.Extensions;

public static class Color
{
    public static float MinColorComponent(this UnityEngine.Color color) => Mathf.Max(0.001f, Mathf.Min(Mathf.Min(color.r, color.g), color.b));
    public static float GetYiq(this UnityEngine.Color color) => (color.r * 299) + (color.g * 587) + (color.b * 114);
}