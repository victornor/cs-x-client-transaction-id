using System;

namespace XClientTransactionId;

public static class RotationUtils
{
    public static double[] ConvertRotationToMatrix(double rotation)
    {
        double rad = rotation * Math.PI / 180;
        return new double[] { Math.Cos(rad), -Math.Sin(rad), Math.Sin(rad), Math.Cos(rad) };
    }

    public static double[] ConvertRotationToMatrix2(double degrees)
    {
        double radians = degrees * Math.PI / 180;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);
        return new double[] { cos, sin, -sin, cos, 0, 0 };
    }
}