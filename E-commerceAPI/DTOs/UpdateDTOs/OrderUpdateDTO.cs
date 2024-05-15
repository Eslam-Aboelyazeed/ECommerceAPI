namespace E_commerceAPI.DTOs.UpdateDTOs
{
    public class OrderUpdateDTO
    {
        public int Id { get; set; }
        public decimal TotalPrice { get; set; }
        public char Status { get; set; }
        //public DateTime OrderDate { get; set; }
    }
}
