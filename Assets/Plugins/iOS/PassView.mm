/*
 * Copyright (C) 2014 giftgaming Ltd
 *
 * Author: Nick Hatter
 *
 */

#import "PassView.h"
#import <UIKit/UIKit.h>
#import <PassKit/PassKit.h> //1

extern UIViewController *UnityGetGLViewController();
extern "C" void UnitySendMessage(const char *, const char *, const char *);

@interface PassViewPlugin()
{
    
}
@end

@implementation PassViewPlugin

- (void)viewDidLoad
{
    [super viewDidLoad];
	// Do any additional setup after loading the view, typically from a nib.
    _passLib = [[PKPassLibrary alloc] init];
}

// Note: passURL is useless at the moment and the coupon path
// is hardcoded for now.
-(void)showPass:(NSString*)passURL
{
	if (![PKPassLibrary isPassLibraryAvailable]) {
        [[[UIAlertView alloc] initWithTitle:@"Error"
                                    message:@"PassKit not available"
                                   delegate:nil
                          cancelButtonTitle:@"Pitty"
                          otherButtonTitles: nil] show];
        return;
    }
    
    NSString* passFile = [[[NSBundle mainBundle] resourcePath]
                          stringByAppendingPathComponent: @"../Documents/giftgaming_coupon.pkpass"];
    
    NSLog(@"passURL is %@ \n", passFile);
    NSData *passData = [NSData dataWithContentsOfFile: passFile];
    
    NSError* error = nil;
    PKPass *pass = [[PKPass alloc] initWithData:passData
                                          error:&error];
    
    //check if pass library is available
    if (![PKPassLibrary isPassLibraryAvailable])
    {
        UIAlertView* alertView = [[UIAlertView alloc] initWithTitle:@"PassBook Unavailable" message:@"PassBook does not seem to be installed. Update to latest iOS." delegate:nil cancelButtonTitle:@"OK" otherButtonTitles:nil];
        [alertView show];
        
        return;
    }
    
    // Present window
    PKAddPassesViewController *vc = [[PKAddPassesViewController alloc] initWithPass:pass];
    [UnityGetGLViewController().view addSubview:vc.view];
    
    [UnityGetGLViewController() presentViewController:vc animated:YES completion:nil];
}

#pragma mark - Pass controller delegate

-(void)addPassesViewControllerDidFinish: (PKAddPassesViewController*) controller
{
    //pass added
    [self dismissViewControllerAnimated:YES completion:nil];
}
@end

extern "C" {
	void *_PassViewPlugin_Init();
	void _PassViewPlugin_Destroy(void *instance);
    void _PassViewPlugin_ShowPass(void *instance, NSString* passURL);
}

void *_PassViewPlugin_Init()
{
	id instance = [PassViewPlugin alloc];
	return (void *)instance;
}

void _PassViewPlugin_Destroy(void *instance)
{
	PassViewPlugin *passViewPlugin = (PassViewPlugin *)instance;
	[passViewPlugin release];
}

void _PassViewPlugin_ShowPass(void *instance, NSString *passURL)
{
	PassViewPlugin *passViewPlugin = (PassViewPlugin *)instance;
	[passViewPlugin showPass:passURL];
}