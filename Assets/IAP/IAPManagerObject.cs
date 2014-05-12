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

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
public class UnitySendMessageDispatcher {
	public static void Dispatch(string name, string method, string message) {
		GameObject obj = GameObject.Find(name);
		if (obj != null)
			obj.SendMessage(method, message);
	}
}
#endif

public class IAPManagerObject : MonoBehaviour {
	Callback callback;

	#if UNITY_EDITOR || UNITY_STANDALONE_OSX
		IntPtr iap;
	#elif UNITY_IPHONE
		IntPtr iap;
	#elif UNITY_ANDROID
		AndroidJavaObject iap;
	#elif UNITY_WEBPLAYER
	#endif
	
	public bool IsKeyboardVisible {
		get {
			#if UNITY_ANDROID && !UNITY_EDITOR
				return mIAPManager;
			#else
				return TouchScreenKeyboard.visible;
			#endif
		}
	}

	#if UNITY_EDITOR || UNITY_STANDALONE_OSX
		[DllImport("IAPManager")]
		private static extern IntPtr _IAPManager_Init();
		[DllImport("IAPManager")]
		private static extern int _IAPManager_Destroy(IntPtr instance);
		[DllImport("IAPManager")]
		private static extern int _IAPManager_Purchase(IntPtr instance, string productID);
		[DllImport("IAPManager")]
		private static extern int _IAPManager_CanMakePurchases(IntPtr instance);
	#elif UNITY_IPHONE
		[DllImport("__Internal")]
		private static extern IntPtr _IAPManager_Init();
		[DllImport("__Internal")]
		private static extern int _IAPManager_Destroy(IntPtr instance);
		[DllImport("__Internal")]
		private static extern int _IAPManager_Purchase(IntPtr instance, string productID);
		[DllImport("__Internal")]
		private static extern int _IAPManager_CanMakePurchases(IntPtr instance);
	#endif

	public void Init() {
		#if UNITY_EDITOR || UNITY_STANDALONE_OSX
			iap = _IAPManager_Init();
		#elif UNITY_IPHONE
			iap = _IAPManager_Init();
		#elif UNITY_ANDROID
			iap = new AndroidJavaObject("com.giftgaming.IAPManager");
			iap.Call("Init", name);
		#elif UNITY_WEBPLAYER
			Application.ExternalCall("unityIAP.init", name);
		#endif
	}

	public void Purchase(String productID) {
		#if UNITY_EDITOR || UNITY_STANDALONE_OSX
			_IAPManager_Purchase(iap, productID);
		#elif UNITY_IPHONE
			_IAPManager_Purchase(iap, productID);
		#elif UNITY_ANDROID
			iap.Call("Purchase", productID);
		#elif UNITY_WEBPLAYER
			Application.ExternalCall("unityIAP.Purchase", productID);
		#endif
	}

	void OnDestroy() {
		#if UNITY_EDITOR || UNITY_STANDALONE_OSX
			if (iap == IntPtr.Zero)
				return;
			//_IAPManager_Destroy(iap);
		#elif UNITY_IPHONE
			if (iap == IntPtr.Zero)
				return;
			_IAPManager_Destroy(iap);
		#elif UNITY_ANDROID
			if (iap == null)
				return;
			iap.Call("Destroy");
		#elif UNITY_WEBPLAYER
			Application.ExternalCall("unityIAP.destroy", name);
		#endif
	}

	#if UNITY_EDITOR || UNITY_STANDALONE_OSX
	void OnGUI() {
		if (iap == IntPtr.Zero)
			return;

		GL.IssuePluginEvent((int)iap);
	}
	#endif
}
