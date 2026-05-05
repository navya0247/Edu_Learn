using EduLearn.EnrollmentService.DTOs;
using EduLearn.EnrollmentService.Interfaces;
using EduLearn.EnrollmentService.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EduLearn.EnrollmentService.Messaging;

/// <summary>
/// Background service that listens to RabbitMQ "payment.success" queue.
/// When PaymentService publishes a successful payment → this auto-enrolls the student.
/// Falls back gracefully if RabbitMQ not running — app still works normally.
/// </summary>
public class PaymentSuccessConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration       _config;
    private readonly ILogger<PaymentSuccessConsumer> _logger;

    private IConnection? _connection;
    private IModel?      _channel;

    private const string QueueName    = "payment.success";
    private const string ExchangeName = "edulearn.events";

    public PaymentSuccessConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<PaymentSuccessConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _config       = config;
        _logger       = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
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

            _connection = factory.CreateConnection();
            _channel    = _connection.CreateModel();

            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(QueueName, ExchangeName, routingKey: "payment.success");
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                var body    = ea.Body.ToArray();
                var json    = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<PaymentSuccessMessage>(json);

                if (message != null)
                    await HandlePaymentSuccessAsync(message);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            };

            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("✅ RabbitMQ consumer started — listening on '{Queue}'", QueueName);
        }
        catch (Exception ex)
        {
            // RabbitMQ not running — app still works, enrollment via API works normally
            _logger.LogWarning("⚠️ RabbitMQ not available: {Msg}. Manual enrollment via API still works.", ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Auto-enroll student after PaymentSuccess message received from RabbitMQ.
    /// Uses EnrollRequestDto with CourseId and PaymentId — matches your Enroll() signature.
    /// </summary>
    private async Task HandlePaymentSuccessAsync(PaymentSuccessMessage msg)
    {
        try
        {
            using var scope       = _scopeFactory.CreateScope();
            var enrollmentSvc     = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

            // Prevent duplicate enrollment
            var alreadyEnrolled = await enrollmentSvc.IsEnrolled(msg.StudentId, msg.CourseId);
            if (alreadyEnrolled)
            {
                _logger.LogWarning("Student {S} already enrolled in Course {C} — skipping duplicate",
                    msg.StudentId, msg.CourseId);
                return;
            }

            // ✅ Fixed: matches your actual Enroll(int studentId, EnrollRequestDto dto) signature
            var dto = new EnrollRequestDto
            {
                CourseId  = msg.CourseId,
                PaymentId = msg.PaymentId.ToString()
            };

            await enrollmentSvc.Enroll(msg.StudentId, dto);

            _logger.LogInformation(
                "✅ RabbitMQ: Auto-enrolled Student {S} in Course {C} after payment {P}",
                msg.StudentId, msg.CourseId, msg.PaymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-enroll Student {S} for Course {C} via RabbitMQ",
                msg.StudentId, msg.CourseId);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}