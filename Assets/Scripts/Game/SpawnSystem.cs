﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class SpawnSystem : NetworkBehaviour
{
    //gameobjects to hold player and spawnpoint prefabs
    [SerializeField] private GameObject playerPrefab = null;
    [SerializeField] private GameObject spawnPoint = null;

    //list of coords for spawnpoints
    private static List<Transform> spawnPoints = new List<Transform>();

    //simple counter to increment as each player connects
    private int nextIndex = 0;

    //instance of pathfinding
    Pathfinding pathfinding = Pathfinding.Instance;

    //method called from PlayerSpawnPoint adds coords to list
    public static void AddSpawnPoint(Transform transform)
    {
        spawnPoints.Add(transform);

        //Order by position in heriarchy
        spawnPoints = spawnPoints.OrderBy(x => x.GetSiblingIndex()).ToList();
    }

    //method called from PlayerSpawnPoint removes coords from list
    public static void RemoveSpawnPoint(Transform transform) => spawnPoints.Remove(transform);

    //when server starts and calls OnServerReadied event, call SpawnPlayer method
    public override void OnStartServer() => NetworkManagerDND.OnServerReadied += SpawnPlayer;

    //when players leaves, call event and remove player
    [ServerCallback]
    private void OnDestroy() => NetworkManagerDND.OnServerReadied -= SpawnPlayer;

    //Server code to spawn player prefabs on every client instance
    [Server]
    public void SpawnPlayer(NetworkConnection conn)
    {
        //get coords of grid 0,0 and instantiate spawnpoint prefab there
        pathfinding.GetGrid().GetXY(new Vector3(0,0), out int x, out int y);
        Vector3 cell = new Vector3(x + nextIndex, y) * pathfinding.GetGrid().GetCellSize() + Vector3.one * pathfinding.GetGrid().GetCellSize() * .5f;
        cell.z = 0;
        Instantiate(spawnPoint, cell, Quaternion.identity);

        //coords of current spawnPoint (from nextIndex counter)
        Transform listSpawnPoint = spawnPoints.ElementAtOrDefault(nextIndex);

        //if null ERROR
        if(listSpawnPoint == null)
        {
            Debug.LogError($"Missing spawn point for player {nextIndex}");
            return;
        }

        //instantiate player prefab at current spawnpoint location and then tie it to client connection
        var playerInstance = Instantiate(playerPrefab, spawnPoints[nextIndex].position, spawnPoints[nextIndex].rotation);
        playerInstance.transform.localScale = new Vector3(pathfinding.GetGrid().GetCellSize() / 5, pathfinding.GetGrid().GetCellSize() / 5, 1);
        NetworkServer.Spawn(playerInstance, conn);

        //increment counter
        nextIndex++;
    }
}
