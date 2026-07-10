# WebhookHandler
The `WebhookHandler` type is designed to handle incoming webhooks, providing a structured approach to processing and verifying webhook events. It allows for registration of endpoints, verification of signatures, and handling of webhook events in an asynchronous manner. This type is part of the `redis-cache-patterns` project, aiming to simplify the integration of webhooks with caching mechanisms for efficient event processing.

## API
### Constructors
- `public WebhookHandler`: Initializes a new instance of the `WebhookHandler` class.
- `public WebhookHandler RegisterEndpoint`: Registers an endpoint for the webhook handler.

### Methods
- `public bool VerifySignature`: Verifies the signature of a webhook event. Returns `true` if the signature is valid, `false` otherwise.
- `public async Task<bool> HandleWebhookAsync`: Asynchronously handles a webhook event. Returns a task that represents the asynchronous operation, with a result indicating whether the handling was successful.

### Properties
- `public string EndpointUrl`: Gets the URL of the registered endpoint.
- `public string Secret`: Gets the secret used for signature verification.
- `public string? Authentication`: Gets the authentication details for the webhook.
- `public bool IsActive`: Indicates whether the webhook handler is active.
- `public int MaxRetries`: Gets the maximum number of retries for handling a webhook event.
- `public string Id`: Gets the identifier of the webhook event.
- `public string Endpoint`: Gets the endpoint associated with the webhook event.
- `public string Payload`: Gets the payload of the webhook event.
- `public DateTime ReceivedAt`: Gets the date and time when the webhook event was received.
- `public DateTime? ProcessedAt`: Gets the date and time when the webhook event was processed, if applicable.
- `public bool IsProcessed`: Indicates whether the webhook event has been processed.

### Events
- `public delegate Task WebhookEventHandler`: Represents a method that will handle a webhook event.

### Other Members
- `public IEnumerable<WebhookEvent> GetProcessedEvents`: Gets the processed webhook events.

## Usage
The following examples demonstrate how to use the `WebhookHandler` type to handle webhook events:

```csharp
// Example 1: Basic Webhook Handling
var webhookHandler = new WebhookHandler();
webhookHandler.RegisterEndpoint("https://example.com/webhook");
var isHandled = await webhookHandler.HandleWebhookAsync();
if (isHandled)
{
    Console.WriteLine("Webhook event handled successfully.");
}
else
{
    Console.WriteLine("Failed to handle webhook event.");
}

// Example 2: Advanced Webhook Handling with Custom Authentication
var advancedWebhookHandler = new WebhookHandler();
advancedWebhookHandler.Authentication = "Bearer YOUR_AUTH_TOKEN";
advancedWebhookHandler.RegisterEndpoint("https://example.com/secure-webhook");
var processedEvents = advancedWebhookHandler.GetProcessedEvents();
foreach (var @event in processedEvents)
{
    Console.WriteLine($"Processed event: {@event.Id}");
}
```

## Notes
- The `WebhookHandler` type is designed to be thread-safe, allowing for concurrent handling of webhook events.
- When using the `HandleWebhookAsync` method, ensure that the calling code can handle asynchronous operations properly to avoid deadlocks or other concurrency issues.
- The `VerifySignature` method may throw exceptions if the signature verification fails due to invalid input or configuration. It is recommended to handle such exceptions appropriately in the calling code.
- The `MaxRetries` property controls how many times the `HandleWebhookAsync` method will attempt to handle a webhook event before considering it failed. Adjust this value based on the specific requirements of your application and the reliability of the webhook event source.
