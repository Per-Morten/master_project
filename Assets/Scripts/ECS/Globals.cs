using UnityEngine;

// Used for interacting with Unity ECS through the editor, as systems aren't properly integrated there yet.
public class Globals : SingletonBase<Globals>
{
    public Data data;

    [System.Serializable]
    public class Data
    {
        public string StreamFilename;
        public bool DrawBounds = true;
        public int BoundsLayer = 0;
        public Vector3 Direction = Vector3.forward;
        public Material OpaqueMaterial;
        public Material TransparentMaterial;

        public GameObject StaticParent;
        public GameObject MeshParent;
        public GameObject IFCGuidUIPrefab;
    }
}