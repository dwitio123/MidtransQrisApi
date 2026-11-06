using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MidtransQrisApi.Models;

namespace MidtransQrisApi.Services;

public class MidtransService
{
	private readonly HttpClient _httpClient;
	private readonly IConfiguration _configuration;

	private readonly string _serverKey;

	public MidtransService(
		HttpClient httpClient,
		IConfiguration configuration
	)
	{
		_httpClient = httpClient;
		_configuration = configuration;

		_serverKey = configuration["Midtrans:ServerKey"]
			?? throw new InvalidOperationException(
				"Midtrans Server Key belum dikonfigurasi"
			);

		var baseUrl = configuration["Midtrans:BaseUrl"]
			?? "https://api.sandbox.midtrans.com";

		_httpClient.BaseAddress = new Uri(baseUrl);

		var credentials = Convert.ToBase64String(
			Encoding.UTF8.GetBytes(_serverKey + ":")
		);

		_httpClient.DefaultRequestHeaders.Authorization =
			new AuthenticationHeaderValue("Basic", credentials);

		_httpClient.DefaultRequestHeaders.Accept.Add(
			new MediaTypeWithQualityHeaderValue("application/json")
		);
	}

	// Generate QRIS
	public async Task<MidtransChargeResponse> CreateQris(
		string orderId,
		decimal amount
	)
	{
		var payload = new
		{
			payment_type = "qris",

			transaction_details = new
			{
				order_id = orderId,
				gross_amount = amount
			},

			qris = new
			{
				acquirer = "gopay"
			}
		};

		var response = await _httpClient.PostAsJsonAsync(
			"/v2/charge",
			payload
		);

		var json = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode)
		{
			throw new HttpRequestException(
				$"Midtrans error: {json}"
			);
		}

		var result = JsonSerializer.Deserialize<MidtransChargeResponse>(
			json,
			new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			}
		);

		return result ?? throw new Exception(
			"Response Midtrans kosong"
		);
	}

	// Check transaction status
	public async Task<JsonDocument> GetStatus(string orderId)
	{
		var response = await _httpClient.GetAsync(
			$"/v2/{Uri.EscapeDataString(orderId)}/status"
		);

		var json = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode)
		{
			throw new HttpRequestException(
				$"Midtrans status error: {json}"
			);
		}

		return JsonDocument.Parse(json);
	}

	// Verify webhook signature
	public bool VerifySignature(MidtransNotification notification)
	{
		var raw = notification.OrderId
			+ notification.StatusCode
			+ notification.GrossAmount
			+ _serverKey;

		var hash = SHA512.HashData(
			Encoding.UTF8.GetBytes(raw)
		);

		var calculatedSignature = Convert.ToHexString(hash)
			.ToLowerInvariant();

		var expected = notification.SignatureKey;

		if (expected.Length != calculatedSignature.Length)
			return false;

		return CryptographicOperations.FixedTimeEquals(
			Encoding.ASCII.GetBytes(calculatedSignature),
			Encoding.ASCII.GetBytes(expected.ToLowerInvariant())
		);
	}

	// Download QR Code image from Midtrans
	public async Task<byte[]> GetQrImage(string transactionId)
	{
		var url = $"/v2/qris/{Uri.EscapeDataString(transactionId)}/qr-code";

		var response = await _httpClient.GetAsync(url);

		response.EnsureSuccessStatusCode();

		return await response.Content.ReadAsByteArrayAsync();
	}
}