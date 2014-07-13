/*
 * Copyright (C) 2014 giftgaming Ltd
 *
 */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Callback = System.Action<string>;

public class PassViewObject : MonoBehaviour {
	Callback callback;

	#if UNITY_EDITOR || UNITY_STANDALONE_OSX
		IntPtr passView;
	#elif UNITY_IPHONE
		IntPtr passView;
	#elif UNITY_ANDROID
		AndroidJavaObject passView;
	#elif UNITY_WEBPLAYER
	#endif
	
	public bool IsKeyboardVisible {
		get {
			#if UNITY_ANDROID && !UNITY_EDITOR
				return false;
			#else
				return TouchScreenKeyboard.visible;
			#endif
		}
	}

	#if UNITY_EDITOR || UNITY_STANDALONE_OSX
		[DllImport("PassView")]
		private static extern IntPtr _PassViewPlugin_Init();
		[DllImport("PassView")]
		private static extern int _PassViewPlugin_Destroy(IntPtr instance);
		[DllImport("PassView")]
		private static extern int _PassViewPlugin_ShowPass(IntPtr instance, string url);
	#elif UNITY_IPHONE
		[DllImport("__Internal")]
		private static extern IntPtr _PassViewPlugin_Init();
		[DllImport("__Internal")]
		private static extern int _PassViewPlugin_Destroy(IntPtr instance);
		[DllImport("__Internal")]
		private static extern int _PassViewPlugin_ShowPass(IntPtr instance, string urlL);
	#endif

	public void Init() {
		#if UNITY_EDITOR || UNITY_STANDALONE_OSX
			//passView = _PassViewPlugin_Init();
		#elif UNITY_IPHONE
			passView = _PassViewPlugin_Init();
		#elif UNITY_ANDROID
			passView = new AndroidJavaObject("com.giftgaming.PassViewPlugin");
			passView.Call("Init", name);
		#elif UNITY_WEBPLAYER
			Application.ExternalCall("unityPassView.init", name);
		#endif
	}

	public void ShowPass(String passURL) {
		#if UNITY_EDITOR || UNITY_STANDALONE_OSX
			_PassViewPlugin_ShowPass(passView, passURL);
		#elif UNITY_IPHONE
			_PassViewPlugin_ShowPass(passView, passURL);
		#elif UNITY_ANDROID
			passView.Call("ShowPass", passURL);
		#elif UNITY_WEBPLAYER
			Application.ExternalCall("unityPassView.showPass", passURL);
		#endif
	}

	void OnDestroy() {
		#if UNITY_EDITOR || UNITY_STANDALONE_OSX
			if (passView == IntPtr.Zero)
				return;
			_PassViewPlugin_Destroy(passView);
		#elif UNITY_IPHONE
			if (passView == IntPtr.Zero)
				return;
			_PassViewPlugin_Destroy(passView);
		#elif UNITY_ANDROID
			if (passView == null)
				return;
			passView.Call("Destroy");
		#elif UNITY_WEBPLAYER
			Application.ExternalCall("unityPassView.destroy", name);
		#endif
	}

	#if UNITY_EDITOR || UNITY_STANDALONE_OSX
	void OnGUI() {
		if (passView == IntPtr.Zero)
			return;

		GL.IssuePluginEvent((int)passView);
	}
	#endif
}
