using UnityEngine;

public class MouseSelect : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var e = Unity.Entities.World.Active.EntityManager.CreateEntity();
            Unity.Entities.World.Active.EntityManager.AddComponentData(e, new RayRequest { Value = new Unity.Physics.Ray { Origin = ray.origin, Displacement = ray.origin + ray.direction * 100 } });
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            Globals.Instance.data.MeshParent.SetActive(!Globals.Instance.data.MeshParent.activeSelf);
            Globals.Instance.data.StaticParent.SetActive(!Globals.Instance.data.StaticParent.activeSelf);
        }
    }
}
