using Microsoft.AspNetCore.Mvc;
using SmartBooking.Application.Interfaces;
using SmartBooking.Domain.Entities;
using SmartBooking.Domain.Enums;

namespace SmartBooking.Api.Controllers;

[ApiController]
[Route("api/test-whatsapp")]
public class TestWhatsAppController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public TestWhatsAppController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> TestSend([FromQuery] string phone, CancellationToken cancellationToken)
    {
        var dummyTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Berber Nail",
            PhoneNumber = phone,
            NotifyOwnerOnNewAppointment = true
        };

        var dummyCustomer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Gökçe Soylu",
            PhoneNumber = phone
        };

        var dummyStaff = new Staff
        {
            Id = Guid.NewGuid(),
            FullName = "Hasan Can"
        };

        var dummyService = new Service
        {
            Id = Guid.NewGuid(),
            Name = "Saç Kesimi",
            Price = 700
        };

        var dummyAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = dummyTenant.Id,
            StartTimeUtc = DateTime.UtcNow.AddDays(1),
            EndTimeUtc = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Price = 700,
            Status = AppointmentStatus.Pending,
            CustomerWantsWhatsAppNotification = true
        };

        await _notificationService.SendAppointmentRequestNotificationAsync(
            dummyAppointment,
            dummyTenant,
            dummyStaff,
            dummyService,
            dummyCustomer,
            cancellationToken);

        return Ok(new { message = "Test isteği Meta API'ye gönderildi. Terminal loglarını inceleyin." });
    }
}