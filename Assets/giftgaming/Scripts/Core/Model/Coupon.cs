using UnityEngine;
using System.Collections;

public class Coupon {
	public string publicURL = "";
	public string brand = "";
	public Texture2D logo;
	public string summary = "";
	public string giftCode = "";
	public string storeLink = "";
	public string couponTerms = "";
	public Color backgroundColor;
	public Color foregroundColor;

	public Coupon() {
	}

	public Coupon(string publicURL, string brand, Texture2D logo, string summary, string giftCode, string storeLink,
	              string couponTerms, string backgroundColor, string foregroundColor) {
		this.publicURL = publicURL;
		this.logo = logo;
		this.summary = summary;
		this.giftCode = giftCode;
		this.storeLink = storeLink;
		this.couponTerms = couponTerms;
		this.backgroundColor = hexToColor(backgroundColor);
		this.foregroundColor = hexToColor(foregroundColor);
	}

	Color hexToColor(string hex) {

		float red 	= (float) byte.Parse(hex.Substring(1,2), System.Globalization.NumberStyles.HexNumber);
		float green = (float) byte.Parse(hex.Substring(3,2), System.Globalization.NumberStyles.HexNumber);
		float blue 	= (float) byte.Parse(hex.Substring(5,2), System.Globalization.NumberStyles.HexNumber);
		Debug.Log("Color: "+red+","+green+","+blue);
		return new Color(red/255.0f, green/255.0f, blue/255.0f, 1.0f);
	}
}
