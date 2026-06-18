# Push Notifications

Darwin sends campaign push notifications through an external HTTP gateway. WebApi owns entitlement, inbox, delivery queue, retry state, and token hygiene. The gateway owns the provider-specific FCM/APNS call.

## Required Configuration

Configure WebApi under `Notifications:PushGateway`:

```json
{
  "Enabled": true,
  "BaseUrl": "https://push-gateway.example.com",
  "Endpoint": "/api/push/send",
  "ApiKey": "<secret from environment>",
  "Provider": "Fcm",
  "TimeoutSeconds": 15,
  "MaxAttempts": 2,
  "InitialBackoffMilliseconds": 250,
  "AndroidChannelId": "campaigns",
  "ApnsTopic": "com.loyan.darwin.mobile.consumer"
}
```

Keep `ApiKey` out of source control. In development the section may exist with `Enabled=false` and an empty key.

## Gateway Request

WebApi sends one request per target device:

```json
{
  "notificationId": "9fd01860-6c72-4cf6-b5ef-64ce7d47426d",
  "userId": "5d5396f6-9d30-4337-9463-3f772cc8ed1b",
  "deviceId": "device-stable-id",
  "pushToken": "<provider-token>",
  "platform": "Android",
  "provider": "Fcm",
  "targetApp": "Consumer",
  "title": "Double points this weekend",
  "body": "Visit us before Sunday and earn twice as many points.",
  "deepLink": "loyan://business/4dd1fb44-39b6-4d4a-a727-7120b313c6fc",
  "data": {
    "notificationId": "9fd01860-6c72-4cf6-b5ef-64ce7d47426d",
    "targetApp": "Consumer",
    "deepLink": "loyan://business/4dd1fb44-39b6-4d4a-a727-7120b313c6fc",
    "sourceType": "campaign",
    "sourceId": "4b940ef6-4bcb-40f5-bbb1-3812358c1633",
    "collapseKey": "campaign-4b940ef64bcb40f5bbb13812358c1633"
  },
  "collapseKey": "campaign-4b940ef64bcb40f5bbb13812358c1633",
  "analyticsLabel": "campaign_push",
  "idempotencyKey": "campaign-push:...",
  "androidChannelId": "campaigns",
  "apnsTopic": "com.loyan.darwin.mobile.consumer"
}
```

The gateway must not log full `pushToken` values. If logging is required, mask all but the first and last few characters.

## Gateway Response

Return a compact provider summary:

```json
{
  "success": true,
  "providerMessageId": "projects/example/messages/123"
}
```

For failures:

```json
{
  "success": false,
  "provider": "Fcm",
  "providerCode": "UNREGISTERED",
  "isTransient": false,
  "isInvalidToken": true
}
```

WebApi also maps common HTTP and provider errors. Invalid or unregistered tokens disable that device for future push. Transient failures are retried conservatively by the WebApi delivery worker.

## Mobile Behavior

Both Android apps register FCM tokens after login/resume. Debug builds warn when `google-services.json` is missing; Release builds must keep the existing guard that fails without Firebase configuration.

FCM data payloads should include:

- `title`
- `body`
- `deepLink`
- `notificationId`
- `targetApp`
- `sourceType`
- `sourceId`

When the app receives a foreground data message, it creates a local Android notification. When the user taps a notification, `MainActivity.OnNewIntent` passes `deepLink` to the app navigator. Unknown or unsafe deep links fall back to the Notifications page.

## Local Testing

1. Run WebApi with `Notifications:PushGateway:Enabled=false` to test inbox and delivery queue creation without sending provider traffic.
2. Enable a local gateway stub and return success/failure JSON to test delivery state transitions.
3. On Android, use a real Firebase project and place `google-services.json` in the app project.
4. Send a test FCM data message with a `loyan://...` deep link.
5. Verify foreground notification display, background tap navigation, and logged-out pending deep-link navigation after login.
