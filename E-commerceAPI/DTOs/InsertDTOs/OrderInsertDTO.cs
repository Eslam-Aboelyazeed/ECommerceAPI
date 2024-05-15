using System.ComponentModel.DataAnnotations.Schema;

namespace E_commerceAPI.DTOs.InsertDTOs
{
    public class OrderInsertDTO
    {
        //public char Status { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime OrderDate { get; set; }
        //public string UserId { get; set; }
    }
}
