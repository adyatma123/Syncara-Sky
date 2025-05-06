using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerVehicle", menuName = "Vehicles/Player")]
public class Vehicles : ScriptableObject
{
    public new string name;
    public string description;

    public Sprite artwork;
    public GameObject vehiclePrefab;
    public AudioSource Shootsound;

    public int health;
    public int maxHeat;
    public float movSpeed;
    public float rotSpeed;
    public float maxRot = 45f;
    public int PayloadLevel;
    public int Price;
}
