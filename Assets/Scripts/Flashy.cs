using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class Flashy : MonoBehaviour {
	public enum MenuScreenMode {HEALTH_WARNING, MAIN_MENU, HOW_TO_PLAY, SHOP, CREDITS, ACHIEVEMENTS, GAME, FRIEND_SCORES, LEVEL_SELECT};
	public MenuScreenMode menuScreenMode = MenuScreenMode.HEALTH_WARNING;
	public int POINTS_TO_PASS_LEVEL = 10;

	public GUISkin skin;
	public GUISkin creditsSkin;
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
	float reactionTime;

	bool isFadingIn = false;
	bool isFlashCaught = false;
	bool isTouchReleased = true;
	bool gameOver = false;
	bool willBeGameOver = false;
	bool hasSavedGameState = false;
	bool savedByUmbrella = false;
	bool isVibrating = false;
	bool isTraining = false;

	float vibrateTimer = 0;
	public float timeToVibrate = 1.0f;

	int dodgeCount = 0;
	int attemptingLevel = 0;
	bool levelPassed = false;
	float levelPassedTimer = 0;
	public float levelPassedTime = 3.0f;
	bool isNewHighScore = false;
	float highScoreTimer = 0;
	public float highScoreTime = 2.0f;
	public Texture highScoreImage;

	public float keepCalmTime = 3.0f;
	float keepCalmTimer = 0f;
	bool keepCalm = false;

	GUIContent counterDisplay = new GUIContent();
	GUIContent umbrellaDisplay = new GUIContent();

	Rect ENTIRE_SCREEN = new Rect(0, 0, Screen.width, Screen.height);
	Rect CENTER_SCREEN = new Rect(25, 100, Screen.width - 50, Screen.height - 25);
	Rect CENTER_SCREEN_MESSAGE = new Rect(Screen.width/4 - 75, Screen.height/2 - 100, Screen.width/2 + 150, 400);

	Rect TOP_LEFT_SCREEN = new Rect(25, 0, 150, 125);
	Rect TOP_RIGHT_SCREEN = new Rect(Screen.width - 125, 0, 150, 125);

	IAPManagerObject iap;
	#if UNITY_EDITOR
		bool isIAPEnabled = true;
	#else
		bool isIAPEnabled = false;
	#endif


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
	string FB_COMMENT_PLACEHOLDER_TEXT = "#thundrclap ";
	string screenshotComment;
	Rect screenshotTextAreaRect = new Rect(23, 100, 547, 96);
	Rect screenshotPreviewRect;
	float screenRatio = (float) Screen.height / (float) Screen.width;
	GUIContent fbConnectButton;
	public Texture2D fbConnectIcon;

	Vector2 creditScrollPosition = new Vector2(0,0);

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
		shareButtonRect = new Rect(shareDialogBackground.width - facebookSkin.GetStyle("Share").fixedWidth - 5, 12, facebookSkin.GetStyle("Share").fixedWidth, facebookSkin.GetStyle("Share").fixedHeight);
		shareDialogRect = new Rect((Screen.width - shareDialogBackground.width)/2, 23, shareDialogBackground.width, shareDialogBackground.height);
		cancelShareButtonRect = new Rect((Screen.width - shareDialogBackground.width)/2 - 4, 4, facebookSkin.GetStyle("Cancel").fixedWidth, facebookSkin.GetStyle("Cancel").fixedHeight);
		screenshotComment = FB_COMMENT_PLACEHOLDER_TEXT;
		screenshotPreviewRect = new Rect (23, 225, 400, 400 * screenRatio);
		fbConnectButton = new GUIContent(" CONNECT", fbConnectIcon);

		fb.CallFBInit();
		fbIndicateConnected();
		fb.loginCallback = delegate { fbIndicateConnected(); };
		fb.player = player;
	}

	void fbIndicateConnected() {
		if(FB.IsLoggedIn) {
			fbConnectButton.text = " CONNECTED ✓";
		}
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
		if(isTraining && dodgeCount >= POINTS_TO_PASS_LEVEL && !levelPassed) {
			if(player.level < attemptingLevel) {
				player.level = attemptingLevel;
			}

			levelPassed = true;
		}

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

						willBeGameOver = true;
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

		if(menuScreenMode == MenuScreenMode.CREDITS) {
			if(Input.touchCount > 0) {
				if(Input.GetTouch(0).phase == TouchPhase.Moved) {
					creditScrollPosition = new Vector2(creditScrollPosition.x, creditScrollPosition.y + Input.GetTouch(0).deltaPosition.y);
				}
			}
		}

		if(isNewHighScore && highScoreTimer < highScoreTime) {
			highScoreTimer += Time.deltaTime;
		}

		if(keepCalm) {
			if(keepCalmTimer < keepCalmTime) {
				keepCalmTimer += Time.deltaTime;
			} else {
				keepCalm = false;
				keepCalmTimer = 0;
				
				// Active main game loop
				gameOver = false;
				highScoreTimer = 0;
				hasSavedGameState = false;
				isShowingGameOverMenu = false;
				isNewHighScore = false;
				reactionTime = 0;
			}
		}

		if(levelPassed) {
			if(levelPassedTimer < levelPassedTime) {
				levelPassedTimer += Time.deltaTime;
			}

			if(levelPassedTimer > levelPassedTime) {
				levelPassed = false;
				savePlayer();
				menuScreenMode = MenuScreenMode.LEVEL_SELECT;
				levelPassedTimer = 0;
			}
		}

		if(willBeGameOver && (Input.touchCount > 0 || Input.GetMouseButton(0)) && !gameOver) {
			reactionTime = flashTimer - timeToFlash;
			gameOver = true;
			willBeGameOver = false;
			Debug.Log("Reaction time "+reactionTime);
		}

	}

	void checkForHighScore() {
		if(dodgeCount > player.bestScore && !isTraining) {
			player.bestScore = dodgeCount;
			isNewHighScore = true;
			audio.PlayOneShot(highScoreSound);
		} else {
			audio.PlayOneShot(zapSound);
		}

		if (FB.IsLoggedIn) {
			var query = new Dictionary<string, string>();
			query["score"] = ""+player.bestScore;
			FB.API("/me/scores", Facebook.HttpMethod.POST, delegate(FBResult r) { Debug.Log("Result: " + r.Text); }, query);
		}
	}
	
	
	void calculateTimeToFlash() {
		timeToFlash = minTimeBetweenFlash + UnityEngine.Random.value*maxTimeBetweenFlash;
	}

	void fadeOutFlash() {
		iTween.CameraFadeTo(iTween.Hash("amount", 0.0f, "time", flashOutTime, "oncompletetarget", this.gameObject, "oncomplete", "initFlash"));
	}

	public void initFlash() {
		if(!willBeGameOver) {
			isFlashCaught = false;
			isFadingIn = false;
			flashTimer = 0;
			calculateTimeToFlash();
		}
	}

	Texture friendImage;

	void OnGUI() {
		GUI.skin = skin;

		switch(menuScreenMode) {
			case MenuScreenMode.LEVEL_SELECT:
					
					GUILayout.BeginArea(CENTER_SCREEN);
					GUI.enabled = true;
					
					GUILayout.Space (40);
					GUILayout.Label("TRAINING");
					GUILayout.Label("(10 PTS TO PASS)");

					if(GUILayout.Button("LEVEL 0 (EASY)")) {
						attemptingLevel = 1;
						isTraining = true;
						flashOutTime = 0.6f;
						menuScreenMode = MenuScreenMode.GAME;
						isTouchReleased = false;
						audio.Stop();
						levelPassed = false;
						dodgeCount = 0;
						counterDisplay.text = ""+dodgeCount;
					}

					GUI.enabled = (player.level > 0);

					if(GUILayout.Button("LEVEL 1 (MED)")) {
						attemptingLevel = 2;
						isTraining = true;
						flashOutTime = 0.5f;
						menuScreenMode = MenuScreenMode.GAME;
						isTouchReleased = false;
						audio.Stop();
						levelPassed = false;
						dodgeCount = 0;
						counterDisplay.text = ""+dodgeCount;
					}

					GUI.enabled = (player.level > 1);

					if(GUILayout.Button("LEVEL 2 (HARD)")) {
						attemptingLevel = 3;
						isTraining = true;
						flashOutTime = 0.4f;
						menuScreenMode = MenuScreenMode.GAME;
						isTouchReleased = false;
						audio.Stop();
						levelPassed = false;
						dodgeCount = 0;
						counterDisplay.text = ""+dodgeCount;
					}

					GUILayout.Space(40);

					if(GUILayout.Button("MAIN MENU")) {
						menuScreenMode = MenuScreenMode.MAIN_MENU;
					}

					GUILayout.EndArea();
			break;

			case MenuScreenMode.FRIEND_SCORES:
				GUILayout.Label("FRIENDS' SCORES");
				GUILayout.Space(10);

				creditScrollPosition = GUILayout.BeginScrollView(creditScrollPosition, false, true, GUILayout.Width(Screen.width), GUILayout.Height(500));
				if(fb.scores != null) {

					int ranking = 0;
					foreach(object score in fb.scores) {
				
						var friendScore = (Dictionary<string, object>) score;
						var user = (Dictionary<string, object>) friendScore["user"];
						
						if(ranking == 0) {
							GUILayout.Label("Champion:");
						}

						if(ranking == 1) {
							GUILayout.Space(20);
						}

						GUILayout.BeginHorizontal();
							if(fb.friendImages.TryGetValue(""+user["id"], out friendImage)) {
								ranking++;
								GUILayout.Label("#"+ranking+" ", GUILayout.Width(75));
								GUILayout.Label(friendImage, GUILayout.Width(75));
								GUILayout.Label(""+user["name"], GUILayout.Width(400));
								GUILayout.Label(""+friendScore["score"], GUILayout.Width(80));
							}
		                GUILayout.EndHorizontal();
					}
				} else {
					GUILayout.Label("RETICULATING SPLINES ETC...");
				}

				GUILayout.EndScrollView();

				if(GUILayout.Button("SHARE SCREENSHOT")) {
					shareScreenshot();
				}

				if(GUILayout.Button("MAIN MENU")) {
					menuScreenMode = MenuScreenMode.MAIN_MENU;
				}
			break;
		
			case MenuScreenMode.CREDITS:
				if(GUILayout.Button("BACK")) {
					menuScreenMode = MenuScreenMode.MAIN_MENU;
				}

				creditScrollPosition = GUILayout.BeginScrollView(creditScrollPosition, false, true);
					GUI.skin = creditsSkin;

					GUILayout.BeginVertical();
					
					GUILayout.Box (thunderclapLogo);
					GUILayout.Label("BY GAMER DEVELOPER EXCHANGE", GUILayout.Width(Screen.width));
					GUILayout.Space(10);
					GUILayout.Label("LEAD PROGRAMMER AND DESIGNER\nNick Hatter\nCEO of giftgaming.com and gamerdevx.com", GUILayout.Width(Screen.width));
					GUILayout.Space(10);
					GUILayout.Label("SPECIAL THANKS");
					GUILayout.Label("Adam James (Meownoodle on YouTube)");
					GUILayout.Label("Robert Streeting (robsws.co.uk)");
					GUILayout.Label("Charles Payne (business-aspirations.com)");
					GUILayout.Label("Mark Salvin (marksalvin.com)");
					GUILayout.Label("Cate (AntiDoge5Life on Facebook)");

					GUILayout.Label("MUSIC\n\"Cut and Run\" by Kevin MacLeod (incompetech.com)\nLicensed under Creative Commons: By Attribution 3.0\nhttp://creativecommons.org/licenses/by/3.0/");
					GUILayout.Label ("GAME OVER SOUND\nwoosh_02.wav by Glaneur de sons (http://www.freesound.org/people/Glaneur%20de%20sons/sounds/34172/)\n\nLicensed under Creative Commons Share Alike 3.0\nhttp://creativecommons.org/licenses/by/3.0/");
					GUILayout.Label("FONTS\n\"Oswald\" by Vernon Adams (vern@newtypography.co.uk)\nLicensed under SIL OPEN FONT LICENSE Version 1.1\nhttp://scripts.sil.org/OFL\n\n" + 
			                				"\"Open Sans\" by Steve Matteson\nLicensed under the Apache License, Version 2.0\nhttp://www.apache.org/licenses/LICENSE-2.0");
				
					GUILayout.EndVertical();

					GUI.skin = skin;
				GUILayout.EndScrollView();


			break;
		
			case MenuScreenMode.HEALTH_WARNING:
				GUILayout.Label("HEALTH WARNING\n\nTHIS GAME CONTAINS FLASHING LIGHTS WHICH MAY INDUCE EPILEPTIC SEIZURES.");

				if(GUILayout.Button("OK")) {
					menuScreenMode = MenuScreenMode.MAIN_MENU;
				}
			break;

			case MenuScreenMode.HOW_TO_PLAY:
						GUILayout.Label("ONLY TOUCH THE SCREEN WHEN IT FLASHES WHITE.\n\nUMBRELLAS ALLOW YOU TO MAKE A MISTAKE.\n\nTHIS IS A HARD GAME AND TAKES PRACTICE.");
						if(GUILayout.Button("TRAINING")) {
							menuScreenMode = MenuScreenMode.LEVEL_SELECT;
						}
			break;

			case MenuScreenMode.MAIN_MENU:

					GUILayout.BeginArea(CENTER_SCREEN);
						
						
						GUILayout.BeginVertical();
						GUILayout.Label(thunderclapLogo);
						GUILayout.Space(25);


						if(GUILayout.Button("PLAY")) {
							isTraining = false;
							flashOutTime = 0.3f;
							menuScreenMode = MenuScreenMode.GAME;
							isTouchReleased = false;
							audio.Stop();
							levelPassed = false;
						}

						if(GUILayout.Button("TRAINING")) {
							menuScreenMode = MenuScreenMode.LEVEL_SELECT;
						}

						if(FB.IsLoggedIn) {
							GUI.enabled = false;
						}		

						if(GUILayout.Button(fbConnectButton)) {							
							fb.CallFBLogin();
						}

						GUI.enabled = true;

						if(GUILayout.Button("FRIENDS' SCORES")) {
							if(!FB.IsLoggedIn) {
								fb.CallFBLogin();
							} else {
								var query = new Dictionary<string, string>();
								query["score"] = ""+player.bestScore;
								FB.API("/me/scores", Facebook.HttpMethod.POST, delegate(FBResult r) { 
									fb.QueryScores();
									menuScreenMode = MenuScreenMode.FRIEND_SCORES;
								}, query);
							}
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

			if(keepCalm) {
				GUI.Label (CENTER_SCREEN_MESSAGE, "KEEP CALM AND CARRY ON\n\nCONTINUE IN "+Mathf.RoundToInt(keepCalmTime - keepCalmTimer)+"...");
			} else {
				if(gameOver || levelPassed) {
					GUILayout.BeginArea(CENTER_SCREEN);
							
					
							if(isNewHighScore) {
								if(highScoreTimer < highScoreTime) {
									GUILayout.Box(highScoreImage);
									GUILayout.Label("NEW HIGH SCORE!");
								} else {
									GUILayout.Label("NEW HIGH SCORE: "+dodgeCount);
									GUILayout.Label("OMG.");
								}
								
							} else {
								if(levelPassed) {
									GUILayout.Label("LEVEL PASSED!");
								} else {
									GUILayout.Label("GAME OVER");											
									if(reactionTime > 0) {
										GUILayout.Label("REACTION: " + Math.Round(reactionTime, 3)+"s"
								        GUILayout.Label("NEED: "+(flashInTime+flashOutTime)+"s");
									} else {
										GUILayout.Label("TAPPED TOO EARLY!");
									}

									GUILayout.Label("SCORE: "+dodgeCount+"   BEST: "+player.bestScore);
								}
							}

							if(isTouchReleased && isShowingGameOverMenu) {
								GUI.enabled = true;
							} else {
								GUI.enabled = false;
							}
					
							
						if((isNewHighScore && highScoreTimer > highScoreTime) || !isNewHighScore)  {
								if(GUILayout.Button("RETRY")) {
									Handheld.StopActivityIndicator();
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
									highScoreTimer = 0;
									hasSavedGameState = false;
									isShowingGameOverMenu = false;
									isNewHighScore = false;
									reactionTime = 0;
									
								
									Debug.Log("Retry pressed");
								}

								if(GUILayout.Button("SHARE SCREENSHOT")) {
									shareScreenshot();
								}
						
								if(isTouchReleased && isShowingGameOverMenu && isIAPEnabled) {
									GUI.enabled = true;
								} else {
									GUI.enabled = false;
								}

								//#if !UNITY_EDITOR
								if(GUILayout.Button("BUY UMBRELLAS")) {
									iap.Purchase("3_UMBRELLAS");
								}

								if(GUILayout.Button("BUY CARRY ON")) {
									#if UNITY_EDITOR
										unlockIAP("CARRY_ON");
									#else
										iap.Purchase("CARRY_ON");
									#endif
								}

								if(GUILayout.Button("BUY MORE TIME")) {
									iap.Purchase("MORE_TIME");
								}

								//#endif

							if(isTouchReleased && isShowingGameOverMenu) {
								GUI.enabled = true;
							} else {
								GUI.enabled = false;
							}

							if(GUILayout.Button("MAIN MENU")) {
								audio.Play();
								menuScreenMode = MenuScreenMode.MAIN_MENU;
							}
						}

						GUILayout.EndArea();
						GUI.enabled = true;
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
				}// End of gameover check
			}// End of keep calm check
		}// End of main game UI check


		GUI.skin = facebookSkin;
		if(showShareDialog) {
			GUI.Box(ENTIRE_SCREEN, "", "Overlay");
			GUI.BeginGroup(shareDialogRect, shareDialogBackground);
			
			screenshotComment = GUI.TextArea(screenshotTextAreaRect, screenshotComment);

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

	}

	void shareScreenshot() {
		if(!FB.IsLoggedIn) {
			fb.CallFBLogin();
		}
		
		fb.getScreenshot(delegate() { showShareDialog = true; });
	}

	public void unlockIAP(string productID) {
		Handheld.StopActivityIndicator();

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

				keepCalm = true;
			break;

			case "EXTRA_TIME":
				player.reactionLeeway = 0.1f;
				savePlayer();
			break;
		}
	}

	public void cancelIAP(string reason) {
		Handheld.StopActivityIndicator();
	}

	public void canMakePurchases(string iOSResult) {
		switch(iOSResult) {
			case "true":
				isIAPEnabled = true;
			break;
		}
	}

}
