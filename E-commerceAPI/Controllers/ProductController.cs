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
    public class ProductController : ControllerBase
    {
        private readonly UnitOfWork unit;

        public ProductController(UnitOfWork unit)
        {
            this.unit = unit;
        }

        // GET: api/Product
        [HttpGet]
        [SwaggerOperation(
        Summary = "This Endpoint returns a list of product elements",
            Description = ""
        )]
        [SwaggerResponse(404, "There weren't any product elements in the database", Type = typeof(void))]
        [SwaggerResponse(200, "Returns A list of product elements", Type = typeof(List<ProductDTO>))]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetProductList()
        {
            var list = await unit.ProductRepository.GetAllElements(p => p.specialOffers, p => p.productReviews);

            if (list == null || list.Count == 0) return NotFound();

            var l = list.Select(p => new ProductDTO { 
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Type = p.Type,
                Rating = p.Rating,
                Description = p.Description,
                ImageUrl = p.ImageUrl,
                Quantity = p.Quantity,
                IsSpecialOffer = (p.specialOffers.FirstOrDefault(sp => sp.ExpireDate > DateTime.Now) != null),
                ExpireDate = p.specialOffers.FirstOrDefault(sp => sp.ExpireDate < DateTime.Now)?.ExpireDate,
                NewPrice = p.specialOffers.FirstOrDefault(sp => sp.ExpireDate < DateTime.Now)?.NewPrice,
                productReviews = p.productReviews.Select(pr => new ProductReviewsDTO
                {
                    Id = pr.ProductId,
                    Rating = pr.Rating,
                    Review = pr.Review,
                    UserName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                }).ToList()
            }).ToList();

            return Ok(l);
        }

        // GET: api/Product/5
        [HttpGet("{id}")]
        [SwaggerOperation(
        Summary = "This Endpoint returns the specified product element",
            Description = ""
        )]
        [SwaggerResponse(404, "The id that was given doesn't exist in the db", Type = typeof(void))]
        [SwaggerResponse(200, "Returns the specified product element", Type = typeof(ProductDTO))]
        public async Task<ActionResult<ProductDTO>> GetProductElement(int id)
        {
            var product = await unit.ProductRepository.GetElementWithoutTracking(p => p.Id == id, p => p.specialOffers, p => p.productReviews);

            if (product == null)
            {
                return NotFound();
            }

            var p = new ProductDTO()
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Type = product.Type,
                Rating = product.Rating,
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                Quantity = product.Quantity,
                IsSpecialOffer = (product.specialOffers.FirstOrDefault(sp => sp.ExpireDate > DateTime.Now) != null),
                ExpireDate = product.specialOffers.FirstOrDefault(sp => sp.ExpireDate < DateTime.Now)?.ExpireDate,
                NewPrice = product.specialOffers.FirstOrDefault(sp => sp.ExpireDate < DateTime.Now)?.NewPrice,
                productReviews = product.productReviews.Select(pr => new ProductReviewsDTO
                {
                    Id = pr.ProductId,
                    Rating = pr.Rating,
                    Review = pr.Review,
                    UserName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                }).ToList()
            };

            return Ok(p);
        }

        // PUT: api/Product/5
        [HttpPut("{id}")]
        [SwaggerOperation(
        Summary = "This Endpoint updates the specified product element",
            Description = ""
        )]
        [SwaggerResponse(404, "The id that was given doesn't exist in the db", Type = typeof(void))]
        [SwaggerResponse(400, "The id that was given doesn't equal the id in the given product object", Type = typeof(void))]
        [SwaggerResponse(202, "Something went wrong, please try again later", Type = typeof(void))]
        [SwaggerResponse(204, "Confirms that the product element was updated successfully", Type = typeof(void))]
        public async Task<IActionResult> PutProduct(int id, ProductUpdateDTO productDTO)
        {
            var product = await unit.ProductRepository.GetElement(p => p.Id == productDTO.Id);

            if (product == null)
            {
                return NotFound();
            }

            if (id != product.Id)
            {
                return BadRequest();
            }

            product.Name = productDTO.Name;
            product.Description = productDTO.Description;
            product.Price = productDTO.Price;
            product.ImageUrl = productDTO.ImageUrl;
            product.Quantity = productDTO.Quantity;
            product.Rating = productDTO.Rating;
            product.Type = productDTO.Type;



            if (unit.ProductRepository.Edit(product))
            {
                if (await unit.ProductRepository.SaveChanges()) 
                {
                    return NoContent(); 
                }
            }

            return Accepted();
        }

        // POST: api/Product
        [HttpPost]
        [SwaggerOperation(
        Summary = "This Endpoint inserts a product element in the db",
            Description = ""
        )]
        //[SwaggerResponse(409, "There was another element with the same id in the database", Type = typeof(void))]
        [SwaggerResponse(202, "Something went wrong, please try again later", Type = typeof(void))]
        [SwaggerResponse(201, "Returns the inserted product element and the url you can use to get it", Type = typeof(ProductDTO))]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<ProductDTO>> PostProduct(ProductInsertDTO product)
        {
            var p = new Product()
            {
               Name = product.Name,
               Quantity = product.Quantity,
               ImageUrl = product.ImageUrl,
               Price = product.Price,
               Description = product.Description,
               Rating = product.Rating,
               Type = product.Type
            };

            if (unit.ProductRepository.Add(p))
            {
                if (await unit.ProductRepository.SaveChanges())
                {
                    var pro = await unit.ProductRepository.GetElementWithoutTracking(prod => prod.Id == p.Id, prod => prod.specialOffers, prod => prod.productReviews);

                    if (pro != null)
                    {
                        var pdto = new ProductDTO()
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Price = p.Price,
                            Type = p.Type,
                            Rating = p.Rating,
                            Description = p.Description,
                            ImageUrl = p.ImageUrl,
                            Quantity = p.Quantity,
                            IsSpecialOffer = (p.specialOffers?.FirstOrDefault(sp => sp.ExpireDate > DateTime.Now) != null),
                            ExpireDate = p.specialOffers?.FirstOrDefault(sp => sp.ExpireDate < DateTime.Now)?.ExpireDate,
                            NewPrice = p.specialOffers?.FirstOrDefault(sp => sp.ExpireDate < DateTime.Now)?.NewPrice,
                            productReviews = p.productReviews?.Select(pr => new ProductReviewsDTO
                            {
                                Id = pr.ProductId,
                                Rating = pr.Rating,
                                Review = pr.Review,
                                UserName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                            }).ToList()
                        };

                        return CreatedAtAction("GetProductElement", new { id = p.Id }, pdto);
                    }
                }
            }

            return Accepted();
        }

        // DELETE: api/Product/5
        [HttpDelete("{id}")]
        [SwaggerOperation(
        Summary = "This Endpoint deletes a product element from the db",
            Description = ""
        )]
        [SwaggerResponse(404, "The id that was given doesn't exist in the db", Type = typeof(void))]
        [SwaggerResponse(202, "Something went wrong, please try again later", Type = typeof(void))]
        [SwaggerResponse(204, "Confirms that the product element was deleted successfully", Type = typeof(void))]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await unit.ProductRepository.GetElement(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            if (unit.ProductRepository.Delete(product))
            {
                if (await unit.ProductRepository.SaveChanges()) return NoContent();
            }

            return Accepted();
        }
    }
}
