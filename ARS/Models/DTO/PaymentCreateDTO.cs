namespace ARS.DTO
{
    public class PaymentCreateDTO
    {
        public int ReservationID { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "CreditCard";
    }
}
