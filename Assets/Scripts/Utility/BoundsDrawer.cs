using System.Collections;
using UnityEngine;


public class BoundsDrawer : MonoBehaviour
{
    public static void Draw(Bounds bounds, Color color)
    {
        var botLeft = bounds.center;
        botLeft.x = bounds.center.x - bounds.extents.x;
        botLeft.y = bounds.center.y - bounds.extents.y;
        botLeft.z = bounds.center.z - bounds.extents.z;

        var botRight = bounds.center;
        botRight.x = bounds.center.x + bounds.extents.x;
        botRight.y = bounds.center.y - bounds.extents.y;
        botRight.z = bounds.center.z - bounds.extents.z;

        var topLeft = bounds.center;
        topLeft.x = bounds.center.x - bounds.extents.x;
        topLeft.y = bounds.center.y + bounds.extents.y;
        topLeft.z = bounds.center.z - bounds.extents.z;

        var topRight = bounds.center;
        topRight.x = bounds.center.x + bounds.extents.x;
        topRight.y = bounds.center.y + bounds.extents.y;
        topRight.z = bounds.center.z - bounds.extents.z;

        Debug.DrawLine(botLeft, botRight, color);
        Debug.DrawLine(botLeft, topLeft, color);
        Debug.DrawLine(botRight, topRight, color);
        Debug.DrawLine(topLeft, topRight, color);

        var newBotLeft = botLeft;
        var newBotRight = botRight;
        var newTopLeft = topLeft;
        var newTopRight = topRight;

        newBotLeft.z = bounds.center.z + bounds.extents.z;
        newBotRight.z = bounds.center.z + bounds.extents.z;
        newTopLeft.z = bounds.center.z + bounds.extents.z;
        newTopRight.z = bounds.center.z + bounds.extents.z;

        Debug.DrawLine(newBotLeft, newBotRight, color);
        Debug.DrawLine(newBotLeft, newTopLeft, color);
        Debug.DrawLine(newBotRight, newTopRight, color);
        Debug.DrawLine(newTopLeft, newTopRight, color);

        Debug.DrawLine(botLeft, newBotLeft, color);
        Debug.DrawLine(botRight, newBotRight, color);
        Debug.DrawLine(topLeft, newTopLeft, color);
        Debug.DrawLine(topRight, newTopRight, color);
    }

    public static void Draw(Unity.Mathematics.AABB bounds, Color32 color)
    {
        Draw(new Bounds(bounds.Center, bounds.Extents * 2), color);
    }
}

