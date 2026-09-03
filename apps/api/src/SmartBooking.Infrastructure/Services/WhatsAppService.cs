using System.Net.Http.Headers;
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
        // 1. Müşteri Bildirimi (Eğer müşteri WhatsApp bildirimini kabul etmişse)
        if (appointment.CustomerWantsWhatsAppNotification && !string.IsNullOrWhiteSpace(customer.PhoneNumber))
        {
            var customerMsg = $"*RandevuCep Bilgilendirme*\n\n" +
                              $"Merhaba Sayın *{customer.FullName}*,\n" +
                              $"*{tenant.Name}* işletmesinden randevu talebiniz başarıyla alınmıştır.\n\n" +
                              $"🗓 *Tarih:* {appointment.StartTimeUtc:dd.MM.yyyy}\n" +
                              $"⏰ *Saat:* {appointment.StartTimeUtc:HH:mm}\n" +
                              $"✂️ *Hizmet:* {service.Name}\n" +
                              $"👤 *Uzman:* {staff.FullName}\n" +
                              $"💰 *Tutar:* {appointment.Price} ₺\n\n" +
                              $"İşletme randevunuzu onayladığında buradan bilgilendirileceksiniz.";

            await SendTextMessageAsync(customer.PhoneNumber, customerMsg, cancellationToken);
        }

        // 2. İşletme Sahibine Bildirim (Eğer işletme sahibi bildirim ayarını açık tutmuşsa)
        if (tenant.NotifyOwnerOnNewAppointment && !string.IsNullOrWhiteSpace(tenant.PhoneNumber))
        {
            var ownerText = $"🔔 *[RandevuCep - Yeni Randevu Talebi]*\n\n" +
                            $"Sayın *{tenant.Name}*,\n" +
                            $"Yeni bir online randevu talebiniz var:\n\n" +
                            $"👤 *Müşteri:* {customer.FullName} ({customer.PhoneNumber})\n" +
                            $"🗓 *Tarih:* {appointment.StartTimeUtc:dd.MM.yyyy} - {appointment.StartTimeUtc:HH:mm}\n" +
                            $"✂️ *Hizmet:* {service.Name}\n" +
                            $"💈 *Personel:* {staff.FullName}\n" +
                            $"💵 *Tutar:* {appointment.Price} ₺";

            await SendInteractiveButtonsAsync(
                tenant.PhoneNumber,
                ownerText,
                appointment.Id,
                cancellationToken
            );
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
            Domain.Enums.AppointmentStatus.Confirmed => "✅ *ONAYLANDI*",
            Domain.Enums.AppointmentStatus.Rejected => "❌ *REDDEDİLDİ*",
            Domain.Enums.AppointmentStatus.Cancelled => "⚠️ *İPTAL EDİLDİ*",
            _ => "DURUM GÜNCELLENDİ"
        };

        var message = $"*RandevuCep Bilgilendirme*\n\n" +
                      $"Sayın *{customer.FullName}*,\n" +
                      $"*{tenant.Name}* işletmesindeki randevunuzun durumu: {statusText}\n\n" +
                      $"🗓 *Tarih:* {appointment.StartTimeUtc:dd.MM.yyyy} - {appointment.StartTimeUtc:HH:mm}\n" +
                      (appointment.Status == Domain.Enums.AppointmentStatus.Confirmed
                          ? "Sizi ağırlamaktan mutluluk duyacağız!"
                          : "Detaylı bilgi için işletmeyle iletişime geçebilirsiniz.");

        await SendTextMessageAsync(customer.PhoneNumber, message, cancellationToken);
    }

    public async Task SendAppointmentReminderAsync(
        Appointment appointment,
        Customer customer,
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        if (!appointment.CustomerWantsWhatsAppNotification || string.IsNullOrWhiteSpace(customer.PhoneNumber))
            return;

        var message = $"⏰ *[RandevuCep Randevu Hatırlatması]*\n\n" +
                      $"Merhaba Sayın *{customer.FullName}*,\n" +
                      $"*{tenant.Name}* işletmesindeki randevunuza 2 saat kaldı!\n\n" +
                      $"⏰ *Saat:* {appointment.StartTimeUtc:HH:mm}\n" +
                      $"Görüşmek üzere!";

        await SendTextMessageAsync(customer.PhoneNumber, message, cancellationToken);
    }

    private async Task SendTextMessageAsync(string to, string body, CancellationToken cancellationToken)
    {
        var cleanTo = FormatPhoneNumber(to);
        _logger.LogInformation("\n================== WHATSAPP ÇIKIŞI [RandevuCep] ==================\nAlıcı: {To}\nMesaj:\n{Body}\n===================================================================", cleanTo, body);

        var token = _configuration["WhatsApp:AccessToken"];
        var phoneId = _configuration["WhatsApp:DefaultPhoneNumberId"];
        if (string.IsNullOrWhiteSpace(token) || token.StartsWith("EAAG...")) return;

        var payload = new
        {
            messaging_product = "whatsapp",
            to = cleanTo,
            type = "text",
            text = new { body }
        };

        await PostToMetaGraphAsync(phoneId, payload, token, cancellationToken);
    }

    private async Task SendInteractiveButtonsAsync(string to, string body, Guid appointmentId, CancellationToken cancellationToken)
    {
        var cleanTo = FormatPhoneNumber(to);
        _logger.LogInformation("\n================== WHATSAPP BUTON ÇIKIŞI [RandevuCep] ==================\nİşletme Tel: {To}\nRandevuId: {Id}\nMesaj:\n{Body}\nButonlar: [ONAYLA] [REDDET]\n=======================================================================", cleanTo, appointmentId, body);

        var token = _configuration["WhatsApp:AccessToken"];
        var phoneId = _configuration["WhatsApp:DefaultPhoneNumberId"];
        if (string.IsNullOrWhiteSpace(token) || token.StartsWith("EAAG...")) return;

        var payload = new
        {
            messaging_product = "whatsapp",
            to = cleanTo,
            type = "interactive",
            interactive = new
            {
                type = "button",
                body = new { text = body },
                action = new
                {
                    buttons = new[]
                    {
                        new { type = "reply", reply = new { id = $"CONFIRM_{appointmentId}", title = "✅ Onayla" } },
                        new { type = "reply", reply = new { id = $"REJECT_{appointmentId}", title = "❌ Reddet" } }
                    }
                }
            }
        };

        await PostToMetaGraphAsync(phoneId, payload, token, cancellationToken);
    }

    private async Task PostToMetaGraphAsync(string phoneId, object payload, string token, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/v19.0/{phoneId}/messages");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Meta Graph API Hatası: {Error}", err);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp mesajı gönderilirken hata oluştu.");
        }
    }

    private string FormatPhoneNumber(string raw)
    {
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0")) digits = "90" + digits[1..];
        if (!digits.StartsWith("90") && digits.Length == 10) digits = "90" + digits;
        return digits;
    }
}