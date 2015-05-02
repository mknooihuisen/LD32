﻿using UnityEngine;
using System.Collections;

/**
 * Superclass for all buildings
 */
public abstract class Building : MonoBehaviour {

	/** Who owns this building */
	public Lot lot;

	/** The building game object */
	public GameObject building;

	public void Demolish() {
		Object.Destroy (this);
	}

	public void Make(Vector3 location) {
		building = (GameObject) Instantiate (building, location, Quaternion.identity);
	}
}
