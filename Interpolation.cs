using System;

namespace XClientTransactionId;

public static class Interpolation
{
    public static double[] Interpolate(double[] fromList, double[] toList, double f)
    {
        if (fromList.Length != toList.Length)
        {
            throw new ArgumentException($"Mismatched interpolation arguments {string.Join(",", fromList)}: {string.Join(",", toList)}");
        }

        var result = new double[fromList.Length];
        for (int i = 0; i < fromList.Length; i++)
        {
            result[i] = InterpolateNum(fromList[i], toList[i], f);
        }
        return result;
    }

    public static double InterpolateNum(double fromVal, double toVal, double f)
    {
        return fromVal * (1 - f) + toVal * f;
    }

    public static double InterpolateBool(bool fromVal, bool toVal, double f)
    {
        return f < 0.5 ? (fromVal ? 1 : 0) : (toVal ? 1 : 0);
    }
}