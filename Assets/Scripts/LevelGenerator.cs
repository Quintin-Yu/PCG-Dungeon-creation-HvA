using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public enum TileType {
    Empty = 0,
    Player,
    Enemy,
    Wall,
    Door,
    Key,
    Dagger,
    End
}

public class LevelGenerator : MonoBehaviour
{
    public GameObject[] tiles;
    public List<SubDungeon> rooms = new List<SubDungeon> (); 

    int maxDungeonSize;
    int minRoomSize = 10;
    int maxRoomSize = 1;

    TileType[,] grid;   

    protected void Start()
    {
        Random.InitState((int)System.DateTime.Now.Ticks);

        maxDungeonSize = Random.Range(64, 129);
        grid = new TileType[maxDungeonSize, maxDungeonSize];

        int width = maxDungeonSize;
        int height = maxDungeonSize;

        FillBlock(grid, 0, 0, width, height, TileType.Wall);

        SubDungeon rootSubDungeon = new SubDungeon(new Rect(0, 0, width, height));
        CreateBSP(rootSubDungeon);
        rootSubDungeon.CreateRoom();

        DrawRooms(rootSubDungeon);
        DrawCorridors(rootSubDungeon);

        SaveRooms(rootSubDungeon);
        //SpawnPlayer(grid[1, 1], Random.Range(0, maxDungeonSize), Random.Range(0, maxDungeonSize));
        SpawnObjects();

        //use 2d array (i.e. for using cellular automata)
        CreateTilesFromArray(grid);
    }

    #region Original Code
    //fill part of array with tiles
    private void FillBlock(TileType[,] grid, int x, int y, int width, int height, TileType fillType) {
        for (int tileY=0; tileY<height; tileY++) {
            for (int tileX=0; tileX<width; tileX++) {
                grid[tileY + y, tileX + x] = fillType;
            }
        }
    }

    //use array to create tiles
    private void CreateTilesFromArray(TileType[,] grid) {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);
        for (int y=0; y<height; y++) {
            for (int x=0; x<width; x++) {
                 TileType tile = grid[y, x];
                 if (tile != TileType.Empty) {
                     CreateTile(x, y, tile);
                 }
            }
        }
    }

    //create a single tile
    private GameObject CreateTile(int x, int y, TileType type) {
        int tileID = ((int)type) - 1;
        if (tileID >= 0 && tileID < tiles.Length)
        {
            GameObject tilePrefab = tiles[tileID];
            if (tilePrefab != null) {
                GameObject newTile = GameObject.Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                newTile.transform.SetParent(transform);
                return newTile;
            }

        } else {
            Debug.LogError("Invalid tile type selected");
        }

        return null;
    }
    #endregion

    #region BSP
    public void CreateBSP(SubDungeon subDungeon)
    {
        //Debug.Log($"Splitting sub-dungeon {subDungeon.debugId}: {subDungeon.rect}");

        if (subDungeon.IAmLeaf())
        {
            // If the sub-dungeon is too large
            if (subDungeon.rect.width > maxRoomSize || subDungeon.rect.height > maxRoomSize || Random.Range(0.0f, 1.0f) > 0.25)
            {
                if (subDungeon.Split(minRoomSize, maxRoomSize))
                {
                    //Debug.Log("Splitted sub-dungeon " + subDungeon.debugId + " in "
                    //  + subDungeon.left.debugId + ": " + subDungeon.left.rect + ", "
                    //  + subDungeon.right.debugId + ": " + subDungeon.right.rect);

                    CreateBSP(subDungeon.left);
                    CreateBSP(subDungeon.right);
                }
            }
        }
    }

    public void DrawRooms(SubDungeon subDungeon)
    {
        if (subDungeon == null)
        {

            Debug.Log($"Entered DrawRooms Method with subDungeon {subDungeon}");
            return;
        }

        if (subDungeon.IAmLeaf())
        {
            FillBlock(grid, (int)subDungeon.room.x, (int)subDungeon.room.y, (int)subDungeon.room.width, (int)subDungeon.room.height, TileType.Empty);
            //Debug.Log($"Room pos x: {subDungeon.room.x} - Room pos y: {subDungeon.room.y} - Room width: {subDungeon.room.width} - Room height: {subDungeon.room.height}");
        }
        else
        {
            DrawRooms(subDungeon.left);
            DrawRooms(subDungeon.right);
        }
    }

    void DrawCorridors(SubDungeon subDungeon)
    {
        if (subDungeon == null)
        {
            return;
        }

        DrawCorridors(subDungeon.left);
        DrawCorridors(subDungeon.right);

        foreach (Rect corridor in subDungeon.corridors)
        {
            for (int i = (int)corridor.x; i < corridor.xMax; i++)
            {
                for (int j = (int)corridor.y; j < corridor.yMax; j++)
                {
                    /*if (boardPositionsFloor[i, j] == null)
                    {
                        GameObject instance = Instantiate(corridorTile, new Vector3(i, j, 0f), Quaternion.identity) as GameObject;
                        instance.transform.SetParent(transform);
                        boardPositionsFloor[i, j] = instance;
                    }*/
                    FillBlock(grid, i, j, 1, 1, TileType.Empty);
                }
            }            
        }
    }
    #endregion

    void SaveRooms(SubDungeon subDungeon)
    {
        if (subDungeon.IAmLeaf())
        {
            rooms.Add(subDungeon);
        }
        else
        {
            SaveRooms(subDungeon.left);
            SaveRooms(subDungeon.right);
        }
    }

    /// <summary>
    /// This method should be changed so that the objects are spawned using a formula based on the rooms.Count to avoid out of index errors.
    /// </summary>
    void SpawnObjects()
    {
        int startersRoom = (int)Random.Range(0, rooms.Count - 5);

        FillBlock(grid, (int)rooms[startersRoom].room.x + 2, (int)rooms[startersRoom].room.y + 2, 1, 1, TileType.Player);
        FillBlock(grid, (int)rooms[startersRoom++].room.x + 2, (int)rooms[startersRoom++].room.y + 2, 1, 1, TileType.Dagger);
        FillBlock(grid, (int)rooms[startersRoom + 2].room.x + 2, (int)rooms[startersRoom + 2].room.y + 2, 1, 1, TileType.Enemy);
        FillBlock(grid, (int)rooms[startersRoom + 3].room.x + 2, (int)rooms[startersRoom + 3].room.y + 2, 1, 1, TileType.Key);
        FillBlock(grid, (int)rooms[startersRoom + 4].room.x + 2, (int)rooms[startersRoom + 4].room.y + 2, 1, 1, TileType.Door);
        FillBlock(grid, (int)rooms[startersRoom + 5].room.x + 2, (int)rooms[startersRoom + 5].room.y + 2, 1, 1, TileType.End);
    }
}
