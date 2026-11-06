using System.Text.Json.Serialization;

namespace MidtransQrisApi.Models;

public class MidtransChargeResponse
{
    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; } = "";

    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = "";

    [JsonPropertyName("transaction_status")]
    public string TransactionStatus { get; set; } = "";

    [JsonPropertyName("payment_type")]
    public string PaymentType { get; set; } = "";

    [JsonPropertyName("actions")]
    public List<MidtransAction> Actions { get; set; } = [];
}

public class MidtransAction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}

public class MidtransNotification
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = "";

    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; } = "";

    [JsonPropertyName("transaction_status")]
    public string TransactionStatus { get; set; } = "";

    [JsonPropertyName("status_code")]
    public string StatusCode { get; set; } = "";

    [JsonPropertyName("gross_amount")]
    public string GrossAmount { get; set; } = "";

    [JsonPropertyName("signature_key")]
    public string SignatureKey { get; set; } = "";

    [JsonPropertyName("payment_type")]
    public string PaymentType { get; set; } = "";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "";

    [JsonPropertyName("fraud_status")]
    public string FraudStatus { get; set; } = "";
}