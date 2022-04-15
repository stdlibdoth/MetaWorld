using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "PrefabTable", menuName = "ScriptableObjects/PrefabTable", order = 1)]
public class PrefabTable : ScriptableObject
{
    public PrefabEntry[] prefabEntries;
}
