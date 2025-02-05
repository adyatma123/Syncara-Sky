using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeliBladeRot : MonoBehaviour
{
    public float rotationSpeed = 10f; // Public variable to control rotation speed

    void Update()
    {
        // Rotate the object around the Y-axis continuously.
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        // Explanation of the code:

        // 1. `public float rotationSpeed = 10f;`
        //    - This declares a public variable named `rotationSpeed` of type `float`.  
        //    - `public` makes this variable accessible in the Unity Inspector, so you can easily adjust the speed without changing the code.
        //    - `10f` is the initial value of the rotation speed (degrees per second). The `f` indicates it's a float.

        // 2. `void Update()`
        //    - The `Update()` function is called every frame.  This is where you put code that needs to run repeatedly.

        // 3. `transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);`
        //    - `transform` refers to the Transform component of the GameObject this script is attached to.  The Transform component controls the object's position, rotation, and scale.
        //    - `Rotate()` is a method of the Transform component that rotates the object.
        //    - `Vector3.up` is a shorthand for `new Vector3(0, 1, 0)`. It represents the up direction (the Y-axis).  We're rotating around the Y-axis.
        //    - `rotationSpeed` is the speed of rotation in degrees per second.
        //    - `Time.deltaTime` is the time elapsed since the last frame.  Multiplying `rotationSpeed` by `Time.deltaTime` makes the rotation frame-rate independent.  This ensures the rotation speed is consistent regardless of how fast or slow the game is running.  Without `Time.deltaTime`, the rotation would appear faster on faster machines and slower on slower machines.

        // How to use this script:

        // 1. Create a new C# script in Unity (e.g., "RotateObject").
        // 2. Copy and paste this code into the script.
        // 3. Attach the script to the GameObject you want to rotate.
        // 4. In the Inspector for the GameObject, you'll see the `Rotation Speed` field.  Adjust the value to change the rotation speed.  A positive value rotates clockwise, and a negative value rotates counter-clockwise.
    }
}
