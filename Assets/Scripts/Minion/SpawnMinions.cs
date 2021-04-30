using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnMinions : MonoBehaviour
{
    public int numOfMinions; //Cantidad de minions a spawnear
    public GameObject prefabMinion;
    public Collider spawnPlatform; //Plataforma en la que harán spawn los minion

    private Vector3 minPlatCoords, maxPlatCoords;
    private Vector3 tempSpawnPos;

    void Start()
    {
        minPlatCoords = spawnPlatform.bounds.min;
        maxPlatCoords = spawnPlatform.bounds.max;

        for(int i = 0; i < numOfMinions; i++)
        {
            InstantiateMinions();
        }
    }

    private void InstantiateMinions()
    {
        //Crear posicion random para instanciar (siempre dentro de los límites de la plataforma
        tempSpawnPos = new Vector3(Random.Range(minPlatCoords.x, maxPlatCoords.x), maxPlatCoords.y + 2f, Random.Range(minPlatCoords.z, maxPlatCoords.z));

        Instantiate(prefabMinion, tempSpawnPos, Quaternion.identity); //Instatiate minion
    }
}
