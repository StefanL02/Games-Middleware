    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class PlanePhysics : MonoBehaviour , IPhysical


    {
        public Vector3 normal
        {
            get => transform.up;
            set => transform.up = value.normalized;  
        }

    public int rank => 0;

    public bool IsColliding(IPhysical other)
    {
        if(other is PlanePhysics)
        {
            return false;
        }


        if (other is SpherePhysics sphere)
        {
            return isCollidingWith(sphere);
        }

        return false;
        /*
        if ( other is SpherePhysics spherePhysics)
        {
            Vector3 v = spherePhysics.transform.position - this.transform.position;
            Vector3 p = Utils.ParallelTo(v, normal);
            return p.magnitude < spherePhysics.Radius;
        }
        return false;
        */
    }

    public void overrideAfterCollision(Vector3 pos, Vector3 vel)
    {
       
    }

    public void ResolveCollision(IPhysical other, ref Vector3 position, ref Vector3 velocity)
    {
        
    }

    internal bool isCollidingWith(SpherePhysics spherePhysics)
        {
            Vector3 v = spherePhysics.transform.position - this.transform.position;
            Vector3 p = Utils.ParallelTo(v, normal);   
            return p.magnitude < spherePhysics.Radius;
        }

        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
