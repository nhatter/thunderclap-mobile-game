using UnityEngine;
using SimpleJSON;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facebook.MiniJSON;


public class FacebookIntegration : MonoBehaviour {
	private string lastResponse = "";

	public bool isInit = false;

	public Texture2D sharingScreenshot;

	public Dictionary<string, int> friendScores = new Dictionary<string, int>();

	public List<object>                 scores          = null;
	public Dictionary<string, Texture>  friendImages    = new Dictionary<string, Texture>();

	public Player player;
	
	public void CallFBInit()
	{
		FB.Init(OnInitComplete, OnHideUnity);
	}
	
	public void OnInitComplete()
	{
		Debug.Log("FB.Init completed: Is user logged in? " + FB.IsLoggedIn);
		isInit = true;
	}
	
	public void OnHideUnity(bool isGameShown)
	{
		Debug.Log("Is game showing? " + isGameShown);
	}
	
	public void CallFBLogin()
	{
		FB.Login("public_profile,publish_actions", LoginCallback);
	}
	
	void LoginCallback(FBResult result)
	{
		if (result.Error != null)
			lastResponse = "Error Response:\n" + result.Error;
		else if (!FB.IsLoggedIn)
		{
			lastResponse = "Login cancelled by Player";
		}
		else
		{
			lastResponse = "Login was successful!";
		}
	}
	
	public void CallFBLogout()
	{
		FB.Logout();
	}

	public void getScreenshot(Action callback) {
		StartCoroutine(TakeScreenshot(callback));
	}

	public void shareScreenshot(string comment, Action callback) {
		StartCoroutine(UploadScreenshot(comment, callback));
	}

	private IEnumerator TakeScreenshot(Action callback) 
	{
		yield return new WaitForEndOfFrame();

		sharingScreenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
		// Read screen contents into the texture
		sharingScreenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
		sharingScreenshot.Apply();

		callback();

		yield return true;
	}

	private IEnumerator UploadScreenshot(string comment, Action callback) 
	{
		byte[] screenshot = sharingScreenshot.EncodeToPNG();

		var wwwForm = new WWWForm();
		wwwForm.AddBinaryData("image", screenshot, "Screenshot.png");
		wwwForm.AddField("name", comment);
		
		FB.API("me/photos", Facebook.HttpMethod.POST, delegate(FBResult r) { Debug.Log("Result of sharing screenshot: " + r.Text); }, wwwForm);

		callback();

		yield return true;
	}

	public void QueryScores()
	{
		FB.API("/app/scores?fields=score,user.limit(20)", Facebook.HttpMethod.GET, ScoresCallback);
	}

	void ScoresCallback(FBResult result) 
	{
		if (result.Error != null)
		{
			Debug.Log(result.Error);
			return;
		}
		
		scores = new List<object>();
		List<object> scoresList = Util.DeserializeScores(result.Text);
		
		foreach(object score in scoresList) 
		{
			var entry = (Dictionary<string,object>) score;
			var user = (Dictionary<string,object>) entry["user"];
			
			string userId = (string)user["id"];

			int playerHighScore = getScoreFromEntry(entry);
			Util.Log("Local players score on server is " + playerHighScore);
			if (playerHighScore < player.bestScore)
			{
				Util.Log("Locally overriding with just acquired score: " + player.bestScore);
				playerHighScore = player.bestScore;
			}
			
			entry["score"] = playerHighScore.ToString();

			scores.Add(entry);
			if (!friendImages.ContainsKey(userId))
			{
				// We don't have this players image yet, request it now
				LoadPicture(Util.GetPictureURL(userId, 128, 128),pictureTexture =>
				            {
					if (pictureTexture != null)
					{
						friendImages.Add(userId, pictureTexture);
					}
				});
			}
		}
		
		// Now sort the entries based on score
		scores.Sort(delegate(object firstObj,
		                     object secondObj)
		            {
			return -getScoreFromEntry(firstObj).CompareTo(getScoreFromEntry(secondObj));
		}
		);
	}

	private int getScoreFromEntry(object obj)
	{
		Dictionary<string,object> entry = (Dictionary<string,object>) obj;
		return Convert.ToInt32(entry["score"]);
	}

	public static void FriendPictureCallback(Texture texture)
	{

	}
	
	delegate void LoadPictureCallback (Texture texture);

	IEnumerator LoadPictureEnumerator(string url, LoadPictureCallback callback)    
	{
		WWW www = new WWW(url);
		yield return www;
		callback(www.texture);
	}
	void LoadPicture (string url, LoadPictureCallback callback)
	{
		FB.API(url,Facebook.HttpMethod.GET,result =>
		       {
			if (result.Error != null)
			{
				Debug.Log(result.Error);
				return;
			}
			
			var imageUrl = Util.DeserializePictureURLString(result.Text);
			
			StartCoroutine(LoadPictureEnumerator(imageUrl,callback));
		});
	}
}
