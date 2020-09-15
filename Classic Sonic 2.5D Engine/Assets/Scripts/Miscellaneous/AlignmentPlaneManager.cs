using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlignmentPlaneManager : MonoBehaviour
{
	[Header("Parameters")]
	public float alignmentSpeed = 0.0f;
	public bool isDynamic = false;

	Vector3 planeUpVector;
    Vector3 planeRightVector;
    Plane plane;

    void Start() {
        UpdatePlane();
    }

    void Update() {
        if(isDynamic) {
        	UpdatePlane();
        }
    }

    void UpdatePlane() {
        Quaternion rotation = Quaternion.Euler(
            transform.rotation.eulerAngles.x + 90.0f,
            transform.rotation.eulerAngles.y,
            transform.rotation.eulerAngles.z
        );

        planeUpVector = rotation * Vector3.up;
        planeRightVector = rotation * Vector3.right;

        plane = new Plane(Vector3.Cross(planeUpVector, planeRightVector), transform.position);
    }

    public void AlignObjectToPlane(GameObject obj) {
        if(obj.tag == "Object AP Collision") {
            PlayerController controller = obj.transform.parent.GetComponent<PlayerController>();
            controller.SetPlane(planeUpVector, planeRightVector, position: transform.position, alignmentSpeed: alignmentSpeed);
        }
    }
}
