using System.ComponentModel.DataAnnotations;

namespace MidtransQrisApi.Models;

public class CreatePaymentRequest
{
    [Required]
    [Range(1000, 1000000000)]
    public decimal Amount { get; set; }
}