using E_commerceAPI.DTOs.DisplayDTOs;
using E_commerceAPI.DTOs.InsertDTOs;
using E_commerceAPI.DTOs.UpdateDTOs;
using E_commerceAPI.Models;
using E_commerceAPI.UnitsOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace E_commerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly UnitOfWork unit;

        public OrderController(UnitOfWork unit)
        {
            this.unit = unit;
        }

        // GET: api/Order
        [HttpGet]
        [SwaggerOperation(
        Summary = "This Endpoint returns a list of order elements",
            Description = ""
        )]
        [SwaggerResponse(404, "There weren't any order elements in the database", Type = typeof(void))]
        [SwaggerResponse(200, "Returns A list of order elements", Type = typeof(List<OrderDTO>))]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetOrderList()
        {
            var list = await unit.OrderRepository.GetAllElements(o => o.orderCoupons);

            if (list == null || list.Count == 0) return NotFound();

            var ordrProducts = await unit.OrderProductsRepository.GetAllElements(op => op.product);

            //var products = await unit.ProductRepository.GetAllElements();

            var l = list.Select(o => new OrderDTO { 
                Id = o.Id,
                OrderDate = o.OrderDate,
                TotalPrice = o.TotalPrice,
                UserEmail = User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Email)?.Value,
                UserName = User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Name)?.Value,
                orderCoupons = o.orderCoupons.Select(oc => new OrderCouponDTO
                {
                    TotalPrice = oc.TotalPrice
                }).ToList(),

                orderProducts = ordrProducts.Where(ors => ors.OrderId == o.Id).Select(ors => new OrderProductsDTO
                {
                    oId = ors.OrderId,
                    pId = ors.ProductId,
                    ProductImage = ors.product.ImageUrl,
                    ProductName = ors.product.Name,
                    ProductPrice = ors.product.Price,
                    ProductType = ors.product.Type,
                    Quantity = ors.Quantity,
                    TotalPrice = ors.TotalPrice
                }).ToList()
            }).ToList();

            return Ok(l);
        }

        // GET: api/Order/5
        [HttpGet("{id}")]
        [SwaggerOperation(
        Summary = "This Endpoint returns the specified order element",
            Description = ""
        )]
        [SwaggerResponse(404, "The id that was given doesn't exist in the db", Type = typeof(void))]
        [SwaggerResponse(200, "Returns the specified order element", Type = typeof(OrderDTO))]
        public async Task<ActionResult<OrderDTO>> GetOrderElement(int id)
        {
            var order = await unit.OrderRepository.GetElementWithoutTracking(o => o.Id == id, o => o.orderCoupons);

            if (order == null)
            {
                return NotFound();
            }

            var ordrProducts = await unit.OrderProductsRepository.GetAllElements(op => op.product);

            var o = new OrderDTO()
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                TotalPrice = order.TotalPrice,
                UserEmail = User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Email)?.Value,
                UserName = User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Name)?.Value,
                orderCoupons = order.orderCoupons.Select(oc => new OrderCouponDTO
                {
                    CouponId = oc.CouponId,
                    OrderId = oc.OrderId,
                    TotalPrice = oc.TotalPrice
                }).ToList(),
                orderProducts = ordrProducts.Where(ors => ors.OrderId == order.Id).Select(ors => new OrderProductsDTO
                {
                    oId = ors.OrderId,
                    pId = ors.ProductId,
                    ProductImage = ors.product.ImageUrl,
                    ProductName = ors.product.Name,
                    ProductPrice = ors.product.Price,
                    ProductType = ors.product.Type,
                    Quantity = ors.Quantity,
                    TotalPrice = ors.TotalPrice
                }).ToList()
            };

            return Ok(o);
        }

        // PUT: api/Order/5
        [HttpPut("{id}")]
        [SwaggerOperation(
        Summary = "This Endpoint updates the specified order element",
            Description = ""
        )]
        [SwaggerResponse(404, "The id that was given doesn't exist in the db", Type = typeof(void))]
        [SwaggerResponse(400, "The id that was given doesn't equal the id in the given order object", Type = typeof(void))]
        [SwaggerResponse(202, "Something went wrong, please try again later", Type = typeof(void))]
        [SwaggerResponse(204, "Confirms that the order element was updated successfully", Type = typeof(void))]
        public async Task<IActionResult> PutOrder(int id, OrderUpdateDTO orderDTO)
        {
            var order = await unit.OrderRepository.GetElement(o => o.Id == orderDTO.Id);

            if (order == null)
            {
                return NotFound();
            }

            if (id != order.Id)
            {
                return BadRequest();
            }

            order.TotalPrice = orderDTO.TotalPrice;
            order.Status = orderDTO.Status;

            if (unit.OrderRepository.Edit(order))
            {
                if (await unit.OrderRepository.SaveChanges()) return NoContent();
            }

            return Accepted();
        }

        // POST: api/Order
        [HttpPost]
        [SwaggerOperation(
        Summary = "This Endpoint inserts a order element in the db",
            Description = ""
        )]
        //[SwaggerResponse(409, "There was another element with the same id in the database", Type = typeof(void))]
        [SwaggerResponse(202, "Something went wrong, please try again later", Type = typeof(void))]
        [SwaggerResponse(201, "Returns the inserted order element and the url you can use to get it", Type = typeof(OrderDTO))]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<OrderDTO>> PostOrder(OrderInsertDTO order)
        {
            var o = new Order()
            {
                TotalPrice = order.TotalPrice,
                OrderDate = DateTime.Now,
                Status = 'O',
                UserId = User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier)?.Value
            };

            if (unit.OrderRepository.Add(o))
            {
                if (await unit.OrderRepository.SaveChanges())
                {
                    var or = await unit.OrderRepository.GetElementWithoutTracking(or => or.Id == o.Id, ordr => ordr.orderCoupons);

                    if (or != null)
                    {
                        var ordrProducts = await unit.OrderProductsRepository.GetAllElements(op => op.product);

                        var odto = new OrderDTO()
                        {
                            Id = or.Id,
                            OrderDate = or.OrderDate,
                            TotalPrice = or.TotalPrice,
                            UserEmail = User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Email)?.Value,
                            UserName = User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.Name)?.Value,
                            orderCoupons = or.orderCoupons.Select(oc => new OrderCouponDTO
                            {
                                TotalPrice = oc.TotalPrice
                            }).ToList(),
                            orderProducts = ordrProducts.Where(ors => ors.OrderId == o.Id).Select(ors => new OrderProductsDTO
                            {
                                oId = ors.OrderId,
                                pId = ors.ProductId,
                                ProductImage = ors.product.ImageUrl,
                                ProductName = ors.product.Name,
                                ProductPrice = ors.product.Price,
                                ProductType = ors.product.Type,
                                Quantity = ors.Quantity,
                                TotalPrice = ors.TotalPrice
                            }).ToList()
                        };

                        return CreatedAtAction("GetOrderElement", new { id = o.Id }, odto);
                    }
                }
            }

            return Accepted();
        }

        // DELETE: api/Order/5
        [HttpDelete("{id}")]
        [SwaggerOperation(
        Summary = "This Endpoint deletes a order element from the db",
            Description = ""
        )]
        [SwaggerResponse(404, "The id that was given doesn't exist in the db", Type = typeof(void))]
        [SwaggerResponse(202, "Something went wrong, please try again later", Type = typeof(void))]
        [SwaggerResponse(204, "Confirms that the order element was deleted successfully", Type = typeof(void))]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await unit.OrderRepository.GetElement(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            if (unit.OrderRepository.Delete(order))
            {
                if (await unit.OrderRepository.SaveChanges()) return NoContent();
            }

            return Accepted();
        }
    }
}
