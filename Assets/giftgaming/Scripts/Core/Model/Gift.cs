using UnityEngine;
using System.Collections;

public class Gift {
	public string giftCode;
	public string giftKey;
	public Texture2D brandLogo;
	public Texture2D couponLogo;
	public string messageToPlayer;
	public string couponURL;

	public bool isGiftOpened = false;
	public bool isCouponRedeemed = false;
	public bool isLiked = false;
	public bool isFollowed = false;
	public bool isTweeted = false;
	public bool isShared = false;
	public bool isAwarded = false;
	
	public float giftReceiveTimeStamp = 0;
	public float giftOpenTimeStamp = 0;
	public float giftOpenTime = 0;
	public float giftCloseTime = 0;
	public float redeemCouponTime = 0;

	public Gift(string giftCode, string giftKey, Texture2D brandLogo, string messageToPlayer, Texture2D couponLogo) {
		this.giftCode = giftCode;
		this.giftKey = giftKey;
		this.brandLogo = brandLogo;
		this.couponLogo = couponLogo;
		this.messageToPlayer = messageToPlayer;
		giftReceiveTimeStamp = Time.time;
	}

	public void drawBrandLogo() {
		if(brandLogo != null) {
			GUILayout.Box(brandLogo
				#if UNITY_EDITOR
			    	, GUILayout.Height(50)
				#endif
			);
		}
	}

	public void drawCouponLogo() {
		if(couponLogo != null) {
			GUILayout.Box(couponLogo
	              #if UNITY_EDITOR
	             	 , GUILayout.Height(100)
	              #endif
			);
		}
	}
}
