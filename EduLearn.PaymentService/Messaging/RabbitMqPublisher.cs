using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace EduLearn.PaymentService.Messaging;

/// <summary>
/// Publishes payment events to RabbitMQ exchange.
/// PaymentService calls this after verifying a successful payment.
/// EnrollmentService listens and auto-enrolls the student.
/// Falls back gracefully if RabbitMQ not running.
/// </summary>
public class RabbitMqPublisher
{
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitMqPublisher> _logger;

    private const string ExchangeName = "edulearn.events";

    public RabbitMqPublisher(IConfiguration config, ILogger<RabbitMqPublisher> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>Publish PaymentSuccess event so EnrollmentService auto-enrolls student.</summary>
    public void PublishPaymentSuccess(PaymentSuccessEvent evt)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:Host"]     ?? "localhost",
                Port     = int.Parse(_config["RabbitMQ:Port"] ?? "5672"),
                UserName = _config["RabbitMQ:Username"] ?? "guest",
                Password = _config["RabbitMQ:Password"] ?? "guest"
            };

            using var connection = factory.CreateConnection();
            using var channel    = connection.CreateModel();

            // Declare exchange (durable — survives RabbitMQ restart)
            channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);

            var json  = JsonSerializer.Serialize(evt);
            var body  = Encoding.UTF8.GetBytes(json);

            // Persistent message — survives RabbitMQ restart
            var props = channel.CreateBasicProperties();
            props.Persistent = true;

            channel.BasicPublish(
                exchange:   ExchangeName,
                routingKey: "payment.success",
                basicProperties: props,
                body: body);

            _logger.LogInformation(
                "✅ RabbitMQ published: PaymentSuccess for Student {S} Course {C}",
                evt.StudentId, evt.CourseId);
        }
        catch (Exception ex)
        {
            // RabbitMQ not running — log warning, payment still saved in DB
            _logger.LogWarning(
                "⚠️ RabbitMQ publish failed: {Msg}. Payment saved but enrollment needs manual trigger.",
                ex.Message);
        }
    }
}

/// <summary>Event payload published to RabbitMQ after successful payment.</summary>
public class PaymentSuccessEvent
{
    public int     PaymentId { get; set; }
    public int     StudentId { get; set; }
    public int     CourseId  { get; set; }
    public decimal Amount    { get; set; }
    public string  OrderId   { get; set; } = string.Empty;
    public DateTime PaidAt   { get; set; }
}
