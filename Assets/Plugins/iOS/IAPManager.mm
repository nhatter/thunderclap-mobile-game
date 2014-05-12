//
//  IAPManager.m
//  Unity-iPhone
//
//  Created by Nick Hatter on 11/05/2014.
//
//

#import "IAPManager.h"

extern UIViewController *UnityGetGLViewController();
extern "C" void UnitySendMessage(const char *, const char *, const char *);

@interface IAPManager()
{
    
}
@end

@implementation IAPManager

#define kInAppPurchaseProUpgradeProductId @"3_UMBRELLAS"

#pragma mark -
#pragma mark SKProductsRequestDelegate methods

- (void)productsRequest:(SKProductsRequest *)request didReceiveResponse:(SKProductsResponse *)response
{
    NSLog(@"productsRequest %@", @"Being called\n");
    for (SKProduct *product in response.products) {
        NSLog(@"Product: %@", product.productIdentifier);
        SKPayment *payment = [SKPayment paymentWithProduct:product];
        [[SKPaymentQueue defaultQueue] addPayment:payment];
    }
    
}

//
// call this method once on startup
//
- (void)loadStore
{
    // restarts any purchases if they were interrupted last time the app was open
    [[SKPaymentQueue defaultQueue] addTransactionObserver:self];
}

//
// call this before making a purchase
//
- (BOOL)canMakePurchases
{
    return [SKPaymentQueue canMakePayments];
}

//
// kick off the upgrade transaction
//
- (void)purchase: (NSString*)productID
{
    NSSet *productIdentifiers = [NSSet setWithObject:productID];
    productsRequest = [[SKProductsRequest alloc] initWithProductIdentifiers:productIdentifiers];
    productsRequest.delegate = self;
    [productsRequest start];
}

#pragma -
#pragma Purchase helpers

//
// saves a record of the transaction by storing the receipt to disk
//
- (void)recordTransaction:(SKPaymentTransaction *)transaction
{
    if ([transaction.payment.productIdentifier isEqualToString:kInAppPurchaseProUpgradeProductId])
    {
        // save the transaction receipt to disk
        [[NSUserDefaults standardUserDefaults] setValue:[NSData dataWithContentsOfURL:[[NSBundle mainBundle] appStoreReceiptURL]] forKey:@"proUpgradeTransactionReceipt" ];
        [[NSUserDefaults standardUserDefaults] synchronize];
    }
}

//
// enable pro features
//
- (void)provideContent:(NSString *)productId
{
    if ([productId isEqualToString:kInAppPurchaseProUpgradeProductId])
    {
        // enable the pro features
        [[NSUserDefaults standardUserDefaults] setBool:YES forKey:@"isProUpgradePurchased" ];
        [[NSUserDefaults standardUserDefaults] synchronize];
    }
}

//
// removes the transaction from the queue and posts a notification with the transaction result
//
- (void)finishTransaction:(SKPaymentTransaction *)transaction wasSuccessful:(BOOL)wasSuccessful
{
    // remove the transaction from the payment queue.
    [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
    
    NSDictionary *userInfo = [NSDictionary dictionaryWithObjectsAndKeys:transaction, @"transaction" , nil];
    if (wasSuccessful)
    {
        NSLog(@"Transaction status: %@" , @"was successful\n");
        // send out a notification that we’ve finished the transaction
        [[NSNotificationCenter defaultCenter] postNotificationName:kInAppPurchaseManagerTransactionSucceededNotification object:self userInfo:userInfo];
    }
    else
    {
        NSLog(@"Transaction status: %@" , @"failed\n");
        // send out a notification for the failed transaction
        [[NSNotificationCenter defaultCenter] postNotificationName:kInAppPurchaseManagerTransactionFailedNotification object:self userInfo:userInfo];
    }
}

//
// called when the transaction was successful
//
- (void)completeTransaction:(SKPaymentTransaction *)transaction
{
    [self recordTransaction:transaction];
    [self provideContent:transaction.payment.productIdentifier];
    [self finishTransaction:transaction wasSuccessful:YES];
}

//
// called when a transaction has been restored and and successfully completed
//
- (void)restoreTransaction:(SKPaymentTransaction *)transaction
{
    [self recordTransaction:transaction.originalTransaction];
    [self provideContent:transaction.originalTransaction.payment.productIdentifier];
    [self finishTransaction:transaction wasSuccessful:YES];
}

//
// called when a transaction has failed
//
- (void)failedTransaction:(SKPaymentTransaction *)transaction
{
    if (transaction.error.code != SKErrorPaymentCancelled)
    {
        // error!
        [self finishTransaction:transaction wasSuccessful:NO];
    }
    else
    {
        // this is fine, the user just cancelled, so don’t notify
        [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
    }
}

#pragma mark -
#pragma mark SKPaymentTransactionObserver methods

//
// called when the transaction status is updated
//
- (void)paymentQueue:(SKPaymentQueue *)queue updatedTransactions:(NSArray *)transactions
{
    for (SKPaymentTransaction *transaction in transactions)
    {
        switch (transaction.transactionState)
        {
            case SKPaymentTransactionStatePurchased:
                NSLog(@"Transaction status: %@" , @"purchased\n");
                [self completeTransaction:transaction];
                break;
            case SKPaymentTransactionStateFailed:
                NSLog(@"Transaction status: %@" , @"failed\n");
                [self failedTransaction:transaction];
                break;
            case SKPaymentTransactionStateRestored:
                NSLog(@"Transaction status: %@" , @"restored\n");
                [self restoreTransaction:transaction];
                break;
            default:
                break;
        }
    }
}
@end

extern "C" {
	void *_IAPManager_Init();
	void _IAPManager_Destroy(void *instance);
    void _IAPManager_Purchase(void *instance, const char *productID);
    void _IAPManager_CanMakePurchases(void *instance);
}

void *_IAPManager_Init()
{
	id instance = [IAPManager alloc];
	return (void *)instance;
}

void _IAPManager_CanMakePurchases(void *instance)
{
	IAPManager *iap = (IAPManager *)instance;
	[iap canMakePurchases];
}

void _IAPManager_Purchase(void *instance, const char *productID)
{
    NSString *productIDString = [NSString stringWithUTF8String:productID];
	IAPManager *iap = (IAPManager *)instance;
	[iap purchase:productIDString];
}

void _IAPManager_Destroy(void *instance)
{
	IAPManager *iap = (IAPManager *)instance;
	[iap release];
}

