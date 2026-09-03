using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;

namespace SmartBooking.Infrastructure.Services;

public class NetgsmNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NetgsmNotificationService> _logger;

    public NetgsmNotificationService(HttpClient httpClient, IConfiguration configuration, ILogger<NetgsmNotificationService> logger)
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
        // 1. Müşteri Bildirimi
        if (appointment.CustomerWantsWhatsAppNotification && !string.IsNullOrWhiteSpace(customer.PhoneNumber))
        {
            var customerMsg = $"[RandevuCep] Sn. {customer.FullName}, {tenant.Name} isletmesinden randevu talebiniz basariyla alindi. Tarih: {appointment.StartTimeUtc:dd.MM.yyyy HH:mm}, Hizmet: {service.Name}, Tutar: {appointment.Price} TL. Isletme onayladiginda SMS ile bilgilendirileceksiniz.";
            await DispatchMessageAsync(customer.PhoneNumber, customerMsg, cancellationToken);
        }

        // 2. İşletme Sahibine Bildirim
        if (tenant.NotifyOwnerOnNewAppointment && !string.IsNullOrWhiteSpace(tenant.PhoneNumber))
        {
            var ownerMsg = $"[RandevuCep] Sn. {tenant.Name}, yeni bir randevu talebiniz var! Musteri: {customer.FullName} ({customer.PhoneNumber}), Hizmet: {service.Name}, Tarih: {appointment.StartTimeUtc:dd.MM.yyyy HH:mm}. Onaylamak icin yonetim panelinizi acin.";
            await DispatchMessageAsync(tenant.PhoneNumber, ownerMsg, cancellationToken);
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
            Domain.Enums.AppointmentStatus.Rejected => "uzgunuz, isletme tarafindan ONAYLANAMADI.",
            Domain.Enums.AppointmentStatus.Cancelled => "IPTAL EDILDI.",
            _ => "DURUMU GUNCELLENDI."
        };

        var message = $"[RandevuCep] Sn. {customer.FullName}, {tenant.Name} isletmesindeki randevunuz {statusText} Tarih: {appointment.StartTimeUtc:dd.MM.yyyy HH:mm}.";
        await DispatchMessageAsync(customer.PhoneNumber, message, cancellationToken);
    }

    public async Task SendAppointmentReminderAsync(
        Appointment appointment,
        Customer customer,
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        if (!appointment.CustomerWantsWhatsAppNotification || string.IsNullOrWhiteSpace(customer.PhoneNumber))
            return;

        var message = $"[RandevuCep] Hatirlatma: Sn. {customer.FullName}, {tenant.Name} isletmesindeki randevunuza 2 saat kaldi! Saat: {appointment.StartTimeUtc:HH:mm}.";
        await DispatchMessageAsync(customer.PhoneNumber, message, cancellationToken);
    }

    private async Task DispatchMessageAsync(string to, string message, CancellationToken cancellationToken)
    {
        var cleanTo = FormatPhoneNumber(to);
        var header = _configuration["Netgsm:Header"] ?? "RandevuCep";
        var userCode = _configuration["Netgsm:UserCode"];
        var password = _configuration["Netgsm:Password"];

        _logger.LogInformation("\n================== KURUMSAL SMS / BILDIRIM GONDERILDI ==================\nBaslik: {Header}\nAlici: {To}\nMesaj:\n{Message}\n=========================================================================", header, cleanTo, message);

        // Canlı NetGSM bilgileri girilmemişse konsola yazıp testi tamamla
        if (string.IsNullOrWhiteSpace(userCode) || string.IsNullOrWhiteSpace(password) || userCode == "KULLANICI_KODU")
        {
            _logger.LogInformation("NetGSM: Canlı hesap bilgisi tanımlanmadığı için bildirim geliştirme simülasyonunda başarıyla üretildi.");
            return;
        }

        try
        {
            var payload = new
            {
                usercode = userCode,
                password = password,
                msgheader = header,
                gsm = new[] { cleanTo },
                message = message,
                filter = "0",
                startdate = "",
                stopdate = ""
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.netgsm.com.tr/sms/send/rest");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("NetGSM API Canlı Yanıtı: {Response}", responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NetGSM iletisim hatasi.");
        }
    }

    private string FormatPhoneNumber(string raw)
    {
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0")) digits = digits[1..];
        if (digits.StartsWith("90") && digits.Length == 12) digits = digits[2..];
        return digits;
    }
}