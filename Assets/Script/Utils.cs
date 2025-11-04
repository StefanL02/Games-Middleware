using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils 
{
   
    public static Vector3 ParallelTo(Vector3 v, Vector3 n)
    {
        Vector3 n_normal = n.normalized;
        return Vector3.Dot(v, n_normal)*n_normal;
    }

    public static Vector3 PerpendicularTo(Vector3 v, Vector3 n)
    {
        return v-ParallelTo(v, n);
    }

    public static float DistanceToPlane(Vector3 point , PlanePhysics p)
    {
        Vector3 v= point-p.transform.position;
        return Vector3.Dot(v, p.normal);
    }

    
        
    
}
