using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathEdge<T>
{
    public float cost; //Cost to traverse this edge (ie cost to enter the tile)
    public PathNode<T> node;
}
