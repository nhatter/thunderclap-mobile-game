using UnityEngine;
using SimpleJSON;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class giftgaming : MonoBehaviour {
	// Assigned from giftgaming.com
	// Make sure you set this to the correct ID
	public GUISkin giftgamingSkin;
	public GUISkin gameSkin;

	public string API_ENDPOINT = "";
	public string API_KEY = "(From Manage Games on dashboard.giftgaming.com)";

	// How often to check for gifts
	public int CHECK_FOR_GIFT_INTERVAL = 2;

	public static giftgaming use;
	string GIFTS_URL;
	public bool isCouponReminderSet = true;
	public bool isShowingNotInterested = false;
	
	string PLAYER_XML_FILE;
	string DATA_PATH;
	string COUPON_CACHE_FILE;

	public Texture2D giftIcon;
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
	Rect TOP_RIGHT;
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

	string passbookButtonText = "Save to Passbook";
	bool isSavingCoupon = false;

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
		listStyle.font = giftgamingSkin.window.font;
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

		GIFTS_URL = "http://"+API_ENDPOINT+":8080/api/ads";
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

		GUIStyle giftButtonStyle = giftgamingSkin.GetStyle("giftButton");
		
		BOTTOM_RIGHT = new Rect(Screen.width - giftButtonStyle.fixedWidth, Screen.height - giftButtonStyle.fixedHeight, giftButtonStyle.fixedWidth, giftButtonStyle.fixedHeight);
		TOP_RIGHT = new Rect(Screen.width - giftButtonStyle.fixedWidth, 0, giftButtonStyle.fixedWidth, giftButtonStyle.fixedHeight);

		couponGeoReminder.text = "            Remind me to use it near: ";
		couponGeoReminder.image = couponGeoReminderImage;

		// Only attempt to load player file after all the paths have been setup
		getggPlayerID();

		use = this;
	}

	public void setInGameGift(GameObject inGameGift) {
		this.inGameGift = inGameGift;
		inGameGiftLogo = (Texture2D) inGameGift.GetComponentInChildren<Renderer>().material.GetTexture(0);
		GUIStyle giftContentsStyle = giftgamingSkin.GetStyle("GiftContents");
		wrappedGiftRect = new Rect(Screen.width/2 - giftContentsStyle.fixedWidth/2, 150 + Screen.height/2 - giftContentsStyle.fixedHeight/2, giftContentsStyle.fixedWidth, giftContentsStyle.fixedHeight);
	}

	public void setInGameGiftLogo(Texture2D inGameGiftTexture) {
		inGameGiftLogo = inGameGiftTexture;
		GUIStyle giftContentsStyle = giftgamingSkin.GetStyle("GiftContents");
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

		string createggPlayerJSON = "{\"action\":\"newPlayerID\", \"API_KEY\":\""+API_KEY+"\"}";
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

		string requestGiftJson = "{\"action\":\"requestGift\", \"playerID\":\""+playerID+"\"}";
		Debug.Log(requestGiftJson);

		WWW postJsonResponse = postJson(requestGiftJson);
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
					Gift newGift = new Gift(giftResponse["giftName"], giftResponse["giftCode"], giftResponse["giftKey"], brandLogo, couponLogo);
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
						isSavingCoupon = false;
						passbookButtonText = "Save to Passbook";
						
						currentGift.isCouponRedeemed = true;

						#if UNITY_IPHONE && !UNITY_EDITOR
							passViewObject = (new GameObject("PassViewObject")).AddComponent<PassViewObject>();
							passViewObject.Init();
							passViewObject.ShowPass(COUPON_CACHE_FILE);
						#endif
					break;

					case "GIFT_CLOSED":
						Debug.Log("Gift closed successfully");
						isSavingCoupon = false;
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

	// Please do not share this with anyone without our permission -thanks
	public string getBasicAuthString() {
		return "Basic dGVzdGVyOkdvb2RHYW1lQWxs";
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
				isSavingCoupon = true;
				passbookButtonText = "Downloading...";

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
	
	public string getGiftCouponSummary() {
		return currentGift.couponURL;
	}

	public int getHumanggPlayerID() {
		return playerID;
	}
	
	public WWW postJson(string json) {
		Hashtable postHeader = new Hashtable();
		postHeader.Add("Content-Type", "application/json");
		postHeader.Add("Authorization", getBasicAuthString());

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
				if(isScreenLandscape()) {
					currentGift.drawBrandLogo(Screen.width-currentGift.couponLogo.width);
				} else {
					currentGift.drawBrandLogo();
				}
			}
		}
	}

	public void drawGiftLogo() {
		if(inGameGiftLogo != null) {
			GUILayout.BeginHorizontal();
				GUILayout.Box(inGameGiftLogo, "GiftContents");
				GUILayout.BeginVertical();
					GUILayout.Label(currentGift.giftName);
					GUILayout.Label(" + a special discount");
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
			GUI.skin = giftgamingSkin;

			GUILayout.BeginVertical(GUILayout.Width(Screen.width), GUILayout.Height(Screen.height));

				if(isScreenLandscape()) {
					GUILayout.BeginVertical();
						GUILayout.FlexibleSpace();

						GUILayout.BeginHorizontal();
							GUILayout.BeginVertical(GUILayout.Width(Screen.width-currentGift.couponLogo.width));
								drawBrandLogo();
								GUILayout.Label ("HAS GIVEN YOU A GIFT!", "giftAnnouncement");
								GUILayout.Space(10);
									GUILayout.BeginHorizontal();
										GUILayout.FlexibleSpace();
										drawGiftLogo();
										GUILayout.FlexibleSpace();
									GUILayout.EndHorizontal();
								GUILayout.FlexibleSpace();
							GUILayout.EndVertical();

							drawCouponLogo();
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
							drawGiftControls();
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
							GUILayout.FlexibleSpace();
								drawSmallPrint();
							GUILayout.FlexibleSpace();
						GUILayout.EndHorizontal();

						GUILayout.FlexibleSpace();

					GUILayout.EndVertical();
				} else {
					GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));
						GUILayout.FlexibleSpace();
							drawBrandLogo();
						GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
						GUILayout.Label ("HAS GIVEN YOU A GIFT!", "giftAnnouncement");
					GUILayout.EndHorizontal();

					GUILayout.Space(4);
					drawGiftLogo();
					
					GUILayout.BeginHorizontal();
						GUILayout.Space(5);
						drawCouponLogo();
					GUILayout.EndHorizontal();

					GUILayout.Space(5);
					drawGiftControls();
					
					drawSmallPrint();
				}

			GUILayout.EndVertical();
		}
	}

	public void drawGiftControls() {

		GUI.skin = gameSkin;
			GUILayout.BeginHorizontal();
				drawCloseGiftButton();
				drawSaveCouponButton();	
			GUILayout.EndHorizontal();
		GUI.skin = giftgamingSkin;

	}

	public void drawCloseGiftButton() {
		if(GUILayout.Button("Close", GUILayout.Width(Screen.width-currentGift.couponLogo.width))) {
			if(!player.hasGivenPreferences) {
				isShowingNotInterested = true;
			} else {
				closeGift();
			}
		}
	}

	public void drawSaveCouponButton() {
		#if UNITY_IPHONE
			if(isSavingCoupon) {
				GUI.enabled = false;
			}
			
			if(GUILayout.Button(passbookButtonText, GUILayout.Width(currentGift.couponLogo.width) )) {
				if(!player.hasBeenOfferedGeoConsent) {
					isDisplayingGeoConsent = true;
				} else {
					redeemCoupon();
				}
			}
			
		GUI.enabled = true;
		#else
			if(GUILayout.Button("", "SaveCoupon")) {
				redeemCoupon();
				closeGift();
			}
		#endif
	}

	public bool isScreenLandscape() {
		return Screen.orientation == ScreenOrientation.LandscapeLeft || Screen.orientation == ScreenOrientation.LandscapeRight || true;
	}

	public void BeginVerticalIfLandscape() {
		if(isScreenLandscape()) {
			GUILayout.BeginVertical(GUILayout.Height(giftgamingSkin.GetStyle("AddToPassbook").fixedHeight));
		} else {
			GUILayout.BeginHorizontal(GUILayout.Height(giftgamingSkin.GetStyle("AddToPassbook").fixedHeight));
		}
	}

	public void EndVerticalIfLandscape() {
		if(isScreenLandscape()) {
			GUILayout.EndVertical();
		} else {
			GUILayout.EndHorizontal();
		}
	}

	public void drawSmallPrint() {
		GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("DELIVERED BY ", GUILayout.MinWidth(180));
				GUILayout.Box(giftgamingLogo, "Logo", GUILayout.Height(60));
				GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			//GUILayout.Space(5);
			GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("giftgaming® privacy policy available at: www.giftgaming.com/privacy\n"
		             	      + "Location-based reminders powered by FOURSQUARE\n", "SmallPrint");
				GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}

	Color originalGUIColor;
	public void drawSavedCoupons() {
		if(isViewingCoupons()) {
			GUI.enabled = true;

			GUI.Box(ENTIRE_SCREEN, "", "Overlay");

			GUI.skin = gameSkin;
			GUILayout.Label("Saved Coupons - Tap to use them");
			if(GUILayout.Button("Close Window")) {
				hideCoupons();
			}
			GUI.skin = giftgamingSkin;

			if(openedCoupon!=null) {
				GUI.enabled = false;
			}

			couponScrollPosition = GUILayout.BeginScrollView(couponScrollPosition, GUILayout.Width (Screen.width));
				GUILayout.BeginVertical();
					
					
					displayCoupons();
					
				GUILayout.EndVertical();
			GUILayout.EndScrollView();

			if(openedCoupon != null) {
				GUI.enabled = true;
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

					GUI.enabled = true;
					if(GUILayout.Button("Use Coupon Online")) {
						Application.OpenURL(openedCoupon.storeLink);
					}

					if(GUILayout.Button("Terms and Conditions")) {
						isDisplayingCouponTerms = true;
					}

					if(GUILayout.Button("Close Coupon")) {
						openedCoupon = null;
						Debug.Log("Close coupon");
					}
					GUI.skin = giftgamingSkin;
					
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
						GUI.color = originalGUIColor;
						if(GUILayout.Button("Close Terms")) {
							isDisplayingCouponTerms = false;
						}
						GUI.skin = giftgamingSkin;
					GUILayout.EndArea();
				}

				GUI.color = originalGUIColor;
			}



		}// End of check for if viewing coupons
	}

	int genderIndex = 0;
	string[] genders = {"Male", "Female"};

	int ageRangeIndex = 0;
	string[] ageRanges = {"Under 18", "18 - 30", "31 - 45", "46 - 60", "60+"};


	public void drawNotInterested() {
		GUI.skin = gameSkin;
		GUILayout.BeginVertical("GiftWindow", GUILayout.Width(Screen.width), GUILayout.Height(Screen.height));
			
			GUILayout.Label("Help giftgaming give better coupons", GUILayout.Height(100));

			GUILayout.Label("Gender", GUILayout.Height(100));
			genderIndex = GUILayout.SelectionGrid(genderIndex, genders, 2);
		
			GUILayout.Label("Age Range",GUILayout.Height(100));
			ageRangeIndex = GUILayout.SelectionGrid(ageRangeIndex, ageRanges, 2);

			if(GUILayout.Button("Save my preferences")) {
				isShowingNotInterested = false;
				player.hasGivenPreferences = true;
				StartCoroutine(saveggPlayerPreferences(genderIndex, ageRangeIndex));
			}
			
		GUILayout.EndVertical();
		GUI.skin = giftgamingSkin;
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
					GUI.skin = giftgamingSkin;
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

	public void reviewCouponsButton() {
		#if !UNITY_IPHONE
		if(GUILayout.Button("giftgaming® Coupons")) {
			requestCoupons();
		}
		#endif
	}

	public void prepareOverlay() {
		if(giftgaming.use.isViewingCoupons()) {
			// Disable other game GUI components
			GUI.enabled = false;
		}
	}

	public void OnGUI() {
		GUI.skin = giftgamingSkin;

		if(isDisplayingGeoConsent) {
			GUI.enabled = false;
		} else {
			GUI.enabled = true;
		}

		GIFTS_URL = "http://"+API_ENDPOINT+":8080/api/ads";

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
	}
}
