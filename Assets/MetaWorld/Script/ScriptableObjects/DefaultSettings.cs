using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "AppDefaultSettings", menuName = "ScriptableObjects/DefaultSettings", order = 1)]
public class DefaultSettings : ScriptableObject
{
    public GameSettings defaultGameSettings;
}
