//
//  SKProduct+LocalisedPrice.m
//  Unity-iPhone
//
//  Created by Nick Hatter on 11/05/2014.
//
//

#import "SKProduct+LocalisedPrice.h"

@implementation SKProduct (LocalisedPrice)

- (NSString *)localisedPrice
{
    NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
    [numberFormatter setFormatterBehavior:NSNumberFormatterBehavior10_4];
    [numberFormatter setNumberStyle:NSNumberFormatterCurrencyStyle];
    [numberFormatter setLocale:self.priceLocale];
    NSString *formattedString = [numberFormatter stringFromNumber:self.price];
    [numberFormatter release];
    return formattedString;
}

@end