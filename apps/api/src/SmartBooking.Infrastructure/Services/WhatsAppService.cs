using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;
using SmartBooking.Domain.Enums;

namespace SmartBooking.Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(HttpClient httpClient, IConfiguration configuration, ILogger<WhatsAppService> logger)
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
        var accessToken = _configuration["WhatsApp:AccessToken"];
        var phoneId = tenant.WhatsAppPhoneNumberId ?? _configuration["WhatsApp:DefaultPhoneNumberId"];

        var messageText = $"🔔 *Yeni Randevu Talebi!*\n\n" +
                          $"👤 *Müşteri:* {customer.FullName}\n" +
                          $"📞 *Telefon:* {customer.PhoneNumber}\n" +
                          $"✂️ *Hizmet:* {service.Name} ({service.Price:N2} TL)\n" +
                          $"💈 *Personel:* {staff.FullName}\n" +
                          $"📅 *Tarih/Saat:* {appointment.StartTimeUtc:dd.MM.yyyy HH:mm} (UTC)\n\n" +
                          $"Randevuyu onaylamak veya reddetmek için aşağıdaki butonları kullanın:";

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = tenant.PhoneNumber,
            type = "interactive",
            interactive = new
            {
                type = "button",
                body = new { text = messageText },
                action = new
                {
                    buttons = new[]
                    {
                        new
                        {
                            type = "reply",
                            reply = new
                            {
                                id = $"CONFIRM_{appointment.Id}",
                                title = "✅ Onayla"
                            }
                        },
                        new
                        {
                            type = "reply",
                            reply = new
                            {
                                id = $"REJECT_{appointment.Id}",
                                title = "❌ Reddet"
                            }
                        }
                    }
                }
            }
        };

        await SendRawMessageAsync(phoneId, accessToken, payload, cancellationToken);
    }

    public async Task SendCustomerStatusUpdateAsync(
        Appointment appointment,
        Customer customer,
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        var accessToken = _configuration["WhatsApp:AccessToken"];
        var phoneId = tenant.WhatsAppPhoneNumberId ?? _configuration["WhatsApp:DefaultPhoneNumberId"];

        var statusText = appointment.Status == AppointmentStatus.Confirmed
            ? "✅ Randevunuz başarıyla onaylandı. Sizi ağırlamaktan mutluluk duyarız!"
            : "❌ Üzgünüz, randevu talebiniz işletme tarafından uygunluk sağlanamadığı için onaylanamadı.";

        var messageText = $"Sayın *{customer.FullName}*,\n\n" +
                          $"{tenant.Name} için oluşturduğunuz randevu durumu:\n" +
                          $"{statusText}\n\n" +
                          $"📅 *Tarih:* {appointment.StartTimeUtc:dd.MM.yyyy HH:mm} (UTC)";

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = customer.PhoneNumber,
            type = "text",
            text = new { preview_url = false, body = messageText }
        };

        await SendRawMessageAsync(phoneId, accessToken, payload, cancellationToken);
    }

    private async Task SendRawMessageAsync(string? phoneId, string? accessToken, object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(phoneId))
        {
            _logger.LogWarning("[WhatsApp Simulation] Token veya PhoneId eksik. Mesaj simüle edildi:\n{Payload}",
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        var url = $"https://graph.facebook.com/v19.0/{phoneId}/messages";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("[WhatsApp API Error] Gönderim başarısız: {Error}", err);
        }
    }
    public async Task SendAppointmentReminderAsync(
        Appointment appointment,
        Customer customer,
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        var accessToken = _configuration["WhatsApp:AccessToken"];
        var phoneId = tenant.WhatsAppPhoneNumberId ?? _configuration["WhatsApp:DefaultPhoneNumberId"];

        var messageText = $"⏰ *Randevu Hatırlatması!*\n\n" +
                          $"Sayın *{customer.FullName}*,\n" +
                          $"*{tenant.Name}* bünyesindeki randevunuza yaklaşık *2 saat* kaldı.\n\n" +
                          $"📅 *Saat:* {appointment.StartTimeUtc:HH:mm} (UTC)\n\n" +
                          $"Gelemeyecekseniz veya bir değişiklik varsa lütfen işletmeyle iletişime geçiniz.";

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = customer.PhoneNumber,
            type = "text",
            text = new { preview_url = false, body = messageText }
        };

        await SendRawMessageAsync(phoneId, accessToken, payload, cancellationToken);
    }
}