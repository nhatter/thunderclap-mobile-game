﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

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
	public float gameOverDelayTime = 0.5f;
	float gameOverDelayTimer = 0;
	bool isShowingGameOverMenu = false;

	public Texture2D flashyIcon;
	public Texture2D umbrellaIcon;
	public Texture2D thunderclapLogo;

	public string[] congratulatoryPhrases;
	string congratulatoryPhrase;

	public AudioClip zapSound;
	public AudioClip caughtSound;
	public AudioClip music;
	public AudioClip highScoreSound;

	float flashTimer;
	float timeToFlash;

	bool isFadingIn = false;
	bool isFlashCaught = false;
	bool isTouchReleased = true;
	bool gameOver = false;
	bool hasSavedGameState = false;
	bool savedByUmbrella = false;
	bool isVibrating = false;

	float vibrateTimer = 0;
	public float timeToVibrate = 1.0f;

	int dodgeCount = 0;
	bool isNewHighScore = false;

	GUIContent counterDisplay = new GUIContent();
	GUIContent umbrellaDisplay = new GUIContent();

	Rect ENTIRE_SCREEN = new Rect(0, 0, Screen.width, Screen.height);
	Rect CENTER_SCREEN = new Rect(25, 100, Screen.width - 50, Screen.height - 25);
	Rect CENTER_SCREEN_MESSAGE = new Rect(Screen.width/4 - 75, Screen.height/2 - 100, Screen.width/2 + 150, 400);

	Rect TOP_LEFT_SCREEN = new Rect(25, 0, 150, 125);
	Rect TOP_RIGHT_SCREEN = new Rect(Screen.width - 125, 0, 150, 125);

	IAPManagerObject iap;
	bool isIAPEnabled = false;

	Player player = new Player();

	string DATA_PATH;
	string PLAYER_XML_FILE;

	public GUISkin facebookSkin;
	FacebookIntegration fb;
	bool showShareDialog = false;
	public Texture2D shareDialogBackground;
	Rect shareButtonRect;
	Rect shareDialogRect;
	Rect cancelShareButtonRect;
	string FB_COMMENT_PLACEHOLDER_TEXT = "Say something about this...";
	string screenshotComment;
	Rect screenshotTextAreaRect = new Rect(23, 100, 554, 96);
	Rect screenshotPreviewRect;
	float screenRatio = (float) Screen.height / (float) Screen.width;

	// Use this for initialization
	void Start () {
		iTween.CameraFadeAdd(flashTexture);
		calculateTimeToFlash();

		counterDisplay.image = flashyIcon;
		counterDisplay.text = ""+dodgeCount;

		umbrellaDisplay.image = umbrellaIcon;
		umbrellaDisplay.text = ""+player.umbrellaCount;

		audio.clip = music;
		audio.Play();
		audio.loop = true;

		#if !UNITY_EDITOR
		iap = (new GameObject("PassViewObject")).AddComponent<IAPManagerObject>();
		iap.Init();
		iap.CanMakePurchases();
		#endif

		DATA_PATH = Application.persistentDataPath+"/";
		PLAYER_XML_FILE = DATA_PATH + "player.xml";
		Debug.Log("Player file is stored at :"+ PLAYER_XML_FILE);
		loadPlayer();

		fb = this.gameObject.AddComponent<FacebookIntegration>();
		shareButtonRect = new Rect(shareDialogBackground.width - facebookSkin.GetStyle("Share").fixedWidth, 5, facebookSkin.GetStyle("Share").fixedWidth, facebookSkin.GetStyle("Share").fixedHeight);
		shareDialogRect = new Rect((Screen.width - shareDialogBackground.width)/2, 23, shareDialogBackground.width, shareDialogBackground.height);
		cancelShareButtonRect = new Rect((Screen.width - shareDialogBackground.width)/2 - 4, 4, facebookSkin.GetStyle("Cancel").fixedWidth, facebookSkin.GetStyle("Cancel").fixedHeight);
		screenshotComment = FB_COMMENT_PLACEHOLDER_TEXT;
		screenshotPreviewRect = new Rect (23, 225, 400, 400 * screenRatio);
	}

	void loadPlayer() {
		if(File.Exists(PLAYER_XML_FILE)) {
			try {
				player = XMLManager.load<Player>(PLAYER_XML_FILE);
			} catch (Exception e) {
				Debug.Log("Error parsing giftgaming Player file " + e);
			}
		}

		// Update GUI
		umbrellaDisplay.text = ""+player.umbrellaCount;
	}

	void savePlayer() {
		try {
			XMLManager.save<Player>(player, PLAYER_XML_FILE);
		} catch (Exception e) {
			Debug.Log("Error parsing giftgaming Player file " + e);
		}
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
						if(player.umbrellaCount > 0) {
							savedByUmbrella = true;
							player.umbrellaCount--;
							umbrellaDisplay.text = ""+player.umbrellaCount;

						} else {
							flashTimer = 0;
							checkForHighScore();

							gameOver = true;
							counterDisplay.text = ""+dodgeCount;


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
				if(isTouchReleased && !isFlashCaught && flashTimer >= timeToFlash && flashTimer <= timeToFlash+flashInTime+flashOutTime+player.reactionLeeway && (Input.touchCount > 0 || Input.GetMouseButton(0))) {
					dodgeCount++;

					audio.PlayOneShot(caughtSound);

					if(dodgeCount % 5 == 0) {
						congratulatoryPhrase = congratulatoryPhrases[Mathf.RoundToInt(UnityEngine.Random.value*(congratulatoryPhrases.Length-1))];
					}

					counterDisplay.text = ""+dodgeCount;
					isFlashCaught = true;
					isTouchReleased = false;
				}
			

				if(flashTimer > timeToFlash+flashInTime+flashOutTime+player.reactionLeeway && !isFlashCaught && !gameOver) {
					Debug.Log("DEBUG");
					if(player.umbrellaCount > 0) {
						savedByUmbrella = true;
						player.umbrellaCount--;
						umbrellaDisplay.text = ""+player.umbrellaCount;
						isFlashCaught = true;
					} else {
						checkForHighScore();

						gameOver = true;
						counterDisplay.text = ""+dodgeCount;

						Debug.Log("Missed the flash");
					}
				}

			}

			flashTimer += Time.deltaTime;
		}

		if(gameOver) { 
			gameOverDelayTimer += Time.deltaTime;

			if(!hasSavedGameState) {
				savePlayer();
				hasSavedGameState = true;
			}
			
			if(gameOverDelayTimer >= gameOverDelayTime) {
				isShowingGameOverMenu = true;
				gameOverDelayTimer = 0;
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
	}

	void checkForHighScore() {
		if(dodgeCount > player.bestScore) {
			player.bestScore = dodgeCount;
			isNewHighScore = true;
			audio.PlayOneShot(highScoreSound);
			
			if (FB.IsLoggedIn) {
				var query = new Dictionary<string, string>();
				query["score"] = ""+player.bestScore;
				FB.API("/me/scores", Facebook.HttpMethod.POST, delegate(FBResult r) { Debug.Log("Result: " + r.Text); }, query);
			}
		} else {
			audio.PlayOneShot(zapSound);
		}
	}
	
	
	void calculateTimeToFlash() {
		timeToFlash = minTimeBetweenFlash + UnityEngine.Random.value*maxTimeBetweenFlash;
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
							isTouchReleased = false;
							audio.Stop();
						}
			break;

			case MenuScreenMode.MAIN_MENU:

					GUILayout.BeginArea(CENTER_SCREEN);
						
						
						GUILayout.BeginVertical();
						GUILayout.Label(thunderclapLogo);
						GUILayout.Space(25);
			
						if(GUILayout.Button("PLAY")) {
							menuScreenMode = MenuScreenMode.GAME;
							isTouchReleased = false;
							audio.Stop();
						}

						if(GUILayout.Button("f CONNECT")) {
							fb.CallFBInit();
						}

						if(GUILayout.Button("HOW TO PLAY")) {
							menuScreenMode = MenuScreenMode.HOW_TO_PLAY;
						}

						if(GUILayout.Button("CREDITS")) {
							menuScreenMode = MenuScreenMode.CREDITS;
						}
						GUILayout.EndVertical();
			
					GUILayout.EndArea();
			break;
		}// End of GUI switch statement

			
		if(menuScreenMode == MenuScreenMode.GAME) {
			GUI.Label(TOP_LEFT_SCREEN, counterDisplay);
			GUI.Label(TOP_RIGHT_SCREEN, umbrellaDisplay);

			if(gameOver) {
				GUILayout.BeginArea(CENTER_SCREEN);
					if(isNewHighScore) {

						  
					
						GUILayout.Label("NEW HIGH SCORE: "+dodgeCount);
						GUILayout.Label("OMG.");
						if(GUILayout.Button("SHARE SCREENSHOT")) {
							fb.getScreenshot(delegate() { showShareDialog = true; });
						}
					} else {
						GUILayout.Label("GAME OVER");
						GUILayout.Space(20);
					
				   		GUILayout.Label("SCORE: "+dodgeCount);
						GUILayout.Label("   BEST: "+player.bestScore);
					}
					

						if(isTouchReleased && isShowingGameOverMenu) {
							GUI.enabled = true;
						} else {
							GUI.enabled = false;
						}

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
							hasSavedGameState = false;
							isShowingGameOverMenu = false;
							isNewHighScore = false;	
						
							Debug.Log("Retry pressed");
						}

						if(isTouchReleased && isShowingGameOverMenu && isIAPEnabled) {
							GUI.enabled = true;
						} else {
							GUI.enabled = false;
						}

						#if !UNITY_EDITOR
						if(GUILayout.Button("BUY UMBRELLAS")) {
							iap.Purchase("3_UMBRELLAS");
						}

						if(GUILayout.Button("BUY CARRY ON")) {
							iap.Purchase("CARRY_ON");
						}

						if(GUILayout.Button("BUY MORE TIME")) {
							iap.Purchase("MORE_TIME");
						}

						#endif

					if(isTouchReleased && isShowingGameOverMenu) {
						GUI.enabled = true;
					} else {
						GUI.enabled = false;
					}

					if(GUILayout.Button("MAIN MENU")) {
						menuScreenMode = MenuScreenMode.MAIN_MENU;
					}

					GUILayout.EndArea();
					GUI.enabled = true;

					GUI.skin = facebookSkin;
					if(showShareDialog) {
						GUI.Box(ENTIRE_SCREEN, "", "Overlay");
						GUI.BeginGroup(shareDialogRect, shareDialogBackground);

							GUI.SetNextControlName("fbComment");
							screenshotComment = GUI.TextArea(screenshotTextAreaRect, screenshotComment);

							if(GUI.GetNameOfFocusedControl() == "fbComment" && screenshotComment == FB_COMMENT_PLACEHOLDER_TEXT) {
								screenshotComment = "";
							}
						
							if(GUI.Button(shareButtonRect, "", "Share")) {
								fb.shareScreenshot(screenshotComment, delegate() { showShareDialog = false; });
							}

							GUI.Box(screenshotPreviewRect, fb.sharingScreenshot);
					    GUI.EndGroup();

						if(GUI.Button(cancelShareButtonRect, "", "Cancel")) {
							showShareDialog = false;
						}

						GUI.enabled = false;
					}
					
					GUI.skin = skin;
				

			} else {
				if(savedByUmbrella) {
					GUI.Label (CENTER_SCREEN_MESSAGE, "SAVED BY UMBRELLA!");
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

	public void unlockIAP(string productID) {
		switch(productID) {
			case "3_UMBRELLAS":
				player.umbrellaCount+= 3;
				umbrellaDisplay.text = ""+player.umbrellaCount;
				savePlayer();
			break;

			case "CARRY_ON":
				// Important: start game loop by assuming button pressed
				// to avoid false positive
				isTouchReleased = false;
				
				savedByUmbrella = false;
				
				// Active main game loop
				gameOver = false;
				hasSavedGameState = false;
				isShowingGameOverMenu = false;
				isNewHighScore = false;
			break;

			case "EXTRA_TIME":
				player.reactionLeeway = 0.1f;
				savePlayer();
			break;
		}
	}

	public void canMakePurchases(string iOSResult) {
		switch(iOSResult) {
			case "true":
				isIAPEnabled = true;
			break;
		}
	}

}
