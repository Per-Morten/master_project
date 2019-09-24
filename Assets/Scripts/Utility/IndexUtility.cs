using Unity.Mathematics;

public class IndexUtility
{
    public static int3 IdxToCoords(int idx, int3 dimensions)
    {
        return new int3
        {
            x = (idx - (dimensions.y * dimensions.x) * (idx / (dimensions.y * dimensions.x))) % dimensions.x,
            y = (idx - (dimensions.y * dimensions.x) * (idx / (dimensions.y * dimensions.x))) / dimensions.x,
            z = idx / (dimensions.y * dimensions.x)
        };
    }

    public static int CoordsToIdx(int3 coords, int3 dimensions)
    {
        return ((dimensions.y * dimensions.x) * coords.z) + dimensions.x * coords.y + coords.x;
    }

    public static int3 CoordsToSubGrid(int3 coords, int3 dimensions, int3 subdimensions)
    {
        int3 ratio = subdimensions / dimensions;

        return new int3
        {
            x = coords.x * ratio.x,
            y = coords.y * ratio.y,
            z = coords.z * ratio.z
        };
    }

    public static int3 CoordsToOuterGrid(int3 coords, int3 dimensions, int3 outerDimensions)
    {
        int3 ratio = dimensions / outerDimensions;
        return new int3
        {
            x = coords.x / ratio.x,
            y = coords.y / ratio.y,
            z = coords.z / ratio.z
        };
    }
}
