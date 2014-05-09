using UnityEngine;
using System.Collections;

public class Flashy : MonoBehaviour {
	public GUISkin skin;
	public Texture2D flashTexture;
	public float flashInTime = 0.25f;
	public float flashOutTime = 0.5f;
	public float minTimeBetweenFlash;
	public float maxTimeBetweenFlash;
	public float reactionLeeway = 0.1f;

	public Texture2D flashyIcon;

	float flashTimer;
	float timeToFlash;

	bool isFadingIn = false;
	bool isFlashCaught = false;

	int dodgeCount = 0;

	GUIContent counterDisplay = new GUIContent();

	// Use this for initialization
	void Start () {
		iTween.CameraFadeAdd(flashTexture);
		calculateTimeToFlash();

		counterDisplay.image = flashyIcon;
		counterDisplay.text = ""+dodgeCount;
	}
	
	// Update is called once per frame
	void Update () {
		if(!isFadingIn) {
			if(flashTimer > timeToFlash) {
				iTween.CameraFadeTo(iTween.Hash("amount", 1.0f, "time", flashInTime, "oncompletetarget", this.gameObject, "oncomplete", "fadeOutFlash"));
				isFadingIn = true;
			} else {
				if(Input.touchCount > 0 || Input.GetMouseButton(0)) {
					flashTimer = 0;
					dodgeCount = 0;
					counterDisplay.text = ""+dodgeCount;
				}
			}
		}

		if(isFadingIn) {
			if(!isFlashCaught && flashTimer >= timeToFlash && flashTimer <= timeToFlash+flashInTime+reactionLeeway && (Input.touchCount > 0 || Input.GetMouseButton(0))) {
				dodgeCount++;
				counterDisplay.text = ""+dodgeCount;
				isFlashCaught = true;
			}

			if(flashTimer > timeToFlash+flashInTime+reactionLeeway && !isFlashCaught) {
				dodgeCount = 0;
				counterDisplay.text = ""+dodgeCount;
				Debug.Log("Missed the flash");
			}

		}

		flashTimer += Time.deltaTime;
	}


	void calculateTimeToFlash() {
		timeToFlash = minTimeBetweenFlash + Random.value*maxTimeBetweenFlash;
	}

	void fadeOutFlash() {
		iTween.CameraFadeTo(iTween.Hash("amount", 0.0f, "time", flashOutTime, "oncompletetarget", this.gameObject, "oncomplete", "completeFlash"));
	}

	void completeFlash() {
		isFlashCaught = false;
		isFadingIn = false;
		flashTimer = 0;
		calculateTimeToFlash();
	}

	void OnGUI() {
		GUI.skin = skin;
		GUILayout.Label(counterDisplay);
	}

}
