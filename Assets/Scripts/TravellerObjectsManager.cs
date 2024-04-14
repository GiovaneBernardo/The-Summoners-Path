
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plaza;
using static Plaza.InternalCalls;
using static Plaza.Input;

public class TravellerObjectsManager : Entity
{
    public static List<UInt64> flyingObjects = new List<UInt64>();
    public void OnStart()
    {

    }

    public void OnUpdate()
    {
        for(int i = 0; i < flyingObjects.Count; ++i)
        {
            new Entity(flyingObjects[i]).GetComponent<Transform>().GetComponent<Transform>().Translation += new Vector3(1.0f * Time.deltaTime, 0.0f, 0.0f);
            //trucks[i].GetComponent<RigidBody>().AddForce(new Vector3(100000.0f, 0.0f, 0.0f) * Time.deltaTime);
        }
    }

    public void OnRestart()
    {

    }
}
