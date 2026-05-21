namespace HyperlocalAI.Application.Appointments;

public interface IAppointmentBookingService
{
    Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppointmentDto>> ListAsync(BusinessVertical? vertical, CancellationToken cancellationToken = default);
}
