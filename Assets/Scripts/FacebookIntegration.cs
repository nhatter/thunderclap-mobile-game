using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FacebookIntegration {
	private string lastResponse = "";
	#region FB.Init() example
	
	public bool isInit = false;
	
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
	
	#endregion
	
	#region FB.Login() example
	
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
	#endregion
}
