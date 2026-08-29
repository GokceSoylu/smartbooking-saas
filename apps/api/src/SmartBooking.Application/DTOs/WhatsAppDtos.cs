namespace SmartBooking.Application.DTOs;

// Webhook'tan Meta'nın gönderdiği payload modelleri
public record WhatsAppWebhookPayload(
    string? Object,
    List<WhatsAppWebhookEntry>? Entry
);

public record WhatsAppWebhookEntry(
    string? Id,
    List<WhatsAppWebhookChange>? Changes
);

public record WhatsAppWebhookChange(
    WhatsAppWebhookValue? Value,
    string? Field
);

public record WhatsAppWebhookValue(
    string? MessagingProduct,
    WhatsAppMetadata? Metadata,
    List<WhatsAppWebhookMessage>? Messages
);

public record WhatsAppMetadata(
    string? DisplayPhoneNumber,
    string? PhoneNumberId
);

public record WhatsAppWebhookMessage(
    string? From,
    string? Id,
    string? Timestamp,
    string? Type,
    WhatsAppInteractive? Interactive
);

public record WhatsAppInteractive(
    string? Type,
    WhatsAppButtonReply? ButtonReply
);

public record WhatsAppButtonReply(
    string? Id,
    string? Title
);