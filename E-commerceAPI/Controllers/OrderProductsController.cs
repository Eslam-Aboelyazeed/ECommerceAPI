using E_commerceAPI.DTOs.DisplayDTOs;
using E_commerceAPI.DTOs.InsertDTOs;
using E_commerceAPI.DTOs.UpdateDTOs;
using E_commerceAPI.Models;
using E_commerceAPI.UnitsOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace E_commerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderProductsController : ControllerBase
    {
        private readonly UnitOfWork unit;

        public OrderProductsController(UnitOfWork unit)
        {
            this.unit = unit;
        }

        // PUT: api/OrderProducts/5
        [HttpPut("{oid}")]
        [SwaggerOperation(
        Summary = "This Endpoint updates the specified orderProduct element",
            Description = ""
        )]
        [SwaggerResponse(404, "The id that was given doesn't exist in the db", Type = typeof(void))]
        [SwaggerResponse(400, "The ids that ware given doesn't equal the ids in the given orderProduct object", Type = typeof(void))]
        [SwaggerResponse(202, "Something went wrong, please try again later", Type = typeof(void))]
        [SwaggerResponse(204, "Confirms that the orderProduct element was updated successfully", Type = typeof(void))]
        public async Task<IActionResult> PutOrderProduct(int oid, OrderProductsUpdateDTO orderProductsUpdateDTO)
        {
            var orderProduct = await unit.OrderProductsRepository.GetElement(op => op.ProductId == orderProductsUpdateDTO.ProductId && op.OrderId == orderProductsUpdateDTO.OrderId);

            if (orderProduct == null)
            {
                return NotFound();
            }

            if (oid != orderProduct.OrderId)
            {
                return BadRequest();
            }

            orderProduct.Quantity = orderProductsUpdateDTO.Quantity;
            orderProduct.TotalPrice = orderProductsUpdateDTO.TotalPrice;

            if (unit.OrderProductsRepository.Edit(orderProduct))
            {
                if (await unit.OrderProductsRepository.SaveChanges()) return NoContent();
            }

            return Accepted();
        }

        // POST: api/OrderProducts
        [HttpPost]
        [SwaggerOperation(
        Summary = "This Endpoint inserts an orderProduct element in the db",
            Description = ""
        )]
        //[SwaggerResponse(409, "There was another element with the same id in the database", Type = typeof(void))]
        [SwaggerResponse(202, "Something went wrong, please try again later", Type = typeof(void))]
        [SwaggerResponse(200, "Returns the inserted orderProduct element", Type = typeof(OrderProductsDTO))]
        [SwaggerResponse(204, "Inserted Successfully but there was an error getting the inserted orderProduct element back", Type = typeof(void))]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<CouponDTO>> PostOrderProduct(OrderProductsInsertDTO orderProductsInsertDTO)
        {
            var oProduct = new OrderProducts()
            {
                OrderId = orderProductsInsertDTO.OrderId,
                ProductId = orderProductsInsertDTO.ProductId,
                Quantity = orderProductsInsertDTO.Quantity,
                TotalPrice = orderProductsInsertDTO.TotalPrice
            };

            if (unit.OrderProductsRepository.Add(oProduct))
            {
                if (await unit.OrderProductsRepository.SaveChanges())
                {
                    var orderProduct = await unit.OrderProductsRepository.GetElement(op => op.ProductId == oProduct.ProductId && op.OrderId == op.OrderId, op => op.product);

                    if (orderProduct != null)
                    {
                        var opdto = new OrderProductsDTO()
                        {
                            oId = orderProduct.OrderId, 
                            pId = orderProduct.ProductId,
                            Quantity = orderProduct.Quantity,
                            TotalPrice = orderProduct.TotalPrice,
                            ProductName = orderProduct.product.Name,
                            ProductType = orderProduct.product.Type,
                            ProductPrice = orderProduct.product.Price,
                            ProductImage = orderProduct.product.ImageUrl
                        };

                        return Ok(opdto);
                    }

                    return NoContent();
                }
            }

            return Accepted();
        }

        // DELETE: api/OrderProducts/5
        [HttpDelete("{oid}/{pid}")]
        [SwaggerOperation(
        Summary = "This Endpoint deletes an orderProduct element from the db",
            Description = ""
        )]
        [SwaggerResponse(404, "The id that was given doesn't exist in the db", Type = typeof(void))]
        [SwaggerResponse(202, "Something went wrong, please try again later", Type = typeof(void))]
        [SwaggerResponse(204, "Confirms that the orderProduct element was deleted successfully", Type = typeof(void))]
        public async Task<IActionResult> DeleteOrderProduct(int oid, int pid)
        {
            var orderProduct = await unit.OrderProductsRepository.GetElement(op => op.OrderId == oid && op.ProductId == pid);
            if (orderProduct == null)
            {
                return NotFound();
            }

            if (unit.OrderProductsRepository.Delete(orderProduct))
            {
                if (await unit.OrderProductsRepository.SaveChanges()) return NoContent();
            }

            return Accepted();
        }
    }
}
