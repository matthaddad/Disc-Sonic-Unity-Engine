using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlignmentPlaneCollisionManager : MonoBehaviour
{
	AlignmentPlaneManager parent;

    void Start()
    {
        parent = transform.parent.GetComponent<AlignmentPlaneManager>();
    }

    void OnTriggerStay(Collider c) {
		parent.AlignObjectToPlane(c.gameObject);
    }
}
