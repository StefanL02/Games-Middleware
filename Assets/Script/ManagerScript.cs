using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class ManagerScript : MonoBehaviour
{
    // Start is called before the first frame update
    
    List<IPhysical> allObjects = new List<IPhysical>();
    void Start()
    {
        allObjects.Clear();

        var allMonobehaviours = FindObjectsOfType<MonoBehaviour>();

        foreach (var mb in allMonobehaviours)
        {
            if (mb is IPhysical physical)
            {
                allObjects.Add(physical);
            }
        }

    }

   

    // Update is called once per frame
    void Update()
    {
        for (int i0 = 0; i0 < allObjects.Count - 1; i0++)
        {
            for (int j0 = i0 + 1; j0 < allObjects.Count; j0++)
            {
                int i, j;

                if (allObjects[i0].rank >= allObjects[j0].rank)
                {
                    i = i0; 
                    j = j0;
                }
                else
                {
                    i = j0;
                    j = i0;
                }
                if (allObjects[i].IsColliding(allObjects[j]))
                {
                    Vector3 PoS = Vector3.zero, vel = Vector3.zero;
                   // allObjects[j].velocity =
                        allObjects[i].ResolveCollision(allObjects[j], ref PoS, ref vel);
                        allObjects[j].overrideAfterCollision(PoS, vel);


                }
            }
        }
          
    }
}
