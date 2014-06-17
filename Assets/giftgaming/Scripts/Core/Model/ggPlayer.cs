using UnityEngine;
using System.Collections;
using System;
using System.Xml; 
using System.Xml.Serialization; 

[Serializable]
public class ggPlayer {
	public int playerID;
	public bool hasGivenPreferences;
	public bool hasGivenGeoConsent;
	public bool hasBeenOfferedGeoConsent;

	public ggPlayer() {
	}

	public ggPlayer(int playerID) {
		this.playerID = playerID;
	}
}
