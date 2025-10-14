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
    public int Tier;
    public int Price;

    [Header("Vehicle Type")]
    [Tooltip("Check this box if the vehicle is a helicopter (affects cursor and potential movement logic).")]
    public bool isHelicopter = false;
}
