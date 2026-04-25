using EduLearn.PaymentService.DTOs;
using EduLearn.PaymentService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduLearn.PaymentService.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _svc;

    public PaymentController(IPaymentService svc) => _svc = svc;

    /// <summary>Create Razorpay order — Student calls this before payment popup</summary>
    [HttpPost("create-order")]
    [Authorize(Roles = "STUDENT")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var result = await _svc.CreateOrderAsync(dto);
        return Ok(result);
    }

    /// <summary>Verify payment after Razorpay popup closes — triggers enrollment</summary>
    [HttpPost("verify")]
    [Authorize(Roles = "STUDENT")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentDto dto)
    {
        var success = await _svc.VerifyPaymentAsync(dto);
        if (!success)
            return BadRequest(new { message = "Payment verification failed — signature mismatch" });

        return Ok(new { message = "Payment verified successfully", paymentId = dto.PaymentId });
    }

    /// <summary>Get payment by ID</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var payment = await _svc.GetByIdAsync(id);
        return payment == null ? NotFound() : Ok(payment);
    }

    /// <summary>All payments by a student</summary>
    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetByStudent(int studentId) =>
        Ok(await _svc.GetByStudentAsync(studentId));

    /// <summary>All payments for a course — Instructor or Admin only</summary>
    [HttpGet("course/{courseId}")]
    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    public async Task<IActionResult> GetByCourse(int courseId) =>
        Ok(await _svc.GetByCourseAsync(courseId));
}
