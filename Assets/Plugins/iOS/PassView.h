//
//  PassView.h
//  Unity-iPhone
//
//  Created by Nick Hatter on 09/03/2014.
//
//

#import <UIKit/UIKit.h>
#import "PassKit/PassKit.h"

@interface PassViewPlugin : UIViewController

@property (strong, nonatomic) PKPassLibrary *passLib;

@end
