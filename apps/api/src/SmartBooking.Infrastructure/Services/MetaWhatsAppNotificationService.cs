using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;

namespace SmartBooking.Infrastructure.Services;

public class MetaWhatsAppNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MetaWhatsAppNotificationService> _logger;

    public MetaWhatsAppNotificationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MetaWhatsAppNotificationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAppointmentRequestNotificationAsync(
        Appointment appointment,
        Tenant tenant,
        Staff staff,
        Service service,
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        // 1. Müşteriye WhatsApp Bildirimi
        if (appointment.CustomerWantsWhatsAppNotification && !string.IsNullOrWhiteSpace(customer.PhoneNumber))
        {
            var customerMsg = $"*Randevu Talebiniz Alındı*\n\nSayın *{customer.FullName}*,\n*{tenant.Name}* işletmesinden randevu talebiniz başarıyla oluşturuldu.\n\n*Hizmet:* {service.Name}\n*Tarih:* {appointment.StartTimeUtc:dd.MM.yyyy HH:mm}\n*Tutar:* {appointment.Price} TL\n\nİşletme randevuyu onayladığında tekrar bilgilendirileceksiniz.";
            await SendDirectTextMessageAsync(customer.PhoneNumber, customerMsg, cancellationToken);
        }

        // 2. İşletme Sahibine İnteraktif Butonlu WhatsApp Bildirimi (Onayla / Reddet)
        var ownerPhone = !string.IsNullOrWhiteSpace(tenant.PhoneNumber) ? tenant.PhoneNumber : customer.PhoneNumber;

        if (tenant.NotifyOwnerOnNewAppointment || !string.IsNullOrWhiteSpace(ownerPhone))
        {
            var ownerMsg = $"*Yeni Randevu Talebi (İşletme Paneli)*\n\nİşletme: *{tenant.Name}*\nYeni bir müşteri randevusu oluşturuldu!\n\n*Müşteri:* {customer.FullName} ({customer.PhoneNumber})\n*Personel:* {staff.FullName}\n*Hizmet:* {service.Name}\n*Tarih:* {appointment.StartTimeUtc:dd.MM.yyyy HH:mm}\n*Tutar:* {appointment.Price} TL\n\nLütfen randevuyu yanıtlayınız:";

            var buttons = new[]
            {
                new { id = $"CONFIRM_{appointment.Id}", title = "✅ Onayla" },
                new { id = $"REJECT_{appointment.Id}", title = "❌ Reddet" }
            };

            await SendInteractiveButtonMessageAsync(ownerPhone, ownerMsg, buttons, cancellationToken);
        }
        else
        {
            _logger.LogWarning("İşletme bildirimleri kapalı veya geçerli bir telefon numarası bulunamadı. Tenant: {TenantId}", tenant.Id);
        }
    }

    public async Task SendCustomerStatusUpdateAsync(
        Appointment appointment,
        Customer customer,
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        if (!appointment.CustomerWantsWhatsAppNotification || string.IsNullOrWhiteSpace(customer.PhoneNumber))
            return;

        string statusText = appointment.Status switch
        {
            Domain.Enums.AppointmentStatus.Confirmed => "ONAYLANDI. Belirtilen saatte sizi bekliyoruz.",
            Domain.Enums.AppointmentStatus.Rejected => "işletme tarafından ONAYLANAMADI.",
            Domain.Enums.AppointmentStatus.Cancelled => "İPTAL EDİLDİ.",
            _ => "DURUMU GÜNCELLENDİ."
        };

        var message = $"*Randevu Durumu Güncellemesi*\n\nSayın *{customer.FullName}*,\n*{tenant.Name}* işletmesindeki randevunuz *{statusText}*\n\n*Tarih:* {appointment.StartTimeUtc:dd.MM.yyyy HH:mm}";
        await SendDirectTextMessageAsync(customer.PhoneNumber, message, cancellationToken);
    }

    public async Task SendAppointmentReminderAsync(
        Appointment appointment,
        Customer customer,
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        if (!appointment.CustomerWantsWhatsAppNotification || string.IsNullOrWhiteSpace(customer.PhoneNumber))
            return;

        var message = $"*Randevu Hatırlatması*\n\nSayın *{customer.FullName}*,\n*{tenant.Name}* işletmesindeki randevunuza 2 saat kaldı! Saat: {appointment.StartTimeUtc:HH:mm}.";
        await SendDirectTextMessageAsync(customer.PhoneNumber, message, cancellationToken);
    }

    private async Task SendDirectTextMessageAsync(string toPhone, string textBody, CancellationToken cancellationToken)
    {
        var token = _configuration["WhatsApp:AccessToken"];
        var phoneId = _configuration["WhatsApp:PhoneNumberId"];
        var version = _configuration["WhatsApp:ApiVersion"] ?? "v22.0";

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phoneId))
        {
            _logger.LogWarning("WhatsApp API ayarları appsettings içinde eksik.");
            return;
        }

        var cleanPhone = FormatPhoneNumber(toPhone);

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = cleanPhone,
            type = "text",
            text = new { preview_url = false, body = textBody }
        };

        var requestUrl = $"https://graph.facebook.com/{version}/{phoneId}/messages";
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp Mesajı İletildi -> {Phone} - Response: {Response}", cleanPhone, responseBody);
            }
            else
            {
                _logger.LogError("WhatsApp API Hatası ({StatusCode}) -> {Phone}: {Body}", response.StatusCode, cleanPhone, responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp mesajı gönderilirken bağlantı hatası oluştu: {Phone}", cleanPhone);
        }
    }

    private async Task SendInteractiveButtonMessageAsync(
        string toPhone,
        string bodyText,
        IEnumerable<dynamic> buttons,
        CancellationToken cancellationToken)
    {
        var token = _configuration["WhatsApp:AccessToken"];
        var phoneId = _configuration["WhatsApp:PhoneNumberId"];
        var version = _configuration["WhatsApp:ApiVersion"] ?? "v22.0";

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phoneId))
        {
            _logger.LogWarning("WhatsApp API ayarları appsettings içinde eksik.");
            return;
        }

        var cleanPhone = FormatPhoneNumber(toPhone);

        var actionButtons = buttons.Select(b => new
        {
            type = "reply",
            reply = new
            {
                id = (string)b.id,
                title = (string)b.title
            }
        }).ToArray();

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = cleanPhone,
            type = "interactive",
            interactive = new
            {
                type = "button",
                body = new { text = bodyText },
                action = new { buttons = actionButtons }
            }
        };

        var requestUrl = $"https://graph.facebook.com/{version}/{phoneId}/messages";
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp Butonlu Bildirim Gönderildi -> {Phone}", cleanPhone);
            }
            else
            {
                _logger.LogError("WhatsApp Buton Gönderme Hatası ({Code}) -> {Phone}: {Body}", response.StatusCode, cleanPhone, responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp buton mesajı gönderilirken bağlantı hatası: {Phone}", cleanPhone);
        }
    }

    private string FormatPhoneNumber(string raw)
    {
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0")) digits = digits[1..];
        if (!digits.StartsWith("90")) digits = "90" + digits;
        return digits;
    }
}