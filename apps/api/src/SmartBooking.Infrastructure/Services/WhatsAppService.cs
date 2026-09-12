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
        if (customer == null || string.IsNullOrWhiteSpace(customer.PhoneNumber))
        {
            _logger.LogWarning(">>> [WhatsAppService] Müşteri veya telefon numarası boş.");
            return;
        }

        var (token, phoneId) = GetConfig();
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(phoneId))
        {
            _logger.LogWarning(">>> [WhatsAppService] AccessToken veya PhoneNumberId konfigürasyonda bulunamadı.");
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
                            new { type = "text", text = customer.FullName ?? "" },
                            new { type = "text", text = tenant?.Name ?? "" }
                        }
                    }
                }
            }
        };

        _logger.LogInformation(">>> [WhatsAppService] 'randevu_alindi' gönderiliyor -> Müşteri: {Phone}", cleanPhone);
        await PostToMetaGraphAsync(phoneId, payload, token, cancellationToken);
    }

    public async Task SendBusinessNewAppointmentNotificationAsync(
        Appointment appointment,
        Tenant tenant,
        Staff staff,
        Service service,
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        if (tenant == null || string.IsNullOrWhiteSpace(tenant.PhoneNumber))
        {
            _logger.LogWarning(">>> [WhatsAppService] İşletme sahibi veya telefon numarası boş.");
            return;
        }

        var (token, phoneId) = GetConfig();
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(phoneId))
        {
            _logger.LogWarning(">>> [WhatsAppService] AccessToken veya PhoneNumberId konfigürasyonda bulunamadı.");
            return;
        }

        var cleanPhone = FormatPhoneNumber(tenant.PhoneNumber);
        var startTimeStr = appointment.StartTimeUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

        // Tutar bilgisi (Service veya Appointment entity'sine göre ayarlayabilirsiniz)
        var priceStr = service?.Price.ToString("F0") ?? "0";

        // Personel adı formatlaması (FullName veya FirstName/LastName fallback)
        var staffName = staff?.FullName ?? $"{staff?.FirstName} {staff?.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(staffName))
        {
            staffName = "Personel";
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            to = cleanPhone,
            type = "template",
            template = new
            {
                name = "randevu_onay_talep", // Meta'da onaylı işletme şablonu
                language = new { code = "tr" },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = new[]
                        {
                            new { type = "text", text = customer?.FullName ?? "Müşteri" },  // {{1}} Müşteri
                            new { type = "text", text = staffName },                       // {{2}} Personel
                            new { type = "text", text = service?.Name ?? "Hizmet" },       // {{3}} Hizmet
                            new { type = "text", text = startTimeStr },                    // {{4}} Tarih
                            new { type = "text", text = priceStr }                         // {{5}} Tutar
                        }
                    }
                }
            }
        };

        _logger.LogInformation(">>> [WhatsAppService] İşletme sahibine randevu onay talebi gönderiliyor -> Tel: {Phone}", cleanPhone);
        await PostToMetaGraphAsync(phoneId, payload, token, cancellationToken);
    }

    public async Task SendCustomerStatusUpdateAsync(
        Appointment appointment,
        Customer customer,
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        if (customer == null || string.IsNullOrWhiteSpace(customer.PhoneNumber))
        {
            _logger.LogWarning(">>> [WhatsAppService] Müşteri veya telefon bilgisi eksik.");
            return;
        }

        var (token, phoneId) = GetConfig();
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(phoneId))
        {
            _logger.LogWarning(">>> [WhatsAppService] AccessToken veya PhoneNumberId eksik.");
            return;
        }

        var cleanPhone = FormatPhoneNumber(customer.PhoneNumber);
        var templateName = appointment.Status == Domain.Enums.AppointmentStatus.Confirmed
            ? "randevu_onaylandi"
            : "randevu_reddedildi";

        var startTimeStr = appointment.StartTimeUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

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

        _logger.LogInformation(">>> [WhatsAppService] '{Template}' gönderiliyor -> Hedef: {Phone}", templateName, cleanPhone);
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
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(">>> [WhatsAppService] Meta API Hatası ({Status}): {Body}", response.StatusCode, responseBody);
            }
            else
            {
                _logger.LogInformation(">>> [WhatsAppService] Meta API Başarılı Yanıt: {Body}", responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ">>> [WhatsAppService] Meta Graph API bağlantı hatası.");
        }
    }

    private (string? Token, string? PhoneId) GetConfig()
    {
        return (_configuration["WhatsApp:AccessToken"], _configuration["WhatsApp:PhoneNumberId"]);
    }

    private string FormatPhoneNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        // Yalnızca rakamları temizle
        var digits = new string(raw.Where(char.IsDigit).ToArray());

        // '05xx...' ise başındaki 0'ı kaldır (11 hane -> 10 hane)
        if (digits.StartsWith("0") && digits.Length == 11)
        {
            digits = digits[1..];
        }

        // '5xx...' ise başına 90 ekle (10 hane -> 12 hane)
        if (digits.Length == 10)
        {
            digits = "90" + digits;
        }

        return digits;
    }
}