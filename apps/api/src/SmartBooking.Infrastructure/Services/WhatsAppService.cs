using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;

namespace SmartBooking.Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WhatsAppService> logger)
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
        _logger.LogInformation(">>> [WhatsAppService] SendAppointmentRequestNotificationAsync tetiklendi.");

        if (customer == null || string.IsNullOrWhiteSpace(customer.PhoneNumber))
        {
            _logger.LogWarning(">>> [WhatsAppService] Müşteri veya telefon numarası boş olduğu için bildirim atlandı.");
            return;
        }

        var (token, phoneId) = GetConfig();
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(phoneId) || token.StartsWith("YOUR_"))
        {
            _logger.LogWarning(">>> [WhatsAppService] AccessToken veya PhoneNumberId appsettings.json içinde geçerli değil.");
            return;
        }

        var cleanPhone = FormatPhoneNumber(customer.PhoneNumber);

        // Meta Şablonu: randevu_alindi -> {{1}} = Müşteri Adı, {{2}} = İşletme Adı
        var payload = new
        {
            messaging_product = "whatsapp",
            to = cleanPhone,
            type = "template",
            template = new
            {
                name = "randevu_alindi",
                language = new { code = "tr" },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = new[]
                        {
                            new { type = "text", text = customer.FullName ?? "" },
                            new { type = "text", text = tenant?.Name ?? "" }
                        }
                    }
                }
            }
        };

        _logger.LogInformation(">>> [WhatsAppService] Müşteriye 'randevu_alindi' bildirimi gönderiliyor. Hedef: {Phone}", cleanPhone);
        await PostToMetaGraphAsync(phoneId, payload, token, cancellationToken);
    }

    public async Task SendCustomerStatusUpdateAsync(
        Appointment appointment,
        Customer customer,
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(">>> [WhatsAppService] SendCustomerStatusUpdateAsync tetiklendi.");

        if (customer == null || string.IsNullOrWhiteSpace(customer.PhoneNumber))
        {
            _logger.LogWarning(">>> [WhatsAppService] Müşteri veya telefon numarası boş olduğu için bildirim atlandı.");
            return;
        }

        var (token, phoneId) = GetConfig();
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(phoneId) || token.StartsWith("YOUR_"))
        {
            _logger.LogWarning(">>> [WhatsAppService] AccessToken veya PhoneNumberId appsettings.json içinde geçerli değil.");
            return;
        }

        var cleanPhone = FormatPhoneNumber(customer.PhoneNumber);
        var templateName = appointment.Status == Domain.Enums.AppointmentStatus.Confirmed
            ? "randevu_onaylandi"
            : "randevu_reddedildi";

        var startTimeStr = appointment.StartTimeUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

        // Meta Şablonu: randevu_onaylandi / randevu_reddedildi -> {{1}} = Müşteri Adı, {{2}} = Tarih/Saat
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
                        parameters = new[]
                        {
                            new { type = "text", text = customer.FullName ?? "" },
                            new { type = "text", text = startTimeStr }
                        }
                    }
                }
            }
        };

        _logger.LogInformation(">>> [WhatsAppService] Müşteriye '{Template}' bildirimi gönderiliyor. Hedef: {Phone}", templateName, cleanPhone);
        await PostToMetaGraphAsync(phoneId, payload, token, cancellationToken);
    }

    public async Task SendAppointmentReminderAsync(
        Appointment appointment,
        Customer customer,
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        if (customer == null || string.IsNullOrWhiteSpace(customer.PhoneNumber)) return;

        var (token, phoneId) = GetConfig();
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(phoneId) || token.StartsWith("YOUR_")) return;

        var cleanPhone = FormatPhoneNumber(customer.PhoneNumber);
        var payload = new
        {
            messaging_product = "whatsapp",
            to = cleanPhone,
            type = "template",
            template = new
            {
                name = "randevu_hatirlatma",
                language = new { code = "tr" },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = new[]
                        {
                            new { type = "text", text = customer.FullName ?? "" },
                            new { type = "text", text = tenant?.Name ?? "" }
                        }
                    }
                }
            }
        };

        await PostToMetaGraphAsync(phoneId, payload, token, cancellationToken);
    }

    private async Task PostToMetaGraphAsync(string phoneId, object payload, string token, CancellationToken cancellationToken)
    {
        var url = $"https://graph.facebook.com/v22.0/{phoneId}/messages";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(">>> [WhatsAppService] Meta API Hatası ({Status}): {Body}", response.StatusCode, errorBody);
            }
            else
            {
                _logger.LogInformation(">>> [WhatsAppService] Mesaj başarıyla Meta tarafına iletildi.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ">>> [WhatsAppService] Meta API'ye istek atılırken istisna oluştu.");
        }
    }

    private (string? Token, string? PhoneId) GetConfig()
    {
        return (_configuration["WhatsApp:AccessToken"], _configuration["WhatsApp:PhoneNumberId"]);
    }

    private string FormatPhoneNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0")) digits = digits[1..];
        if (!digits.StartsWith("90") && digits.Length == 10) digits = "90" + digits;
        return digits;
    }
}