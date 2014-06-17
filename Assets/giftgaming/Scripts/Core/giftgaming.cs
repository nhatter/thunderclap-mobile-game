using UnityEngine;
using SimpleJSON;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class giftgaming : MonoBehaviour {
	// Assigned from giftgaming.com
	// Make sure you set this to the correct ID
	public GUISkin skin;
	public GUISkin gameSkin;

	public int GAME_ID = 3;

	// How often to check for gifts
	public int CHECK_FOR_GIFT_INTERVAL = 2;

	public static giftgaming use;
	public string host = "";
	public string GIFTS_URL;
	public bool IS_DEBUG_MODE = true;
	public bool isCouponReminderSet = true;
	public bool isShowingNotInterested = false;

	public float giftTagRotation = 10.0f;
	public Vector2 giftTagRotationPivot = new Vector2(Screen.width, Screen.height);
	
	string PLAYER_XML_FILE;
	string DATA_PATH;
	string COUPON_CACHE_FILE;

	public Texture2D giftIcon;
	public Texture2D foursquareLogo;
	GUIContent giftButton = new GUIContent();

	Dictionary<string, Gift> giftLookup = new Dictionary<string, Gift>();
	Queue<Gift> giftQueue = new Queue<Gift>();
	public Gift currentGift;

	ggPlayer player;
	int playerID = -1;
		
	JSONNode couponResponse;
	PassViewObject passViewObject;

	bool _isViewingCoupons = false;

	Dictionary<string, Texture2D> logoCache = new Dictionary<string, Texture2D>();
	List<Coupon> coupons = new List<Coupon>();
	List<Sponsor> topSponsors = new List<Sponsor>();

	Rect BOTTOM_RIGHT;
	Rect ENTIRE_SCREEN = new Rect(0, 0, Screen.width, Screen.height);
	Rect COUPON_SCREEN = new Rect(0,150, Screen.width, Screen.height-150);
	GameObject inGameGift;
	Texture2D inGameGiftLogo;
	Rect wrappedGiftRect;

	public static int THIRD_OF_SCREEN_WIDTH = (int)(Screen.width/3.0f);
	
	private bool showList = false;
	private int selectedStoreChainIndex = 0;
	private GUIContent[] storeChainList;
	private GUIStyle listStyle;
	private bool picked = false;

	public Texture2D couponGeoReminderImage;
	private GUIContent couponGeoReminder = new GUIContent();

	bool isDisplayingCouponTerms = false;
	Vector2 couponTermsScrollPosition = new Vector2(0,0);

	bool isDisplayingGeoConsent = false;
	public Texture2D geoConsentImage;

	public bool isSafeToDistractPlayer = false;

	void setupDropdown() {
		// Make some content for the popup list
		storeChainList = new GUIContent[5];
		storeChainList[0] = new GUIContent("Sainsburys");
		storeChainList[1] = new GUIContent("Tesco");
		storeChainList[2] = new GUIContent("Waitrose");
		storeChainList[3] = new GUIContent("Morrisons");
		storeChainList[4] = new GUIContent("Superdrug");
		
		// Make a GUIStyle that has a solid white hover/onHover background to indicate highlighted items
		listStyle = new GUIStyle();
		listStyle.normal.textColor = Color.white;
		var tex = new Texture2D(2, 2);
		var colors = new Color[4];
		for (int i=0; i<4; i++) {
			colors[i] = Color.white;
		}

		tex.SetPixels(colors);
		tex.Apply();
		listStyle.font = skin.window.font;
		listStyle.hover.background = tex;
		listStyle.onHover.background = tex;
		listStyle.padding.left = listStyle.padding.right = listStyle.padding.top = listStyle.padding.bottom = 4;
	}

	void Start()
	{
		if(giftgaming.use != null) {
			Destroy(this.gameObject);
			return;
		}

		DontDestroyOnLoad(this);

		GIFTS_URL = "http://"+host+":8080/api/ads";
		setupDropdown();
		setup();
	}

	void Update() {
		if(_isViewingCoupons) {
			if(Input.touchCount > 0) {
				couponScrollPosition = new Vector2(0, couponScrollPosition.y+Input.GetTouch(0).deltaPosition.y);
			}
		}
	}

	void setup() {
		// Don't re-init if giftgaming already running
		if(use != null) {
			return;
		}

		DATA_PATH = Application.persistentDataPath+"/";

		PLAYER_XML_FILE = DATA_PATH + "giftgaming.xml";
		Debug.Log("giftgaming player prefs file is at "+PLAYER_XML_FILE);

		COUPON_CACHE_FILE = DATA_PATH + "giftgaming_coupon.pkpass";
		Debug.Log("Coupon cache file is at "+COUPON_CACHE_FILE);

		giftButton.image = giftIcon;

		GUIStyle giftButtonStyle = skin.GetStyle("giftButton");
		
		BOTTOM_RIGHT = new Rect(Screen.width - giftButtonStyle.fixedWidth, Screen.height - giftButtonStyle.fixedHeight, giftButtonStyle.fixedWidth, giftButtonStyle.fixedHeight);


		Screen.orientation = ScreenOrientation.Portrait;

		couponGeoReminder.text = "            Remind me to use it near: ";
		couponGeoReminder.image = couponGeoReminderImage;

		// Only attempt to load player file after all the paths have been setup
		getggPlayerID();

		use = this;
	}

	public void setInGameGift(GameObject inGameGift) {
		this.inGameGift = inGameGift;
		inGameGiftLogo = (Texture2D) inGameGift.GetComponentInChildren<Renderer>().material.GetTexture(0);
		GUIStyle giftContentsStyle = skin.GetStyle("GiftContents");
		wrappedGiftRect = new Rect(Screen.width/2 - giftContentsStyle.fixedWidth/2, 150 + Screen.height/2 - giftContentsStyle.fixedHeight/2, giftContentsStyle.fixedWidth, giftContentsStyle.fixedHeight);
	}

	public void setInGameGiftLogo(Texture2D inGameGiftTexture) {
		inGameGiftLogo = inGameGiftTexture;
		GUIStyle giftContentsStyle = skin.GetStyle("GiftContents");
		wrappedGiftRect = new Rect(Screen.width/2 - giftContentsStyle.fixedWidth/2, 150 + Screen.height/2 - giftContentsStyle.fixedHeight/2, giftContentsStyle.fixedWidth, giftContentsStyle.fixedHeight);

	}

	// remember to use StartCoroutine when calling this function!
	IEnumerator getggPlayerIDThread()
	{
		/** Sample gift request format: 
		 * curl 
		 * --header "Content-type: application/json"
		 * --request POST "http://giftgaming.com/ad" 
		 * --data '{"appId" : "dev-app-id", "requestId" : "2", "deviceId" : "cookie", "targeting" : {"gender" : "female", "age" : 24}}'
		 */

		string createggPlayerJSON = "{\"action\":\"newPlayerID\", \"gameID\":\""+GAME_ID+"\"}";
		Debug.Log("Create player JSON: " + createggPlayerJSON);

		WWW postJsonResponse = postJson(createggPlayerJSON);
		yield return postJsonResponse; // wait til download done
		Debug.Log("Response "+postJsonResponse.ToString());

		if (postJsonResponse.error != null) {
			print("There was an error getting the ggPlayer ID: " + postJsonResponse.error);
			StartCoroutine(getggPlayerIDThread());
			yield return new WaitForSeconds(2);
		} else {
			var giftResponse = JSONNode.Parse(getValidJson(postJsonResponse.text));
			Debug.Log(getValidJson(postJsonResponse.text));
			if(giftResponse["ID"] != null) {
				if(!int.TryParse(giftResponse["ID"], out playerID)) {
					print("Could not parse player ID");
				}

				requestGift();
				yield return playerID;

				#if !UNITY_WEBPLAYER
				try {
					player = new ggPlayer(playerID);
					XMLManager.save<ggPlayer>(player, PLAYER_XML_FILE);
			    } catch (Exception e) {
					Debug.Log("Could not write giftgaming ggPlayer file " + e);
				}
				#endif
			}
			yield return new WaitForSeconds(2);
		}
	}
	
	public void getggPlayerID() {
		if(File.Exists(PLAYER_XML_FILE)) {
			Debug.Log("giftgaming ggPlayer file exists - loading existing player");

			try {
				player = XMLManager.load<ggPlayer>(PLAYER_XML_FILE);
				playerID = player.playerID;
				requestGift();
			} catch (Exception e) {
				Debug.Log("Error parsing giftgaming ggPlayer file " + e);
				StartCoroutine(getggPlayerIDThread());
			}
		} else {
			StartCoroutine(getggPlayerIDThread());
		}
	}

	IEnumerator saveggPlayerPreferences(int genderIndex, int ageIndex) {
		string postJsonString = "{\"action\":\"notInterested\", \"playerID\":\""+playerID+"\", \"genderIndex\":\""+genderIndex+"\", \"ageRangeIndex\":\""+ageIndex+"\"}";
		
		Debug.Log(postJsonString);
		
		WWW postJsonResponse = postJson(postJsonString);
		yield return postJsonResponse; // wait til download done
		
		if (postJsonResponse.error != null) {
			print("There was an error opening the gift:" + postJsonResponse.error);
		} else {
			var giftResponse = JSONNode.Parse(getValidJson(postJsonResponse.text));
			Debug.Log(getValidJson(postJsonResponse.text));

			if(giftResponse["response"] != null) {
				switch(giftResponse["response"]) {
					case "PREFERENCES_SAVED":
						Debug.Log("ggPlayer gift preferences saved successfully");
					break;
				}
			}
		}
	}

	IEnumerator requestGiftThread() {
		yield return new WaitForSeconds(CHECK_FOR_GIFT_INTERVAL);

		requestGift();

		WWW postJsonResponse = postJson("{\"action\":\"requestGift\", \"playerID\":\""+playerID+"\"}");
		yield return postJsonResponse; // wait til download done

		if (postJsonResponse.error != null) {
			print("There was an error requesting the gift:" + postJsonResponse.error);
		} else {
			var giftResponse = JSONNode.Parse(getValidJson(postJsonResponse.text));
			Debug.Log(getValidJson(postJsonResponse.text));

			if(giftResponse["giftCode"] != null) {
				if(!giftLookup.ContainsKey(giftResponse["giftKey"])) {

					yield return StartCoroutine(cacheLogo(giftResponse["creative"]["logoURL"]));
					yield return StartCoroutine(cacheLogo(giftResponse["creative"]["coupon"]["logo"]));

					Texture2D brandLogo;
					Texture2D couponLogo;
					logoCache.TryGetValue(giftResponse["creative"]["logoURL"], out brandLogo);
					logoCache.TryGetValue(giftResponse["creative"]["coupon"]["logo"], out couponLogo);
					Debug.Log("coupon logo :" +giftResponse["creative"]["coupon"]["logo"]);
					string messageToPlayer = giftResponse["creative"]["messageToPlayer"];
					messageToPlayer = messageToPlayer.Replace("\u005cr\u005cn", "\n");
					Gift newGift = new Gift(giftResponse["giftCode"], giftResponse["giftKey"], brandLogo, messageToPlayer, couponLogo);
					giftQueue.Enqueue(newGift);
					giftLookup.Add(giftResponse["giftKey"], newGift);
				}
			}
		}
	}

	IEnumerator giftAction(string giftAction, string giftKey, float time) {
		string postJsonString = "{\"action\":\""+giftAction+"\", \"playerID\":\""+playerID+"\", \"giftKey\":\""+giftKey+"\", \"time\":"+time*1000;

		// Remember to pass selected store chain and location if saving coupon
		if(giftAction == "redeem") {
			postJsonString += ", \"storeChain\":\""+storeChainList[selectedStoreChainIndex].text+"\", \"latitude\":"+52.2050+", \"longitude\":"+0.1190;
		}

		// End JSON string construction
		postJsonString += "}";

		Debug.Log(postJsonString);

		WWW postJsonResponse = postJson(postJsonString);
		yield return postJsonResponse; // wait til download done
		
		if (postJsonResponse.error != null) {
			print("There was an error opening the gift:" + postJsonResponse.error);
		} else {
			var giftResponse = JSONNode.Parse(getValidJson(postJsonResponse.text));
			Debug.Log(getValidJson(postJsonResponse.text));
			if(giftResponse["response"] != null) {
				switch(giftResponse["response"]) {
					case "GIFT_OPENED":
						Debug.Log("Gift opened successfully");
					break;

					case "COUPON_REDEEMED":
						Debug.Log("Coupon redeemed successfully");
						currentGift.couponURL = giftResponse["couponURL"];

						Handheld.StartActivityIndicator();
						WWW downloadCoupon = new WWW (currentGift.couponURL);
						yield return downloadCoupon;

						// Cache coupon incase needs to be used again
						File.WriteAllBytes(COUPON_CACHE_FILE, downloadCoupon.bytes);

						Handheld.StopActivityIndicator();

						currentGift.isCouponRedeemed = true;

						#if UNITY_IPHONE && !UNITY_EDITOR
							passViewObject = (new GameObject("PassViewObject")).AddComponent<PassViewObject>();
							passViewObject.Init();
							passViewObject.ShowPass(COUPON_CACHE_FILE);
						#endif
					break;

					case "GIFT_CLOSED":
						Debug.Log("Gift closed successfully");
					break;

					case "PREFERENCES_SAVED":
						Debug.Log("ggPlayer gift preferences saved successfully");
					break;
				}
			}
			yield return new WaitForSeconds(CHECK_FOR_GIFT_INTERVAL);
		}
	}

	Texture2D couponLogo;
	IEnumerator getCouponsThread() {
		WWW postJsonResponse = postJson("{\"action\":\"getCoupons\", \"playerID\":\""+playerID+"\"}");
		yield return postJsonResponse; // wait til download done
		
		if (postJsonResponse.error != null) {
			print("There was an error getting the coupons:" + postJsonResponse.error);
		} else {
			var giftResponse = JSONNode.Parse(getValidJson(postJsonResponse.text));
			Debug.Log(getValidJson(postJsonResponse.text));
			
			if(giftResponse["couponList"] != null) {
				couponResponse = giftResponse["couponList"];
				Debug.Log("Coupons: "+couponResponse.Count);
				HashSet<string> urls = new HashSet<string>();

				coupons = new List<Coupon>();
				for(int i=0; i<couponResponse.Count; i++) {
					if(couponResponse[i]["logoURL"] != null) {
						string URL = couponResponse[i]["logoURL"];
						string unquotedURL = URL.Replace("\"","");
						urls.Add(URL);
					}
				}

				foreach(string url in urls) {
					Debug.Log(url);
					yield return StartCoroutine(cacheLogo(url));
				}
	
				for(int i=0; i<couponResponse.Count; i++) {
					if(couponResponse[i]["logoURL"] != null) {
						string URL = couponResponse[i]["logoURL"];
						string unquotedURL = URL.Replace("\"","");
						if(!logoCache.TryGetValue(unquotedURL, out couponLogo)) {
							Debug.Log("Cache miss: " + unquotedURL);
						}

						coupons.Add(new Coupon(couponResponse[i]["couponURL"], couponResponse[i]["brand"], couponLogo, couponResponse[i]["summary"], couponResponse[i]["giftCode"],
						                       couponResponse[i]["storeLink"], couponResponse[i]["couponTerms"], couponResponse[i]["backgroundColor"], couponResponse[i]["foregroundColor"]));
					}
				}

			}
		}
	}

	IEnumerator cacheLogo(string url) {
		if(!logoCache.ContainsKey(url)) {
			Debug.Log("Logo cache miss - downloading logo");
			WWW downloadLogo = new WWW(url);
			yield return downloadLogo;
			Texture2D brandLogo = downloadLogo.texture;
			logoCache.Add(url, brandLogo);
			Debug.Log("Cached logo: "+url);
		} else {
			Debug.Log("Cache logo hit");
		}
	}

	IEnumerator getTopSponsorsThread() {
		WWW postJsonResponse = postJson("{\"action\":\"getTopSponsors\", \"playerID\":\""+playerID+"\"}");
		yield return postJsonResponse; // wait til download done
		
		if (postJsonResponse.error != null) {
			print("There was an error getting the top sponsors:" + postJsonResponse.error);
		} else {
			var giftResponse = JSONNode.Parse(getValidJson(postJsonResponse.text));
			Debug.Log(getValidJson(postJsonResponse.text));
			
			if(giftResponse["topSponsors"] != null) {
				JSONNode sponsorResponse = giftResponse["topSponsors"];
				topSponsors = new List<Sponsor>();
				for(int i=0; i<sponsorResponse.Count; i++) {
					if(sponsorResponse[i]["logo"] != null) {
						string URL = sponsorResponse[i]["logo"];
						string unquotedURL = URL.Replace("\"","");
						yield return StartCoroutine(cacheLogo(unquotedURL));
						Sponsor sponsor = new Sponsor();
						Texture2D logo;
						logoCache.TryGetValue(unquotedURL, out logo);
						sponsor.logo = logo;
						topSponsors.Add(sponsor);
					}
				}

			}
		}
	}

	Vector2 couponScrollPosition;
	Coupon openedCoupon;

	public void displayCoupons() {
		if(coupons.Count > 0) {
			foreach(Coupon coupon in coupons) {
				GUILayout.BeginHorizontal();
					if(coupon.logo != null) {
						if(GUILayout.Button(coupon.logo, "box")) {
							Debug.Log("Opening "+coupon.publicURL+"...");
							openedCoupon = coupon;
						}
					}
				GUILayout.EndHorizontal();
			}
		}
	}

	public void displayTopSponsors() {
		int rank = 1;
		if(topSponsors.Count > 0) {
			foreach(Sponsor sponsor in topSponsors) {
				if(sponsor.logo != null) {
					//GUILayout.BeginHorizontal();
						GUILayout.Label("#"+rank, "PlainText");
						GUILayout.Box(sponsor.logo);
					//GUILayout.EndHorizontal();
					rank++;
				}
			}
		}
	}
	
	public bool isViewingCoupons() {
		return _isViewingCoupons;
	}

	public void hideCoupons() {
		_isViewingCoupons = false;
	}

	public void requestCoupons() {
		_isViewingCoupons = true;
		StartCoroutine(getCouponsThread());
	}

	public void requestGift() {
		StartCoroutine(requestGiftThread());
	}

	public int getPendingGiftCount() {
		return giftLookup.Keys.Count;
	}

	public void share() {
		if(currentGift != null) {
			if(currentGift.isGiftOpened) {
				currentGift.isShared = true;
				StartCoroutine(giftAction("share", currentGift.giftKey, Time.time - currentGift.giftReceiveTimeStamp));
			}
		}
	}

	public void award() {
		if(currentGift != null) {
			if(currentGift.isGiftOpened) {
				currentGift.isAwarded = true;
				StartCoroutine(giftAction("award", currentGift.giftKey, Time.time - currentGift.giftReceiveTimeStamp));
			}
		}
	}

	public void redeemCoupon() {
		if(currentGift != null) {
			if(currentGift.isGiftOpened) {
				currentGift.isCouponRedeemed = true;
				currentGift.redeemCouponTime = Time.time - currentGift.giftReceiveTimeStamp;

				StartCoroutine(giftAction("redeem", currentGift.giftKey, currentGift.redeemCouponTime));
			}
		}
	}

	public void openGift() {
		if(currentGift != null) {
			if(!currentGift.isGiftOpened) {
				currentGift.isGiftOpened = true;
				currentGift.giftOpenTimeStamp = Time.time;
				currentGift.giftOpenTime = currentGift.giftOpenTimeStamp - currentGift.giftReceiveTimeStamp;
				giftLookup.Remove(currentGift.giftKey);

				StartCoroutine(giftAction("openGift", currentGift.giftKey, currentGift.giftOpenTime));
				StartCoroutine(getTopSponsorsThread());
			}
		}
	}
	
	public void closeGift() {
		if(currentGift != null) {
			if(currentGift.isGiftOpened) {
				Input.location.Stop();

				currentGift.isGiftOpened = false;
				currentGift.giftCloseTime = Time.time - currentGift.giftOpenTimeStamp;
				giftLookup.Remove(currentGift.giftKey);
				Debug.Log("gift close time: "+currentGift.giftCloseTime);

				StartCoroutine(giftAction("closeGift", currentGift.giftKey, currentGift.giftCloseTime));
			}
		}
	}

	public bool isGiftOpened() {
		if(currentGift == null) {
			return false;
		} else {
			return currentGift.isGiftOpened;
		}
	}

	public bool isCouponRedeemed() {
		if(currentGift == null) {
			return false;
		} else {
			return currentGift.isCouponRedeemed;
		}
	}

	public bool isShared() {
		if(currentGift == null) {
			return false;
		} else {
			return currentGift.isShared;
		}
	}

	public bool isAwarded() {
		if(currentGift == null) {
			return false;
		} else {
			return currentGift.isAwarded;
		}
	}

	public void nextGift() {
		if(giftQueue.Count > 0) {
			currentGift = giftQueue.Dequeue();
		}
	}

	public bool hasGiftMessage() {
		if(currentGift != null) {
			return currentGift.messageToPlayer != null;
		} else {
			return false;
		}
	}

	public string getGiftMessage() {
		return currentGift.messageToPlayer;
	}

	public string getGiftCouponSummary() {
		return currentGift.couponURL;
	}

	public int getHumanggPlayerID() {
		return playerID;
	}
	
	public WWW postJson(string json) {
		Hashtable postHeader = new Hashtable();
		postHeader.Add("Content-Type", "application/json");
		var encoding = new System.Text.UTF8Encoding();
		WWW postJsonResponse = new WWW(GIFTS_URL, encoding.GetBytes(json), postHeader);
		return postJsonResponse; // Wait until the download is done
	}

	string getValidJson(string rawJson) {
		var unescapedResponse = rawJson.Replace("\\\"", "\"");
		return trimJsonQuotes(unescapedResponse);
	}

	string trimJsonQuotes(string json) {
		return json.Substring(1).Substring(0, json.Length-2);
	}

	public void drawBrandLogo() {
		if(currentGift != null) {
			if(currentGift.brandLogo != null) {
				currentGift.drawBrandLogo();
			}
		}
	}

	public void drawGiftLogo() {
		if(inGameGiftLogo != null) {
			GUILayout.BeginHorizontal();
				GUILayout.BeginVertical();
					GUILayout.BeginHorizontal();
						GUILayout.Box(inGameGiftLogo, "GiftContents");
						GUILayout.Label(" + a special discount");
					GUILayout.EndHorizontal();
				GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}
	}

	public void drawCouponLogo() {
		if(currentGift != null) {
			if(currentGift.couponLogo != null) {
				currentGift.drawCouponLogo();
			} else {
				Debug.Log("NO COUPON");
			}
		}
	}

	public void drawGiftButton() {
		if(getPendingGiftCount() > 0 && !isGiftOpened()) {
			if(GUI.Button(BOTTOM_RIGHT, giftButton, "giftButton")) {
				giftgamingGifts.openNextGift();
			}
		}
	}

	public Texture2D giftgamingLogo;
	string thanksTweet = "@FairTrade thanks for the help in #sweetdemo #giftgaming";

	public void drawGift() {
		if(isGiftOpened()) {

			GUILayout.BeginVertical("GiftWindow", GUILayout.Width(Screen.width), GUILayout.Height(Screen.height));
				GUILayout.BeginHorizontal();
					drawBrandLogo();
				GUILayout.EndHorizontal();

				//GUI.skin = gameSkin;
				GUILayout.Label ("HAS GIVEN YOU A GIFT!");
				//GUI.skin = skin;

				GUILayout.Space(4);
				drawGiftLogo();
				GUILayout.BeginHorizontal();
				GUILayout.Space(5);
				drawCouponLogo();
				GUILayout.EndHorizontal();
				
				GUILayout.Space(5);
			
				GUILayout.BeginHorizontal(GUILayout.Height(skin.GetStyle("AddToPassbook").fixedHeight));
					GUI.skin = gameSkin;
					#if UNITY_IPHONE
						if(GUILayout.Button("Save to Passbook")) {
							if(!player.hasBeenOfferedGeoConsent) {
								isDisplayingGeoConsent = true;
							} else {
								redeemCoupon();
							}
						}
					#else
						if(GUILayout.Button("", "SaveCoupon")) {
							redeemCoupon();
							closeGift();
						}
					#endif

					
					if(GUILayout.Button("Close")) {
						if(!player.hasGivenPreferences) {
							isShowingNotInterested = true;
						} else {
							closeGift();
						}
					}
					GUI.skin = skin;
		
				GUILayout.EndHorizontal();

				GUILayout.BeginVertical();
					GUILayout.BeginHorizontal();
						GUILayout.Label("DELIVERED BY ");
						GUILayout.Label(giftgamingLogo, "Logo", GUILayout.Height(60));
					GUILayout.EndHorizontal();
					GUILayout.Space(5);
					GUILayout.Label("giftgaming® privacy policy available at: www.giftgaming.com/privacy\n"
			                	 +  "Location-based reminders powered by FOURSQUARE and APPLE, INC\n"
								 +  "giftgaming Ltd is NOT AFFILIATED with FOURSQUARE or APPLE, INC", "SmallPrint");
				GUILayout.EndVertical();


			GUILayout.EndVertical();

			//if ( Popup.List (new Rect(375, 620+254, 225, skin.GetStyle("Button").fixedHeight), 
			//                 ref showList, ref selectedStoreChainIndex, new GUIContent(storeChainList[selectedStoreChainIndex]), storeChainList, "StoreSelect", "Window", listStyle)) {
			//		picked = true;
			//}
		}
	}

	Color originalGUIColor;
	public void drawSavedCoupons() {
		if(isViewingCoupons()) {
			GUI.Box(ENTIRE_SCREEN, "", "Overlay");

			GUI.skin = gameSkin;
			GUILayout.Label("Saved Coupons - Tap to use them");
			if(GUILayout.Button("Close Window")) {
				hideCoupons();
			}
			GUI.skin = skin;
			
			couponScrollPosition = GUILayout.BeginScrollView(couponScrollPosition, GUILayout.Width (Screen.width));
				GUILayout.BeginVertical();
					

					displayCoupons();
					
				GUILayout.EndVertical();
			GUILayout.EndScrollView();

			if(openedCoupon != null) {
				originalGUIColor = GUI.color;

				GUI.Box(ENTIRE_SCREEN, "", "Overlay");

				GUI.color = openedCoupon.backgroundColor;
				GUI.Box(COUPON_SCREEN, "", "Coupon");

				GUI.color = openedCoupon.foregroundColor;
				GUILayout.BeginArea(COUPON_SCREEN, "");
					GUI.skin = gameSkin;
					GUILayout.Space (10);
					GUILayout.Label(openedCoupon.brand);

					GUI.color = originalGUIColor;
					GUILayout.Box(openedCoupon.logo);
					GUILayout.Space(10);

					
					if(GUILayout.Button("Use Coupon Online")) {
						Application.OpenURL(openedCoupon.storeLink);
					}

					if(GUILayout.Button("Terms and Conditions")) {
						isDisplayingCouponTerms = true;
					}

					if(GUILayout.Button("Close Coupon")) {
						openedCoupon = null;
					}
					GUI.skin = skin;
					
				GUILayout.EndArea();

				if(isDisplayingCouponTerms) {

					GUI.Box(ENTIRE_SCREEN, "", "Overlay");

					GUI.color = openedCoupon.backgroundColor;
					GUI.Box(COUPON_SCREEN, "", "Coupon");

					GUI.color = openedCoupon.foregroundColor;
					GUILayout.BeginArea(COUPON_SCREEN, "");
						

						GUILayout.Label("TERMS AND CONDITIONS", "CouponTerms");

						couponScrollPosition = GUILayout.BeginScrollView(couponScrollPosition, GUILayout.Width (Screen.width));
							GUILayout.Label(openedCoupon.couponTerms, "CouponTerms");
						GUILayout.EndScrollView();

						GUI.skin = gameSkin;
						if(GUILayout.Button("Close Terms")) {
							isDisplayingCouponTerms = false;
						}
						GUI.skin = skin;
					GUILayout.EndArea();
				}

				GUI.color = originalGUIColor;
			}
		}
	}

	int genderIndex = 0;
	string[] genders = {"Male", "Female"};

	int ageRangeIndex = 0;
	string[] ageRanges = {"Under 18", "18 - 30", "31 - 45", "46 - 60", "60+"};


	public void drawNotInterested() {
		GUI.skin = gameSkin;
		GUILayout.BeginVertical("GiftWindow", GUILayout.Width(Screen.width), GUILayout.Height(Screen.height));
			
			GUILayout.Label("Help giftgaming give better coupons");

			GUILayout.Label("Gender");
			genderIndex = GUILayout.SelectionGrid(genderIndex, genders, 2);
		
			GUILayout.Label("Age Range");
			ageRangeIndex = GUILayout.SelectionGrid(ageRangeIndex, ageRanges, 2);

			if(GUILayout.Button("Save my preferences")) {
				isShowingNotInterested = false;
				player.hasGivenPreferences = true;
				StartCoroutine(saveggPlayerPreferences(genderIndex, ageRangeIndex));
			}
			
		GUILayout.EndVertical();
		GUI.skin = skin;
	}

	void drawGeoConsent() {
		GUI.Box(ENTIRE_SCREEN, "", "Overlay");
		GUI.BeginGroup(COUPON_SCREEN, geoConsentImage);
			GUILayout.BeginArea (new Rect(0, 340, 250, 330));
				GUILayout.BeginHorizontal();
					GUI.skin = gameSkin;
					if(GUILayout.Button("Yes")) {
						Input.location.Start();
						giveGeoConsent(true);
					}

					if(GUILayout.Button("No")) {
						giveGeoConsent(false);
					}
					GUI.skin = skin;
				GUILayout.EndHorizontal();
			GUILayout.EndArea();
		GUI.EndGroup();
	}

	void giveGeoConsent(bool isAllowedToTrackLocation) {
		isDisplayingGeoConsent = false;
		player.hasGivenGeoConsent = isAllowedToTrackLocation;
		player.hasBeenOfferedGeoConsent = true;
		XMLManager.save<ggPlayer>(player, PLAYER_XML_FILE);
		redeemCoupon();
	}

	void listCallBack() {
		Debug.Log("Test");
	}

	public void OnGUI() {
		GUI.skin = skin;

		if(isDisplayingGeoConsent) {
			GUI.enabled = false;
		} else {
			GUI.enabled = true;
		}

		GIFTS_URL = "http://"+host+":8080/api/ads";

		if(isSafeToDistractPlayer) {
			drawGiftButton();
		}

		if(isShowingNotInterested) {
			drawNotInterested();
		} else {
			drawGift();
		}

		if(isDisplayingGeoConsent) {
			GUI.enabled = true;
			drawGeoConsent();
		}

		drawSavedCoupons();



	}
}
