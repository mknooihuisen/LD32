﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/**
 * A site is like a town or village with lots to lease and people who live there to buy things and be employees
 */
public class Site {

	/** How often, in quarters, labor should be reallocated */
	private const int REALLOCATE = 1;

	/** The default number of employees at a new site */
	public const int DEFAULT_EMPLOYEES = 15;

	/** The default number of resource lots per site */
	public const int RESOURCE_COUNT = 3;

	/** This site's SitePlane */
	public GameObject SitePlane { get; private set; }

	/** This site's lots */
	public List<Lot> Lots { get; private set; }

	/** How many rows of lots there are */
	public int rows { get; private set; }

	/** How many columns of lots there are */
	public int cols { get; private set; }

	/** The Vector2 location of the lot currently being worked with */
	private Vector2 current;

	/** The name of the site */
	public string name { get; private set; }

	/** The list of the neighboring sites */
	public List<Site> neighbors { get; set; }

	/** How many employees live at this site */
	public int employees { get; set; }

	/**
	 * Creates a site
	 * 
	 * @param rows the number of rows of lots the site has
	 * @param cols the number of columns of lots the site has
	 * @param owner the business that owns the lots at this site
	 */
	public Site(int rows, int cols, Business owner) {
		name = RandomNameGenerator.generatePlaceName();
		Lots = new List<Lot>();
		current = new Vector2(0, 0);
		neighbors = new List<Site>();
		employees = DEFAULT_EMPLOYEES;
		this.rows = rows;
		this.cols = cols;

		GameObject go;
		go = (GameObject)Object.Instantiate(Resources.Load("SitePlane2"));
		TextMesh text = go.GetComponentInChildren<TextMesh>();
		text.text = name;
		SitePlane = go;
		current = new Vector2(-cols / 2.0f + 0.5f + go.transform.position.x, 
		                       -rows / 2.0f + 0.5f + go.transform.position.z);
		for (int i = 0; i < rows * cols; i++) {
			NewLot(owner);
		}
		MoveLots();

		AddLotResources();

		GameDirector.gameTime.performActionRepeatedly(REALLOCATE, () => {
			this.AllocateLabor();
			return true;});
	}

	/**
	 * Allocates labor to buildings based on the best wages
	 * 
	 * This occurs once every REALLOCATE quarters
	 */
	public void AllocateLabor() {
		List<Building> available = new List<Building>();
		foreach (Lot l in Lots) {
			if (l.Building != null && l.Building.laborCap > 0) {
				Building b = l.Building;
				this.employees += b.employees;
				b.employees = 0;
				available.Add(b);
			} else if (l.Building != null && l.Building.laborCap > 0) {
				l.Building.employees = 0;
			}
		}
		int attemptCount = 0;
		while (this.employees > 0 && available.Count > 0 && attemptCount < 1000) {
			int bestWage = 1; // Employees will not work for nothing
			List<Building> best = new List<Building>();
			foreach (Building b in available) {
				if (b.employeeWage > bestWage) {
					best.Clear();
					best.Add(b);
					bestWage = b.employeeWage;
				} else if (b.employeeWage == bestWage) {
					best.Add(b);
				}
			}
			if (best.Count == 0) {
				break;
			}
			if (best.Count == 1) {
				giveLabor(best [0]);
				available.Remove(best [0]);
			} else { 
				Debug.Log("Multiple bests!");
				while (this.employees > 0 && best.Count > 1) {
					for (int i = 0; i < best.Count; i++) {
						// If we've met the labor cap, remove this building from the list
						if (best [i].laborCap <= best [i].employees) {
							best.Remove(best [i]);
						} 

						// If there are any employees left, add one to this building
						else if (this.employees > 0) {
							best [i].employees++;
							this.employees--;
						}
					}
				}
				if (best.Count > 1) {
					giveLabor(best [0]);
					available.Remove(best [0]);
				}
			}
			attemptCount++;
		}
	}

	/**
	 * Gives labor to a building if the site has laborers and it can take the employees
	 * 
	 * @param building the building to give labor to
	 */
	private void giveLabor(Building building) {
		if (this.employees <= 0) {
			return;
		}
		int needs = building.laborCap - building.employees;
		if (this.employees >= needs) {
			this.employees -= needs;
			building.employees += needs;

		} else {
			building.employees += this.employees;
			this.employees = 0;
		}
	}

	/**
	 * Creates a new lot on this site given an owner
	 * 
	 * @owner sets the owner of the site
	 * @return the lot that is created
	 */
	private Lot NewLot(Business owner) {
		Lot newLot = new Lot(this, owner, new Vector3(current.x * 10.1f, 0.1f, current.y * 10.1f), SitePlane.transform.rotation);
		Lots.Add(newLot);

		return newLot;
	}

	/**
	 * Moves lots to their correct possition
	 */
	private void MoveLots() {
		// The current position of the next lot
		current = new Vector2(-cols / 2.0f + 0.5f, -rows / 2.0f + 0.5f);
		foreach (Lot lot in Lots) {
			if (current.x > (cols / 2.0f)) {
				current.x = -cols / 2.0f + 0.5f;
				current.y++;
			}

			lot.RepositionLotPlane(new Vector3(current.x * 10.1f + SitePlane.transform.position.x, 
			            SitePlane.transform.position.y + 1.0f, 
					    current.y * 10.1f + SitePlane.transform.position.z)
			);

			current.x++;
		}
	}

	/**
	 * Adds resources to lots
	 */
	private void AddLotResources() {
		List<Resource> resources = ResourceExtensions.RandomResources(RESOURCE_COUNT);
		List<Lot> lots = ResourceExtensions.ChooseRandom(Lots.ToArray(), RESOURCE_COUNT);

		for (int i = 0; i < RESOURCE_COUNT; i++) {
			lots [i].Resource = resources [i];
		}
	}

	/**
	 * Places the site at the given location and moves its lots with it
	 * 
	 * @param location the location to place the site at
	 */
	public void placeSite(Vector3 location) {
		SitePlane.transform.position = location;
		MoveLots();
	}

	/**
	 * Gets the location of the site plane
	 * 
	 * @return the Vector3 location of the SitePlane
	 */
	public Vector3 getPlaneLocation() {
		return SitePlane.transform.position;
	}
}