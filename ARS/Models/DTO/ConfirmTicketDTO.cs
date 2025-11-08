namespace ARS.DTO
{
    public class ConfirmTicketDTO
    {
        public int ReservationID { get; set; }
        public int UserID { get; set; }            // xác minh đúng user sở hữu vé
    }
}
