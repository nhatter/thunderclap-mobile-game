using UnityEngine;
using System.Collections;

public class Flashy : MonoBehaviour {
	public GUISkin skin;
	public Texture2D flashTexture;

	public bool isTutorialMode = false;
	public bool isDisplayingHealthWarning = true;

	public float flashInTime = 0.25f;
	public float flashOutTime = 0.5f;
	public float minTimeBetweenFlash;
	public float maxTimeBetweenFlash;
	public float reactionLeeway = 0.1f;

	public Texture2D flashyIcon;
	public Texture2D umbrellaIcon;

	public string[] congratulatoryPhrases;
	string congratulatoryPhrase;

	public AudioClip zapSound;
	public AudioClip caughtSound;

	float flashTimer;
	float timeToFlash;

	bool isFadingIn = false;
	bool isFlashCaught = false;
	bool isTouchReleased = true;
	bool gameOver = false;
	bool savedByUmbrella = false;
	bool isVibrating = false;

	float vibrateTimer = 0;
	public float timeToVibrate = 1.0f;

	int dodgeCount = 0;
	int umbrellaCount = 1;


	GUIContent counterDisplay = new GUIContent();
	GUIContent umbrellaDisplay = new GUIContent();

	Rect ENTIRE_SCREEN = new Rect(0, 0, Screen.width, Screen.height);
	Rect CENTER_SCREEN = new Rect(Screen.width/4 - 75, Screen.height/2 - 100, Screen.width/2 + 150, 400);
	Rect TOP_RIGHT_SCREEN = new Rect(Screen.width - 100, 0, 100, 125);

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
		if(!gameOver && !isTutorialMode) {
			// Now, assuming it isn't game over...
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
							audio.PlayOneShot(zapSound);
							isVibrating = true;
							Debug.Log("Game over by early/late touch");
						}

						isTouchReleased = false;
					}
				}
			}

			if(isFadingIn) {
				Debug.Log("Fade in !!!");
			}
			if(isFadingIn) {
				if(isTouchReleased && !isFlashCaught && flashTimer >= timeToFlash && flashTimer <= timeToFlash+flashInTime+reactionLeeway && (Input.touchCount > 0 || Input.GetMouseButton(0))) {
					dodgeCount++;

					audio.PlayOneShot(caughtSound);

					if(dodgeCount % 5 == 0) {
						congratulatoryPhrase = congratulatoryPhrases[Mathf.RoundToInt(Random.value*(congratulatoryPhrases.Length-1))];
					}

					counterDisplay.text = ""+dodgeCount;
					isFlashCaught = true;
					isTouchReleased = false;
				}
			

				if(flashTimer > timeToFlash+flashInTime+reactionLeeway && !isFlashCaught && !gameOver) {
					Debug.Log("DEBUG");
					if(umbrellaCount > 0) {
						savedByUmbrella = true;
						umbrellaCount--;
						umbrellaDisplay.text = ""+umbrellaCount;
						isFlashCaught = true;
					} else {
						dodgeCount = 0;
						gameOver = true;
						counterDisplay.text = ""+dodgeCount;
						audio.PlayOneShot(zapSound);
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
	}


	void calculateTimeToFlash() {
		timeToFlash = minTimeBetweenFlash + Random.value*maxTimeBetweenFlash;
	}

	void fadeOutFlash() {
		iTween.CameraFadeTo(iTween.Hash("amount", 0.0f, "time", flashOutTime, "oncompletetarget", this.gameObject, "oncomplete", "initFlash"));
	}

	public void initFlash() {
		isFlashCaught = false;
		isFadingIn = false;
		flashTimer = 0;
		calculateTimeToFlash();
	}

	void OnGUI() {
		GUI.skin = skin;

		if(isTutorialMode) {
			GUILayout.BeginArea(ENTIRE_SCREEN);
			if(isDisplayingHealthWarning) {
				GUILayout.Label("HEALTH WARNING\n\nTHIS GAME CONTAINS FLASHING LIGHTS WHICH MAY INDUCE EPILEPTIC SEIZURES.");

				if(GUILayout.Button("OK")) {
					isDisplayingHealthWarning = false;
				}
			} else {

				GUILayout.Label("THUNDERCLAP\n\nONLY TOUCH THE SCREEN WHEN IT FLASHES WHITE. THIS IS A HARD GAME.");
					
				if(GUILayout.Button("PLAY")) {
					isTutorialMode = false;
					isTouchReleased = false;
				}
			}
			GUILayout.EndArea();
		} else {
			GUILayout.Label(counterDisplay);
			GUI.Label(TOP_RIGHT_SCREEN, umbrellaDisplay);
		}


		if(gameOver && !isTutorialMode) {
			GUILayout.BeginArea(CENTER_SCREEN);
				GUILayout.Label("GAME OVER");
				
					if(GUILayout.Button("RETRY")) {
						initFlash();

						// Important: start game loop by assuming button pressed
						// to avoid false positive
						isTouchReleased = false;

						savedByUmbrella = false;

						// Active main game loop
						gameOver = false;

						Debug.Log("Retry pressed");
					}

					if(GUILayout.Button("BUY UMBRELLAS")) {
					}
				
			GUILayout.EndArea();
		} else {
			if(savedByUmbrella) {
				GUI.Label (CENTER_SCREEN, "PHEW, THAT WAS CLOSE!");
			} else {
				if(dodgeCount == 1) {
					GUI.Label(CENTER_SCREEN, "YOU'VE GOT IT!");
				}

				if(dodgeCount%5 == 0 && dodgeCount != 0) {
					GUI.Label(CENTER_SCREEN, congratulatoryPhrase);
				}
			}
		}


	}

}
