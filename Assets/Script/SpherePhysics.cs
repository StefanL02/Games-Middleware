using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.Diagnostics;


public class SpherePhysics : MonoBehaviour, IPhysical
{
    
    public Vector3 previousPosition, V0;
    Vector3 acceleration, velocity;
    float gravity = 9.81f;
    float CoR = 0.5f;
    internal float mass = 1.0f;

  

    private void Start()
    {
        
    }


    private void Update()
    {
        V0 = velocity;
        previousPosition = transform.position;
        acceleration = gravity * Vector3.down;
        velocity += acceleration * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        
        

       // if (plane.isCollidingWith(this))
        {
            
           /* transform.position-=velocity * Time.deltaTime;
            Vector3 vel_parallel = Utils.ParallelTo(vel_ToI,plane.normal);
            Vector3 vel_perpendicullar = Utils.PerpendicularTo(vel_ToI, plane.normal);
            velocity = vel_perpendicullar - CoR * vel_parallel;
            */

            
            //Calculate of time of impact? Calcuate the position of the spheres and recalcualte the velocity?
            // Normal of collision at time of impact
            /* Vector3 normal = (sphere1 - sphere2).normalized;

             Vector3 sphere1Parallel = Utils.ParallelTo(velocity, normal);
             Vector3 sphere1Perpendicular = Utils.PerpendicularTo(velocity, normal);
             Vector3 sphere2Parallel = Utils.ParallelTo(sphere2.velocity, normal);
             Vector3 sphere2Perpendicular = Utils.PerpendicularTo(sphere2.velocity, normal);
             */
        }

    }


    public bool isCollidingWithSphere(SpherePhysics otherSphere)
    {
        return Vector3.Distance(transform.position, otherSphere.transform.position) < (Radius + otherSphere.Radius );
    }

    public bool IsColliding(IPhysical other)
    {
        if (other is PlanePhysics)
        {
            PlanePhysics plane = other as PlanePhysics;
            Vector3 v = transform.position - plane.transform.position;
            Vector3 p = Utils.ParallelTo(v, plane.normal);

            return p.magnitude < Radius;
        }

        if (other is SpherePhysics otherSphere)
        {
            return Vector3.Distance(transform.position, otherSphere.transform.position) < (Radius + otherSphere.Radius);
        }

        return false;
    }

    public void ResolveCollision(IPhysical other, ref Vector3 otherPos, ref Vector3 otherVel)
    {

        float TimeInterval = Time.deltaTime;


        if (other is PlanePhysics)
        {
            PlanePhysics plane = other as PlanePhysics;

            
            float D0 = Utils.DistanceToPlane(previousPosition, plane) - Radius;
            float D1 = Utils.DistanceToPlane(transform.position, plane) - Radius;
            float speed = (D1 - D0) / TimeInterval;
            float ToI = -D0 / speed;

            Vector3 vel_ToI = V0+acceleration* ToI;
            Vector3 pos_ToI = previousPosition + vel_ToI * ToI;

            Vector3 vel_parallel = Utils.ParallelTo(vel_ToI, plane.normal);
            Vector3 vel_perpendicullar = Utils.PerpendicularTo(vel_ToI, plane.normal);
            Vector3 vel_res = vel_perpendicullar - CoR * vel_parallel;

            velocity = vel_res + acceleration * (TimeInterval - ToI);
            transform.position = pos_ToI + velocity * (TimeInterval - ToI);

            float d = Utils.DistanceToPlane(transform.position, plane) - Radius;

            if (d < 0)
            {
                transform.position -= d * plane.normal;
            }
        }

        if (other is SpherePhysics) 
        {
            SpherePhysics sphere = (SpherePhysics)other;
            //Calcualte ToI
            float D0 = Vector3.Distance(previousPosition, sphere.previousPosition) - Radius - sphere.Radius;
            float D1 = Vector3.Distance(transform.position, sphere.transform.position) - Radius - sphere.Radius;
            float speed = (D1 - D0) / TimeInterval;
            float ToI = -D0 / speed;

            Vector3 vel_ToI = V0 + acceleration * ToI;
            Vector3 pos_ToI = previousPosition + vel_ToI * ToI;

            Vector3 vel_ToIOther = sphere.V0 + sphere.acceleration * ToI;
            Vector3 pos_ToIOther = sphere.previousPosition + vel_ToIOther * ToI;

            Vector3 normal = (pos_ToI - pos_ToIOther).normalized;
            Vector3 vel_parallel = Utils.ParallelTo(vel_ToI, normal);
            Vector3 vel_perp = Utils.PerpendicularTo(vel_ToI, normal);
            Vector3 vel_parallelOther = Utils.ParallelTo(vel_ToIOther, normal);
            Vector3 vel_perpOther = Utils.PerpendicularTo(vel_ToIOther, normal);



            Vector3 velPerpAfter = ElasticCollision(vel_parallel, vel_parallelOther, mass, sphere.mass);
            Vector3 velPerpAfterOther = ElasticCollision(vel_parallelOther, vel_parallel, sphere.mass, mass);
            Vector3 velAfter = -CoR * vel_parallel + velPerpAfter;
            Vector3 velAfterOther = -CoR * vel_parallelOther + velPerpAfterOther;

            float remainingTime = TimeInterval - ToI;
            velocity = velAfter + acceleration * remainingTime;
            transform.position = pos_ToI + velocity * remainingTime;

            otherVel = velAfterOther + sphere.acceleration * remainingTime; 
            otherPos = pos_ToIOther + otherVel * remainingTime;
            return;



            /*
            float u1 = Vector3.Dot(vel_perp, normal);
            float u2 = Vector3.Dot(vel_perpOther, normal);

            float v1 = (u1 * (mass - CoR * mass) + (1 + CoR) * mass * u2) / (mass + mass);
            float v2 = (u2 * (mass - CoR * mass) + (1 + CoR) * mass * u1) / (mass + mass);


            Vector3 vel_perpAfter = normal * v1;
            Vector3 vel_perpAfterOther = normal * v2;

            Vector3 velocityAfter = vel_perpAfter + vel_parallel;
            Vector3 velocityAfterOther = vel_perpAfterOther + vel_parallelOther;

            velocity = velocityAfter + acceleration * (TimeInterval - ToI);
            transform.position = pos_ToI + velocity * (TimeInterval - ToI);

            */



        }
    }

    private Vector3 ElasticCollision(Vector3 vel_parallel, Vector3 vel_parallelOther, float mass1, float mass2)
    {
        float calc1 = ((mass1 - mass2) / (mass1 + mass2));
        Vector3 x = calc1 * vel_parallel;
        float calc2 = ((2 * mass2) / (mass1 + mass2));
        Vector3 y = calc2 * vel_parallelOther;

        return x + y;
    }


    public void overrideAfterCollision(Vector3 pos, Vector3 vel)
    {
        transform.position = pos;
        this.velocity = vel;
    }

    public float Radius
    {
        get
        {
          
            float diameter = Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);
            return diameter / 2f;
        }
        set
        {
          
            transform.localScale = value * 2f * Vector3.one;
        }
    }

    public int rank => 1;
}

