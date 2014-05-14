﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FacebookIntegration : MonoBehaviour {
	private string lastResponse = "";

	public bool isInit = false;

	public Texture2D sharingScreenshot;
	
	public void CallFBInit()
	{
		FB.Init(OnInitComplete, OnHideUnity);
	}
	
	public void OnInitComplete()
	{
		Debug.Log("FB.Init completed: Is user logged in? " + FB.IsLoggedIn);
		isInit = true;
		CallFBLogin();
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
}
