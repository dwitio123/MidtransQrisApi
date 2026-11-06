using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MidtransQrisApi.Data;
using MidtransQrisApi.Models;
using MidtransQrisApi.Services;

namespace MidtransQrisApi.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly MidtransService _midtrans;

	public PaymentsController(
		AppDbContext db,
		MidtransService midtrans
	)
	{
		_db = db;
		_midtrans = midtrans;
	}

	// POST /api/payments/qris
	[HttpPost("qris")]
	public async Task<IActionResult> CreateQris(
		[FromBody] CreatePaymentRequest request
	)
	{
		var orderId = "ORDER-" + Guid.NewGuid().ToString("N");

		try
		{
			// Request QRIS ke Midtrans
			var result = await _midtrans.CreateQris(
				orderId,
				request.Amount
			);

			// Cari URL QR Code dari actions
			var qrUrl = result.Actions
				.FirstOrDefault(x => x.Name == "generate-qr-code")
				?.Url;

			// Simpan transaksi ke database
			var payment = new Payment
			{
				OrderId = orderId,
				TransactionId = result.TransactionId,
				Amount = request.Amount,
				Status = result.TransactionStatus,
				QrCodeUrl = qrUrl
			};

			_db.Payments.Add(payment);
			await _db.SaveChangesAsync();

			return Created(
				$"/api/payments/{orderId}",
				new
				{
					orderId = payment.OrderId,
					transactionId = payment.TransactionId,
					amount = payment.Amount,
					qrUrl = payment.QrCodeUrl,
					status = payment.Status
				}
			);
		}
		catch (HttpRequestException ex)
		{
			return StatusCode(502, new
			{
				message = "Gagal menghubungi Midtrans",
				detail = ex.Message
			});
		}
	}

	// GET /api/payments/{orderId}
	[HttpGet("{orderId}")]
	public async Task<IActionResult> GetStatus(string orderId)
	{
		var payment = await _db.Payments
			.FirstOrDefaultAsync(x => x.OrderId == orderId);

		if (payment == null)
		{
			return NotFound(new
			{
				message = "Transaksi tidak ditemukan"
			});
		}

		try
		{
			// Ambil status terbaru dari Midtrans
			using var result = await _midtrans.GetStatus(orderId);

			var root = result.RootElement;

			var status = root
				.GetProperty("transaction_status")
				.GetString();

			var transactionId = root
				.GetProperty("transaction_id")
				.GetString();

			var amount = root
				.GetProperty("gross_amount")
				.GetDecimal();

			// Pastikan transaksi sesuai dengan database
			if (
				transactionId != payment.TransactionId ||
				amount != payment.Amount
			)
			{
				return StatusCode(502, new
				{
					message = "Data transaksi Midtrans tidak cocok"
				});
			}

			UpdatePaymentStatus(payment, status);

			await _db.SaveChangesAsync();
		}
		catch (HttpRequestException)
		{
			// Jika Midtrans tidak bisa dihubungi,
			// kembalikan status terakhir dari database.
		}

		return Ok(new
		{
			orderId = payment.OrderId,
			amount = payment.Amount,
			status = payment.Status
		});
	}

	// GET /api/payments/{orderId}/qr-image
	[HttpGet("{orderId}/qr-image")]
	public async Task<IActionResult> GetQrImage(string orderId)
	{
		var payment = await _db.Payments
			.FirstOrDefaultAsync(x => x.OrderId == orderId);

		if (payment == null)
			return NotFound();

		try
		{
			var image = await _midtrans.GetQrImage(
				payment.TransactionId
			);

			return File(image, "image/png");
		}
		catch (HttpRequestException)
		{
			return StatusCode(502, new
			{
				message = "Gagal mengambil QR Code"
			});
		}
	}

	// POST /api/payments/notification
	[HttpPost("notification")]
	public async Task<IActionResult> Notification(
		[FromBody] MidtransNotification notification
	)
	{
		// Validasi signature
		if (!_midtrans.VerifySignature(notification))
		{
			return Unauthorized(new
			{
				message = "Invalid signature"
			});
		}

		var payment = await _db.Payments
			.FirstOrDefaultAsync(
				x => x.OrderId == notification.OrderId
			);

		if (payment == null)
			return NotFound();

		// Validasi identitas dan nominal transaksi
		if (
			payment.TransactionId != notification.TransactionId ||
			payment.Amount != decimal.Parse(
				notification.GrossAmount,
				System.Globalization.CultureInfo.InvariantCulture
			) ||
			notification.PaymentType != "qris" ||
			notification.Currency != "IDR"
		)
		{
			return BadRequest(new
			{
				message = "Transaction mismatch"
			});
		}

		UpdatePaymentStatus(
			payment,
			notification.TransactionStatus
		);

		await _db.SaveChangesAsync();

		return Ok(new
		{
			message = "Notification processed"
		});
	}

	private static void UpdatePaymentStatus(
		Payment payment,
		string? newStatus
	)
	{
		if (string.IsNullOrWhiteSpace(newStatus))
			return;

		// Status final tidak boleh diturunkan.
		if (payment.Status is "settlement"
			or "expire"
			or "deny"
			or "cancel")
		{
			return;
		}

		payment.Status = newStatus;
		payment.UpdatedAt = DateTime.UtcNow;
	}
}