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
        if (string.IsNullOrWhiteSpace(customer.PhoneNumber)) return;

        var (token, phoneId) = GetConfig();
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(phoneId))
        {
            _logger.LogWarning("WhatsApp yapılandırma bilgileri (Token/PhoneId) eksik.");
            return;
        }

        var cleanPhone = FormatPhoneNumber(customer.PhoneNumber);
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
                            new { type = "text", text = customer.FullName },
                            new { type = "text", text = tenant.Name }
                        }
                    }
                }
            }
        };

        await PostToMetaGraphAsync(phoneId, payload, token, cancellationToken);
    }

    public async Task SendCustomerStatusUpdateAsync(
        Appointment appointment,
        Customer customer,
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customer.PhoneNumber)) return;

        var (token, phoneId) = GetConfig();
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(phoneId)) return;

        var cleanPhone = FormatPhoneNumber(customer.PhoneNumber);
        var templateName = appointment.Status == Domain.Enums.AppointmentStatus.Confirmed
            ? "randevu_onaylandi"
            : "randevu_reddedildi";

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
                            new { type = "text", text = customer.FullName }
                        }
                    }
                }
            }
        };

        await PostToMetaGraphAsync(phoneId, payload, token, cancellationToken);
    }

    public async Task SendAppointmentReminderAsync(
        Appointment appointment,
        Customer customer,
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customer.PhoneNumber)) return;

        var (token, phoneId) = GetConfig();
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(phoneId)) return;

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
                            new { type = "text", text = customer.FullName },
                            new { type = "text", text = tenant.Name }
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
            _logger.LogInformation("WhatsApp API isteği atılıyor... Endpoint: {Url}", url);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("WhatsApp API Hatası ({Status}): {Body}", response.StatusCode, errorBody);
            }
            else
            {
                _logger.LogInformation("WhatsApp mesajı başarıyla gönderildi.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp mesajı gönderilirken bir istisna oluştu.");
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