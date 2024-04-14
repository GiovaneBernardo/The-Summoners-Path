
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
    public static float flyingObjectsSpeed = 48.0f;
    public static List<UInt64> flyingObjects = new List<UInt64>();
    public void OnStart()
    {

    }

    public void OnUpdate()
    {
        Vector3 playerPosition = FindEntityByName("Player").GetComponent<Transform>().Translation;
        flyingObjectsSpeed = 48.0f;
        for (int i = 0; i < flyingObjects.Count; ++i)
        {
            if (Vector3.Distance(new Entity(flyingObjects[i]).GetComponent<Transform>().Translation, playerPosition) > 500.0f)
            {
                flyingObjects.RemoveAt(i);
                //new Entity(flyingObjects[i]).Delete();
                continue;
            }
            new Entity(flyingObjects[i]).GetComponent<Transform>().MoveTowards(new Vector3(0.0f, flyingObjectsSpeed * Time.deltaTime, 0.0f));
            //trucks[i].GetComponent<RigidBody>().AddForce(new Vector3(100000.0f, 0.0f, 0.0f) * Time.deltaTime);
        }
    }

    public void OnRestart()
    {

    }
}
