using UnityEngine;
using UnityEngine.UI;

public class VhcDis : MonoBehaviour
{
    [Header("Aircraft Description")]
    [SerializeField] private Text vehicleName;
    [SerializeField] private Text vehicleDesc;

    [Header("Aircraft Model")]
    [SerializeField] private Transform vehicleContainer;

    public void VehicleDisplayer(Vehicles _vehicles)
    {
        vehicleName.text = _vehicles.name;
        vehicleDesc.text = _vehicles.description;

        if (vehicleContainer.childCount > 0)
            Destroy(vehicleContainer.GetChild(0).gameObject);

        Instantiate(_vehicles.vehiclePrefab, vehicleContainer.position, vehicleContainer.rotation, vehicleContainer);
    }
}
