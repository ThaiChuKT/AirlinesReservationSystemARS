namespace ARS.DTO
{
    public class BlockTicketDTO
    {
        public int UserID { get; set; }
        public int FlightID { get; set; }
        public int ScheduleID { get; set; }
        public DateOnly TravelDate { get; set; }
        public string Class { get; set; } = "Economy";
        public int NumAdults { get; set; } = 1;
        public int NumChildren { get; set; } = 0;
        public int NumSeniors { get; set; } = 0;

        // tuỳ chọn: cho phép truyền số phút giữ chỗ (nếu bạn muốn xử lý hết hạn sau này)
        public int? HoldMinutes { get; set; } = 15;
    }
}
