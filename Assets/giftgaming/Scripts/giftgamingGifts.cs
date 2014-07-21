using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class giftgamingGifts : MonoBehaviour {
	public static void openNextGift() {
		giftgaming.use.nextGift();
		giftgaming.use.openGift();

		/** Implement your own custom actions here for gift codes
		 * 
		 * Example:
		 * 
		 * Flashy.use.unlockIAP(giftgaming.use.currentGift.giftCode);
		 * 
		 * // Set icon to use when showing the user what gift they got
		 * // Note: you probably want a different logo for each gift
		 * giftgaming.use.setInGameGiftLogo(Flashy.use.umbrellaIcon);
		 * 
		 */
	}

	/* Description functions:
	 * In case you wanted to explain what the gift does for the player
	 * 
	 */
	public static string getGiftDescription() {
		return getGiftDescription(giftgaming.use.currentGift.giftCode);
	}

	public static string getGiftDescription(string giftCode) {
		switch(giftCode) {
		case "SWEET_ZAPPER":
			return "A SWEET ZAPPER\nZaps one kind of sweet. Click on the kind of sweet you want zapped then click the SWEET ZAPPER button!";
			break;

			default:
			return "";
		}
	}
}
