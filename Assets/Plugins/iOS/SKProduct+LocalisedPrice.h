//
//  SKProduct+LocalisedPrice.h
//  Unity-iPhone
//
//  Created by Nick Hatter on 11/05/2014.
//
//

#import <Foundation/Foundation.h>
#import <StoreKit/StoreKit.h>

@interface SKProduct (LocalisedPrice)

@property (nonatomic, readonly) NSString *localisedPrice;

@end