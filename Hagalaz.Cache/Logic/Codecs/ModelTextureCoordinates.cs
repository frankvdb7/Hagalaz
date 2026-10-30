using System;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Revision-742 model projections from Model.method4441/4443/4444/4459/4492.</summary>
internal static class ModelTextureCoordinates
{
    internal sealed record Mapping(int Type, int A, int B, int C, int ScaleX = 0, int ScaleY = 0, int ScaleZ = 0,
        int Rotation = 0, int Direction = 0, int Offset = 0, int OffsetY = 0, int OffsetZ = 0);

    internal static float[] Generate(int[] positions, int[] indices, int[] textureIds, int[] pointers, Mapping[] mappings)
    {
        var result = new float[indices.Length * 2];
        var centers = new int[mappings.Length][];
        var matrices = new float[mappings.Length][];
        for (var mapping = 0; mapping < mappings.Length; mapping++)
        {
            if (mappings[mapping].Type == 0) continue;
            var min = new[] { int.MaxValue, int.MaxValue, int.MaxValue };
            var max = new[] { int.MinValue, int.MinValue, int.MinValue };
            for (var face = 0; face < pointers.Length; face++)
            {
                if (textureIds[face] < 0 || pointers[face] != mapping) continue;
                for (var corner = 0; corner < 3; corner++)
                    for (var axis = 0; axis < 3; axis++)
                    {
                        var value = positions[indices[face * 3 + corner] * 3 + axis];
                        min[axis] = Math.Min(min[axis], value);
                        max[axis] = Math.Max(max[axis], value);
                    }
            }
            centers[mapping] = [(min[0] + max[0]) / 2, (min[1] + max[1]) / 2, (min[2] + max[2]) / 2];
            matrices[mapping] = Matrix(mappings[mapping]);
        }
        for (var face = 0; face < textureIds.Length; face++)
        {
            if (textureIds[face] < 0) continue;
            var pointer = pointers[face];
            if (pointer < 0)
            {
                // The client's default mapping is different from an explicit face-aligned triangle.
                Array.Copy(new float[] { 0, 1, 1, 1, 0, 0 }, 0, result, face * 6, 6);
                continue;
            }
            var map = mappings[pointer];
            var boxSide = 0;
            if (map.Type == 2)
            {
                var a = Point(indices[face * 3]);
                var b = Point(indices[face * 3 + 1]);
                var c = Point(indices[face * 3 + 2]);
                var nx = (b[1] - a[1]) * (c[2] - a[2]) - (c[1] - a[1]) * (b[2] - a[2]);
                var ny = (b[2] - a[2]) * (c[0] - a[0]) - (c[2] - a[2]) * (b[0] - a[0]);
                var nz = (b[0] - a[0]) * (c[1] - a[1]) - (c[0] - a[0]) * (b[1] - a[1]);
                var matrix = matrices[pointer];
                var x = (nx * matrix[0] + ny * matrix[1] + nz * matrix[2]) / (64f / map.ScaleX);
                var y = (nx * matrix[3] + ny * matrix[4] + nz * matrix[5]) / (64f / map.ScaleY);
                var z = (nx * matrix[6] + ny * matrix[7] + nz * matrix[8]) / (64f / map.ScaleZ);
                boxSide = Math.Abs(y) > Math.Abs(x) && Math.Abs(y) > Math.Abs(z) ? (y > 0 ? 0 : 1)
                    : Math.Abs(z) > Math.Abs(x) && Math.Abs(z) > Math.Abs(y) ? (z > 0 ? 2 : 3) : x > 0 ? 4 : 5;
            }
            for (var corner = 0; corner < 3; corner++)
            {
                var point = Point(indices[face * 3 + corner]);
                var uv = map.Type == 0 ? Planar(point, Point(map.A), Point(map.B), Point(map.C))
                    : Project(point, centers[pointer], matrices[pointer], map, boxSide);
                result[face * 6 + corner * 2] = uv[0];
                result[face * 6 + corner * 2 + 1] = uv[1];
            }
            if (map.Type is 1 or 3)
            {
                var period = map.Type == 1 ? map.ScaleZ / 1024f : 1f;
                var axis = map.Direction & 1;
                var first = result[face * 6 + axis];
                for (var corner = 1; corner < 3; corner++)
                {
                    var offset = face * 6 + corner * 2 + axis;
                    if (result[offset] - first > period / 2) result[offset] -= period;
                    else if (first - result[offset] > period / 2) result[offset] += period;
                }
            }
        }
        foreach (var coordinate in result)
            if (!float.IsFinite(coordinate)) throw new NotSupportedException("Degenerate texture projection cannot produce finite coordinates.");
        return result;

        float[] Point(int vertex) => [positions[vertex * 3], positions[vertex * 3 + 1], positions[vertex * 3 + 2]];
    }

    private static float[] Planar(float[] point, float[] origin, float[] a, float[] b)
    {
        float uu = 0, uv = 0, vv = 0, pu = 0, pv = 0;
        for (var axis = 0; axis < 3; axis++)
        {
            var u = a[axis] - origin[axis]; var v = b[axis] - origin[axis]; var p = point[axis] - origin[axis];
            uu += u * u; uv += u * v; vv += v * v; pu += p * u; pv += p * v;
        }
        var determinant = uu * vv - uv * uv;
        return determinant == 0 ? [0, 0] : [(pu * vv - pv * uv) / determinant, (pv * uu - pu * uv) / determinant];
    }

    private static float[] Project(float[] point, int[] center, float[] matrix, Mapping map, int side)
    {
        var dx = point[0] - center[0]; var dy = point[1] - center[1]; var dz = point[2] - center[2];
        var x = dx * matrix[0] + dy * matrix[1] + dz * matrix[2];
        var y = dx * matrix[3] + dy * matrix[4] + dz * matrix[5];
        var z = dx * matrix[6] + dy * matrix[7] + dz * matrix[8];
        var offset = map.Offset / 256f;
        float u, v;
        if (map.Type == 2)
        {
            var oy = map.OffsetY / 256f; var oz = map.OffsetZ / 256f;
            (u, v) = side switch
            {
                0 => (x + offset + 0.5f, -z + oz + 0.5f),
                1 => (x + offset + 0.5f, z + oz + 0.5f),
                2 => (-x + offset + 0.5f, -y + oy + 0.5f),
                3 => (x + offset + 0.5f, -y + oy + 0.5f),
                4 => (z + oz + 0.5f, -y + oy + 0.5f),
                _ => (-z + oz + 0.5f, -y + oy + 0.5f)
            };
        }
        else
        {
            u = (float)Math.Atan2(x, z) / 6.2831855f + 0.5f;
            if (map.Type == 1) { u *= map.ScaleZ / 1024f; v = y + 0.5f + offset; }
            else v = (float)Math.Asin(y / (float)Math.Sqrt(x * x + y * y + z * z)) / 3.1415927f + 0.5f + offset;
        }
        return map.Direction switch { 1 => [-v, u], 2 => [-u, -v], 3 => [v, -u], _ => [u, v] };
    }

    private static float[] Matrix(Mapping map)
    {
        var cos = (float)Math.Cos(map.Rotation * 0.024543693f);
        var sin = (float)Math.Sin(map.Rotation * 0.024543693f);
        float[] rotation = [cos, 0, sin, 0, 1, 0, -sin, 0, cos];
        var tiltCos = map.B / 32767f;
        var tiltSin = -(float)Math.Sqrt(1 - tiltCos * tiltCos);
        var oneMinusCos = 1 - tiltCos;
        var length = (float)Math.Sqrt(map.A * map.A + map.C * map.C);
        var result = new float[9];
        if (length == 0 && tiltCos == 0) Array.Copy(rotation, result, 9);
        else
        {
            var ax = length == 0 ? 1 : -map.C / length;
            var az = length == 0 ? 0 : map.A / length;
            float[] tilt = [tiltCos + ax * ax * oneMinusCos, az * tiltSin, az * ax * oneMinusCos,
                -az * tiltSin, tiltCos, ax * tiltSin,
                ax * az * oneMinusCos, -ax * tiltSin, tiltCos + az * az * oneMinusCos];
            for (var row = 0; row < 3; row++)
                for (var column = 0; column < 3; column++)
                    for (var k = 0; k < 3; k++) result[row * 3 + column] += rotation[row * 3 + k] * tilt[k * 3 + column];
        }
        float sx, sy, sz;
        if (map.Type == 1)
        {
            sx = map.ScaleX < 0 ? -map.ScaleX / 1024f : 1;
            sz = map.ScaleX > 0 ? map.ScaleX / 1024f : 1;
            sy = 64f / map.ScaleY;
        }
        else if (map.Type == 2) { sx = 64f / map.ScaleX; sy = 64f / map.ScaleY; sz = 64f / map.ScaleZ; }
        else { sx = map.ScaleX / 1024f; sy = map.ScaleY / 1024f; sz = map.ScaleZ / 1024f; }
        for (var i = 0; i < 9; i++) result[i] *= i < 3 ? sx : i < 6 ? sy : sz;
        return result;
    }
}
