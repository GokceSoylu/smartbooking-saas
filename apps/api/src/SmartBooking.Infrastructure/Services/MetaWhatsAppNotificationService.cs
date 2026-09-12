using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;
using SmartBooking.Domain.Enums;

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
        var localStartTime = ConvertToLocalTime(appointment.StartTimeUtc);

        // 1. Müşteriye Şablon Mesajı: randevu_alindi (Parametreler: {{1}}=Müşteri, {{2}}=İşletme)
        if (appointment.CustomerWantsWhatsAppNotification && !string.IsNullOrWhiteSpace(customer?.PhoneNumber))
        {
            _logger.LogInformation(">>> Müşteriye 'randevu_alindi' şablonu gönderiliyor: {Phone}", customer.PhoneNumber);
            await SendTemplateMessageAsync(
                customer.PhoneNumber,
                "randevu_alindi",
                new[] { customer.FullName ?? "Değerli Müşterimiz", tenant?.Name ?? "İşletme" },
                cancellationToken
            );
        }

        // 2. İşletme Sahibine Butonlu Onay Talebi: randevu_onay_talep
        var ownerPhone = !string.IsNullOrWhiteSpace(tenant?.PhoneNumber) ? tenant.PhoneNumber : customer?.PhoneNumber;

        if (!string.IsNullOrWhiteSpace(ownerPhone))
        {
            _logger.LogInformation(">>> İşletmeye 'randevu_onay_talep' şablonu gönderiliyor: {Phone}", ownerPhone);
            var parameters = new[]
            {
                customer?.FullName ?? "Müşteri",
                staff?.FullName ?? "Personel",
                service?.Name ?? "Hizmet",
                localStartTime.ToString("dd.MM.yyyy HH:mm"),
                appointment.Price.ToString("0.00") + " TL"
            };

            var buttons = new[]
            {
                new { id = $"CONFIRM_{appointment.Id}", title = "Onayla" },
                new { id = $"REJECT_{appointment.Id}", title = "Reddet" }
            };

            await SendInteractiveTemplateWithButtonsAsync(ownerPhone, "randevu_onay_talep", parameters, buttons, cancellationToken);
        }
        else
        {
            _logger.LogWarning(">>> İşletme telefon numarası bulunamadı, bildirim gönderilemedi. TenantId: {TenantId}", tenant?.Id);
        }
    }

    public async Task SendCustomerStatusUpdateAsync(
        Appointment appointment,
        Customer customer,
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        if (customer == null || string.IsNullOrWhiteSpace(customer.PhoneNumber)) return;

        var localStartTime = ConvertToLocalTime(appointment.StartTimeUtc);

        if (appointment.Status == AppointmentStatus.Confirmed)
        {
            _logger.LogInformation(">>> Müşteriye 'randevu_onaylandi' şablonu gönderiliyor: {Phone}", customer.PhoneNumber);
            await SendTemplateMessageAsync(
                customer.PhoneNumber,
                "randevu_onaylandi",
                new[] { customer.FullName ?? "Müşteri", localStartTime.ToString("dd.MM.yyyy HH:mm") },
                cancellationToken
            );
        }
        else if (appointment.Status == AppointmentStatus.Rejected)
        {
            _logger.LogInformation(">>> Müşteriye red bildirimi gönderiliyor: {Phone}", customer.PhoneNumber);
            var message = $"Sayın {customer.FullName},\n{tenant?.Name ?? "İşletme"} randevu talebinizi ne yazık ki onaylayamadı.";
            await SendDirectTextMessageAsync(customer.PhoneNumber, message, cancellationToken);
        }
    }

    public async Task SendAppointmentReminderAsync(
        Appointment appointment,
        Customer customer,
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        if (customer == null || string.IsNullOrWhiteSpace(customer.PhoneNumber)) return;

        var localStartTime = ConvertToLocalTime(appointment.StartTimeUtc);
        var message = $"*Randevu Hatırlatması*\n\nSayın *{customer.FullName}*,\n*{tenant?.Name}* işletmesindeki randevunuza 2 saat kaldı! Saat: {localStartTime:HH:mm}.";
        await SendDirectTextMessageAsync(customer.PhoneNumber, message, cancellationToken);
    }

    private async Task SendTemplateMessageAsync(string toPhone, string templateName, string[] parameters, CancellationToken cancellationToken)
    {
        var (token, phoneId, version) = GetConfig();
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phoneId))
        {
            _logger.LogError(">>> WhatsApp AccessToken veya PhoneNumberId konfigürasyonda eksik!");
            return;
        }

        var cleanPhone = FormatPhoneNumber(toPhone);
        var parameterObjects = parameters.Select(p => new { type = "text", text = p }).ToArray();

        var payload = new
        {
            messaging_product = "whatsapp",
            to = cleanPhone,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = "tr" },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = parameterObjects
                    }
                }
            }
        };

        await ExecutePostAsync(cleanPhone, payload, cancellationToken);
    }

    private async Task SendInteractiveTemplateWithButtonsAsync(
        string toPhone,
        string templateName,
        string[] parameters,
        dynamic[] buttons,
        CancellationToken cancellationToken)
    {
        var (token, phoneId, version) = GetConfig();
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phoneId))
        {
            _logger.LogError(">>> WhatsApp AccessToken veya PhoneNumberId konfigürasyonda eksik!");
            return;
        }

        var cleanPhone = FormatPhoneNumber(toPhone);
        var parameterObjects = parameters.Select(p => new { type = "text", text = p }).ToArray();

        var payload = new
        {
            messaging_product = "whatsapp",
            to = cleanPhone,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = "tr" },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = parameterObjects
                    },
                    new
                    {
                        type = "button",
                        sub_type = "quick_reply",
                        index = "0",
                        parameters = new[]
                        {
                            new { type = "payload", payload = buttons[0].id }
                        }
                    },
                    new
                    {
                        type = "button",
                        sub_type = "quick_reply",
                        index = "1",
                        parameters = new[]
                        {
                            new { type = "payload", payload = buttons[1].id }
                        }
                    }
                }
            }
        };

        await ExecutePostAsync(cleanPhone, payload, cancellationToken);
    }

    private async Task SendDirectTextMessageAsync(string toPhone, string textBody, CancellationToken cancellationToken)
    {
        var (token, phoneId, version) = GetConfig();
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phoneId)) return;

        var cleanPhone = FormatPhoneNumber(toPhone);
        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = cleanPhone,
            type = "text",
            text = new { preview_url = false, body = textBody }
        };

        await ExecutePostAsync(cleanPhone, payload, cancellationToken);
    }

    private async Task<bool> ExecutePostAsync(string cleanPhone, object payload, CancellationToken cancellationToken)
    {
        var (token, phoneId, version) = GetConfig();
        var requestUrl = $"https://graph.facebook.com/{version}/{phoneId}/messages";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(">>> [Meta WhatsApp Başarılı] -> Hedef: {Phone}", cleanPhone);
                return true;
            }

            _logger.LogError(">>> [Meta WhatsApp API Hatası] ({StatusCode}) -> Hedef: {Phone} | Yanıt: {Body}", response.StatusCode, cleanPhone, responseBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ">>> [Meta WhatsApp İstisna Hatası] -> Hedef: {Phone}", cleanPhone);
            return false;
        }
    }

    private (string? Token, string? PhoneId, string Version) GetConfig()
    {
        var token = _configuration["WhatsApp:AccessToken"];
        var phoneId = _configuration["WhatsApp:PhoneNumberId"];
        var version = _configuration["WhatsApp:ApiVersion"] ?? "v22.0";
        return (token, phoneId, version);
    }

    private string FormatPhoneNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0")) digits = digits[1..];
        if (!digits.StartsWith("90") && digits.Length == 10) digits = "90" + digits;
        return digits;
    }

    private DateTime ConvertToLocalTime(DateTime utcDateTime)
    {
        try
        {
            var trZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, trZone);
        }
        catch
        {
            return utcDateTime.AddHours(3);
        }
    }
}