#if (UNITY_EDITOR || UNITY_STANDALONE) && !UNITY_WEBGL
using System.Collections.Generic;
using System;
using NumSharp;

public class npyParser
{
    public static Timeline loadNpy(string path, double timestep)
    {
        throw new NotImplementedException("Numpy is not accessible on webgl");
        Dictionary<Time, position> processed = new Dictionary<Time, position>();

        var py = np.Load<double[,]>(path);

        return _loadNpy(py, timestep);
    }

    private static Timeline _loadNpy(double[,] npy, double timestep)
    {
        Dictionary<double, position> processed = new Dictionary<double, position>();
        for (int x = 0; x < npy.GetLength(0); x++)
        {
            double t = npy[x, 0];
            position p = new position(
                npy[x, 1],
                npy[x, 2],
                npy[x, 3]);

            processed.Add(t, p);
        }

        return new Timeline(processed, timestep);
    }
}
#endif