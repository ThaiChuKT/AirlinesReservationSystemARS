namespace ARS.DTO
{
    public class PaymentGatewayRequestDTO
    {
        public int ReservationID { get; set; }
        public string PaymentMethod { get; set; } = "CreditCard";
        public decimal Amount { get; set; }        // số tiền cần thanh toán
    }
}
