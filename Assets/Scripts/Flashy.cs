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
	public Texture2D umbrellaIcon;

	public string[] congratulatoryPhrases;
	string congratulatoryPhrase;



	float flashTimer;
	float timeToFlash;

	bool isFadingIn = false;
	bool isFlashCaught = false;
	bool isTouchReleased = true;
	bool gameOver = false;
	bool savedByUmbrella = false;

	int dodgeCount = 0;
	int umbrellaCount = 1;


	GUIContent counterDisplay = new GUIContent();
	GUIContent umbrellaDisplay = new GUIContent();

	Rect CENTER_SCREEN = new Rect(Screen.width/4, Screen.height/2, Screen.width/2, 300);
	Rect TOP_RIGHT_SCREEN = new Rect(Screen.width - 75, 0, 100, 100);

	// Use this for initialization
	void Start () {
		iTween.CameraFadeAdd(flashTexture);
		calculateTimeToFlash();

		counterDisplay.image = flashyIcon;
		counterDisplay.text = ""+dodgeCount;

		umbrellaDisplay.image = umbrellaIcon;
		umbrellaDisplay.text = ""+umbrellaCount;
	}
	
	// Update is called once per frame
	void Update () {
		if(!isFadingIn && !isFlashCaught && !gameOver) {
			if(flashTimer > timeToFlash) {
				iTween.CameraFadeTo(iTween.Hash("amount", 1.0f, "time", flashInTime, "oncompletetarget", this.gameObject, "oncomplete", "fadeOutFlash"));
				isFadingIn = true;
				savedByUmbrella = false;
			} else {
				if(isTouchReleased && (Input.touchCount > 0 || Input.GetMouseButton(0))) {
					if(umbrellaCount > 0 && !savedByUmbrella) {
						savedByUmbrella = true;
						umbrellaCount--;
						umbrellaDisplay.text = ""+umbrellaCount;
					} else {
						flashTimer = 0;
						dodgeCount = 0;
						gameOver = true;
						counterDisplay.text = ""+dodgeCount;

					}

					isTouchReleased = false;
				}
			}
		}

		if(isFadingIn) {
			if(isTouchReleased && !isFlashCaught && flashTimer >= timeToFlash && flashTimer <= timeToFlash+flashInTime+reactionLeeway && (Input.touchCount > 0 || Input.GetMouseButton(0))) {
				dodgeCount++;

				if(dodgeCount % 5 == 0) {
					congratulatoryPhrase = congratulatoryPhrases[Mathf.RoundToInt(Random.value*(congratulatoryPhrases.Length-1))];
				}

				counterDisplay.text = ""+dodgeCount;
				isFlashCaught = true;
				isTouchReleased = false;
			}


			if(flashTimer > timeToFlash+flashInTime+reactionLeeway && !isFlashCaught) {
				if(umbrellaCount > 0) {
					savedByUmbrella = true;
					umbrellaCount--;
					umbrellaDisplay.text = ""+umbrellaCount;
					isFlashCaught = true;
				} else {
					dodgeCount = 0;
					gameOver = true;
					counterDisplay.text = ""+dodgeCount;
					Debug.Log("Missed the flash");
				}
			}

		}

		#if UNITY_EDITOR
		if(Input.GetMouseButtonUp(0)) {
			isTouchReleased = true;
			Debug.Log("Touch released");
		}
		#else
		if(Input.touchCount == 0) {
			isTouchReleased = true;
		}
		#endif

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

		if(dodgeCount%5 == 0 && dodgeCount != 0) {
			GUI.Label(CENTER_SCREEN, congratulatoryPhrase);
		}

		if(gameOver) {
			GUILayout.BeginArea(CENTER_SCREEN);
				GUILayout.Label("GAME OVER", GUILayout.Width(Screen.width/2));
				
					if(GUILayout.Button("RETRY", GUILayout.Width(Screen.width/2))) {
						gameOver = false;
					}

					if(GUILayout.Button("BUY UMBRELLAS", GUILayout.Width(Screen.width/2))) {
					}
				
			GUILayout.EndArea();
		}

		GUI.Label(TOP_RIGHT_SCREEN, umbrellaDisplay);

		if(savedByUmbrella) {
			GUI.Label (CENTER_SCREEN, "PHEW, THAT WAS CLOSE!");
		}
	}

}
