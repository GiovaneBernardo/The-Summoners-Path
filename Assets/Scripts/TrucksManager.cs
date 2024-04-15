
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plaza;
using static Plaza.InternalCalls;
using static Plaza.Input;
using static Plaza.Physics;

public class TravellerObjectsManager : Entity
{
    public static float flyingObjectsSpeed = 48.0f;
    public static float cactusSpeed = 100.0f;
    public static float flyingBallSpeed = 336.0f;
    public static List<UInt64> flyingObjects = new List<UInt64>();
    public static List<UInt64> flyingBalls = new List<UInt64>();
    public static List<Vector3> staticSwordsPosition = new List<Vector3>();
    public static UInt64 terrainUuid = 0;
    public static List<UInt64> staticSwordsEntityUuid = new List<UInt64>();

    public static List<UInt64> flyingCactusUuids = new List<UInt64>();
    public static List<Vector3> flyingCactusPositions = new List<Vector3>();
    public void OnStart()
    {
        terrainUuid = FindEntityByName("Terrain").Uuid;

        for (int i = 0; i < 8; ++i)
        {
            Entity newStaticSword = Instantiate(FindEntityByName("StaticSword"));
            staticSwordsEntityUuid.Add(newStaticSword.Uuid);

            Vector3 randomPosition = GetStaticSwordPosition(GetCorrectedCenter(FindEntityByName("Player").GetComponent<Transform>().Translation));
            newStaticSword.GetComponent<Transform>().Translation = randomPosition;
            staticSwordsPosition.Add(randomPosition);
        }

        for (int i = 0; i < 4; ++i)
        {
            Entity instantiatedCactus = Instantiate(FindEntityByName("Cactus1"));
            instantiatedCactus.Name = "CactusA" + i;
            flyingCactusUuids.Add(instantiatedCactus.Uuid);
            flyingCactusPositions.Add(GenerateRandomCactusPosition(FindEntityByName("Player").GetComponent<Transform>().Translation));
        }
    }

    public static Vector3 GetCorrectedCenter(Vector3 vector)
    {
        return new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 correctedCenter = vector;
        correctedCenter.Z = Math.Min(correctedCenter.Z, -250.0f);
        correctedCenter.Z = Math.Max(correctedCenter.Z, 250.0f);
        return correctedCenter;
    }

    public static Vector3 GetStaticSwordPosition(Vector3 center)
    {
        Random random = new Random();
        Vector3 randomPosition = new Vector3((float)random.NextDouble() * 500 - 250, 1000.0f, (float)random.NextDouble() * 800 - 400);
        randomPosition += center;
        RaycastHit hit;
        Physics_RaycastSpecific(randomPosition, new Vector3(0.0f, -1.0f, 0.0f), 2000.0f, out hit, terrainUuid);
        randomPosition.Y = hit.point.Y + 75.0f;
        return randomPosition;
    }

    public static void Reset(Vector3 playerPosition)
    {
        /* Regenerate static swords */
        for (int j = 0; j < staticSwordsPosition.Count; ++j)
        {
            CharacterController.swordsCount++;

            Vector3 randomPosition = GetStaticSwordPosition(GetCorrectedCenter(playerPosition));
            new Entity(staticSwordsEntityUuid[j]).GetComponent<Transform>().Translation = randomPosition;
            staticSwordsPosition[j] = randomPosition;

            continue;
        }

        /* Regenerate Cactus */
        for (int i = 0; i < flyingCactusPositions.Count; ++i)
        {
            RegenerateCactus(i, playerPosition);
        }
    }

    public void OnUpdate()
    {
        Vector3 playerPosition = FindEntityByName("Player").GetComponent<Transform>().Translation;
        flyingObjectsSpeed = 48.0f;
        for (int i = 0; i < flyingObjects.Count; ++i)
        {
            if (Vector3.Distance(new Entity(flyingObjects[i]).GetComponent<Transform>().Translation, playerPosition) > 1500.0f)
            {
                flyingObjects.RemoveAt(i);
                //InternalCalls.EntityDelete(flyingObjects[i]);
                //new Entity(flyingObjects[i]).Delete();
                continue;
            }
            new Entity(flyingObjects[i]).GetComponent<Transform>().MoveTowards(new Vector3(0.0f, flyingObjectsSpeed * Time.deltaTime, 0.0f));
            //trucks[i].GetComponent<RigidBody>().AddForce(new Vector3(100000.0f, 0.0f, 0.0f) * Time.deltaTime);
        }

        for (int i = 0; i < flyingBalls.Count; ++i)
        {
            Vector3 ballPosition = new Entity(flyingBalls[i]).GetComponent<Transform>().Translation;
            if (Vector3.Distance(ballPosition, playerPosition) > 1500.0f)
            {
                flyingBalls.RemoveAt(i);
                continue;
            }
            for (int j = 0; j < staticSwordsPosition.Count; ++j)
            {
                if (Vector3.Distance(ballPosition, staticSwordsPosition[j]) < 30.0f)
                {
                    HitStaticSword(i, j);
                    continue;
                }
            }

            new Entity(flyingBalls[i]).GetComponent<Transform>().MoveTowards(new Vector3(0.0f, 0.0f, flyingBallSpeed * Time.deltaTime));
        }

        UpdateCactus(playerPosition);
    }

    public void HitStaticSword(int i, int j)
    {
        CharacterController.PlaySwordPointSound();
        CharacterController.swordsCount += 3;

        Vector3 randomPosition = GetStaticSwordPosition(GetCorrectedCenter(FindEntityByName("Player").GetComponent<Transform>().Translation));
        new Entity(staticSwordsEntityUuid[j]).GetComponent<Transform>().Translation = randomPosition;
        staticSwordsPosition[j] = randomPosition;

        InternalCalls.EntityDelete(flyingBalls[i]);
        flyingBalls.RemoveAt(i);
    }

    public void UpdateCactus(Vector3 playerPosition)
    {
        for (int i = 0; i < flyingCactusUuids.Count; ++i)
        {
            Entity cactus = new Entity(flyingCactusUuids[i]);
            RaycastHit hit;
            Physics_RaycastSpecific(flyingCactusPositions[i] + new Vector3(0.0f, 0.0f, cactusSpeed * Time.deltaTime), new Vector3(0.0f, -1.0f, 0.0f), 1000.0f, out hit, terrainUuid);
            cactus.GetComponent<Transform>().Translation = hit.point + new Vector3(30.0f);
            flyingCactusPositions[i] = hit.point + new Vector3(0.0f, 300.0f, 0.0f);

            if (hit.hitUuid == 0 || (playerPosition.Z - hit.point.Z < 10.0f && hit.point.Z > -400.0f)) 
            {
                RegenerateCactus(i, FindEntityByName("Player").GetComponent<Transform>().Translation);
            }
        }
    }

    public static Vector3 GenerateRandomCactusVector()
    {
        Random random = new Random();
        return new Vector3((float)random.NextDouble() * 650.0f - 325.0f, 100.0f, -500.0f);
    }

    public static Vector3 GenerateRandomCactusPosition(Vector3 playerPosition)
    {
        Vector3 randomVector = GenerateRandomCactusVector();
        if (Vector3.Distance(randomVector, playerPosition) < 10.0f)
            return GenerateRandomCactusPosition(playerPosition);
        return randomVector;
    }

    public static void RegenerateCactus(int index, Vector3 playerPosition)
    {
        flyingCactusPositions[index] = GenerateRandomCactusPosition(playerPosition);
    }

    public void OnRestart()
    {

    }
}
