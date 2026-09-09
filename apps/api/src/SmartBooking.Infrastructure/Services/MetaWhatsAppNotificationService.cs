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
        // 1. Müşteriye Şablon Mesajı: randevu_alindi
        if (appointment.CustomerWantsWhatsAppNotification && !string.IsNullOrWhiteSpace(customer.PhoneNumber))
        {
            await SendTemplateMessageAsync(
                customer.PhoneNumber,
                "randevu_alindi",
                new[] { customer.FullName, tenant.Name },
                cancellationToken
            );
        }

        // 2. İşletme Sahibine Şablon ve Butonlu İstek: randevu_onay_talep
        var ownerPhone = !string.IsNullOrWhiteSpace(tenant.PhoneNumber) ? tenant.PhoneNumber : customer.PhoneNumber;

        if (tenant.NotifyOwnerOnNewAppointment || !string.IsNullOrWhiteSpace(ownerPhone))
        {
            var parameters = new[]
            {
                customer.FullName,
                staff.FullName,
                service.Name,
                appointment.StartTimeUtc.ToString("dd.MM.yyyy HH:mm"),
                appointment.Price.ToString("0.00")
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
            _logger.LogWarning("İşletme bildirimleri kapalı veya telefon numarası yok. Tenant: {TenantId}", tenant.Id);
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

        if (appointment.Status == Domain.Enums.AppointmentStatus.Confirmed)
        {
            // Şablon: randevu_onaylandi
            await SendTemplateMessageAsync(
                customer.PhoneNumber,
                "randevu_onaylandi",
                new[] { customer.FullName, appointment.StartTimeUtc.ToString("dd.MM.yyyy HH:mm") },
                cancellationToken
            );
        }
        else
        {
            // Diğer durumlar için genel metin bilgilendirmesi
            string statusText = appointment.Status switch
            {
                Domain.Enums.AppointmentStatus.Rejected => "işletme tarafından ONAYLANAMADI.",
                Domain.Enums.AppointmentStatus.Cancelled => "İPTAL EDİLDİ.",
                _ => "DURUMU GÜNCELLENDİ."
            };

            var message = $"*Randevu Durumu Güncellemesi*\n\nSayın *{customer.FullName}*,\n*{tenant.Name}* işletmesindeki randevunuz *{statusText}*\n\n*Tarih:* {appointment.StartTimeUtc:dd.MM.yyyy HH:mm}";
            await SendDirectTextMessageAsync(customer.PhoneNumber, message, cancellationToken);
        }
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

    private async Task SendTemplateMessageAsync(string toPhone, string templateName, string[] parameters, CancellationToken cancellationToken)
    {
        var (token, phoneId, version) = GetConfig();
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phoneId)) return;

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
        IEnumerable<dynamic> buttons,
        CancellationToken cancellationToken)
    {
        var (token, phoneId, version) = GetConfig();
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phoneId)) return;

        var cleanPhone = FormatPhoneNumber(toPhone);
        var parameterObjects = parameters.Select(p => new { type = "text", text = p }).ToArray();

        var actionButtons = buttons.Select((b, index) => new
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
                            new { type = "payload", payload = ((object[])buttons)[0].GetType().GetProperty("id")?.GetValue(((object[])buttons)[0])?.ToString() }
                        }
                    },
                    new
                    {
                        type = "button",
                        sub_type = "quick_reply",
                        index = "1",
                        parameters = new[]
                        {
                            new { type = "payload", payload = ((object[])buttons)[1].GetType().GetProperty("id")?.GetValue(((object[])buttons)[1])?.ToString() }
                        }
                    }
                }
            }
        };

        var isSuccess = await ExecutePostAsync(cleanPhone, payload, cancellationToken);

        if (!isSuccess)
        {
            _logger.LogWarning("Butonlu şablon mesajı gönderilemedi, düz metin deneniyor: {Phone}", cleanPhone);
            var fallbackText = $"Yeni randevu talebi var. Onaylamak veya reddetmek için panelinizi kullanın.";
            await SendDirectTextMessageAsync(cleanPhone, fallbackText, cancellationToken);
        }
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
                _logger.LogInformation("WhatsApp İşlemi Başarılı -> {Phone}", cleanPhone);
                return true;
            }

            _logger.LogError("WhatsApp API Hatası ({StatusCode}) -> {Phone}: {Body}", response.StatusCode, cleanPhone, responseBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp mesajı gönderilirken hata: {Phone}", cleanPhone);
            return false;
        }
    }

    private (string? Token, string? PhoneId, string Version) GetConfig()
    {
        var token = _configuration["WhatsApp:AccessToken"];
        var phoneId = _configuration["WhatsApp:PhoneNumberId"] ?? "1329477973578164";
        var version = _configuration["WhatsApp:ApiVersion"] ?? "v22.0";
        return (token, phoneId, version);
    }

    private string FormatPhoneNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0")) digits = digits[1..];
        if (!digits.StartsWith("90")) digits = "90" + digits;
        return digits;
    }
}