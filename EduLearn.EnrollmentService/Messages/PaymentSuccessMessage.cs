namespace EduLearn.EnrollmentService.Messages;

/// <summary>
/// Message published to RabbitMQ by PaymentService after successful payment.
/// EnrollmentService listens for this and auto-enrolls the student.
/// PDF Non-Functional: MassTransit + RabbitMQ for async enrollment confirmation events.
/// </summary>
public class PaymentSuccessMessage
{
    public int    PaymentId  { get; set; }
    public int    StudentId  { get; set; }
    public int    CourseId   { get; set; }
    public decimal Amount    { get; set; }
    public string OrderId    { get; set; } = string.Empty;
    public DateTime PaidAt   { get; set; }
}
