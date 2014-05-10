using UnityEngine;
using System.Collections;

public class Flashy : MonoBehaviour {
	public enum MenuScreenMode {HEALTH_WARNING, MAIN_MENU, HOW_TO_PLAY, SHOP, CREDITS, ACHIEVEMENTS, GAME};
	public MenuScreenMode menuScreenMode = MenuScreenMode.HEALTH_WARNING;

	public GUISkin skin;
	public Texture2D flashTexture;

	public bool isTutorialMode = false;
	public bool isDisplayingHealthWarning = true;
	public bool isDisplayingCredits = false;

	public float flashInTime = 0.25f;
	public float flashOutTime = 0.5f;
	public float minTimeBetweenFlash;
	public float maxTimeBetweenFlash;
	public float reactionLeeway = 0.1f;

	public Texture2D flashyIcon;
	public Texture2D umbrellaIcon;
	public Texture2D thunderclapLogo;

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
	
	int bestScore = 0;


	GUIContent counterDisplay = new GUIContent();
	GUIContent umbrellaDisplay = new GUIContent();

	Rect ENTIRE_SCREEN = new Rect(0, 0, Screen.width, Screen.height);
	Rect CENTER_SCREEN = new Rect(Screen.width/4 - 75, Screen.height/2 - 300, Screen.width/2 + 150, 600);
	Rect CENTER_SCREEN_MESSAGE = new Rect(Screen.width/4 - 75, Screen.height/2 - 100, Screen.width/2 + 150, 400);

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
		if(!gameOver && menuScreenMode == MenuScreenMode.GAME) {
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
							if(dodgeCount > bestScore) {
								bestScore = dodgeCount;
							}

							gameOver = true;
							counterDisplay.text = ""+dodgeCount;
							audio.PlayOneShot(zapSound);

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
						if(dodgeCount > bestScore) {
							bestScore = dodgeCount;
						}
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

		//GUILayout.BeginArea(ENTIRE_SCREEN);
		switch(menuScreenMode) {
		
			case MenuScreenMode.CREDITS:
				if(GUILayout.Button("BACK")) {
					menuScreenMode = MenuScreenMode.MAIN_MENU;
				}

				GUILayout.BeginVertical();

				GUILayout.Space(20);
				GUILayout.Label ("\"Woo-sh\" GAME OVER SOUND");
				GUILayout.Label ("woosh_02.wav by Glaneur de sons (http://www.freesound.org/people/Glaneur%20de%20sons/sounds/34172/)");
				GUILayout.Label ("Licensed under Creative Commons Share Alike 3.0 (http://creativecommons.org/licenses/by/3.0/)");
				GUILayout.Space(20);
				GUILayout.Label("SPECIAL THANKS");
				GUILayout.Label("Adam James (Meownoodle on YouTube)");
				GUILayout.Label("Robert Streeting (robsws.co.uk)");
				GUILayout.Label("Charles Payne (business-aspirations.com)");
				GUILayout.Label("Mark Salvin (marksalvin.com)");
				GUILayout.Label("Cate (AntiDoge5Life on Facebook)");
				GUILayout.EndVertical();
			break;
		
			case MenuScreenMode.HEALTH_WARNING:
				GUILayout.Label("HEALTH WARNING\n\nTHIS GAME CONTAINS FLASHING LIGHTS WHICH MAY INDUCE EPILEPTIC SEIZURES.");

				if(GUILayout.Button("OK")) {
					menuScreenMode = MenuScreenMode.MAIN_MENU;
				}
			break;

			case MenuScreenMode.HOW_TO_PLAY:
						GUILayout.Label("ONLY TOUCH THE SCREEN WHEN IT FLASHES WHITE.\n\nUMBRELLAS ALLOW YOU TO MAKE A MISTAKE.\n\nTHIS IS A HARD GAME AND TAKES PRACTICE.");
						if(GUILayout.Button("PLAY")) {
							menuScreenMode = MenuScreenMode.GAME;
						}
			break;

			case MenuScreenMode.MAIN_MENU:

					GUI.BeginGroup(CENTER_SCREEN);
						GUILayout.Label(thunderclapLogo);
						GUILayout.Space(25);
						if(GUILayout.Button("PLAY")) {
							menuScreenMode = MenuScreenMode.GAME;
							isTouchReleased = false;
						}

						if(GUILayout.Button("HOW TO PLAY")) {
							menuScreenMode = MenuScreenMode.HOW_TO_PLAY;
						}

						if(GUILayout.Button("CREDITS")) {
							menuScreenMode = MenuScreenMode.CREDITS;
						}
					GUI.EndGroup();
			break;
		}// End of GUI switch statement

		//GUILayout.EndArea();
			
		if(menuScreenMode == MenuScreenMode.GAME) {
			GUILayout.Label(counterDisplay);
			GUI.Label(TOP_RIGHT_SCREEN, umbrellaDisplay);

			if(gameOver) {
				GUILayout.BeginArea(CENTER_SCREEN);
					GUILayout.Label("GAME OVER");
					GUILayout.Space(20);
					GUILayout.Label("SCORE: "+dodgeCount);
					GUILayout.Label("   BEST: "+bestScore);
					
						if(GUILayout.Button("RETRY")) {
							initFlash();

							// Reset score
							dodgeCount = 0;
							counterDisplay.text = ""+dodgeCount;

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
					GUI.Label (CENTER_SCREEN_MESSAGE, "PHEW, THAT WAS CLOSE!");
				} else {
					if(dodgeCount == 0) {
						GUI.Label(CENTER_SCREEN_MESSAGE, "WAIT FOR IT...");
					}

					if(dodgeCount == 1) {
						GUI.Label(CENTER_SCREEN_MESSAGE, "YOU'VE GOT IT!");
					}

					if(dodgeCount%5 == 0 && dodgeCount != 0) {
						GUI.Label(CENTER_SCREEN_MESSAGE, congratulatoryPhrase);
					}
				}
			}
		}
	}

}
