using E_commerceAPI.DTOs.DisplayDTOs;
using E_commerceAPI.DTOs.InsertDTOs;
using E_commerceAPI.DTOs.UpdateDTOs;
using E_commerceAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NuGet.Common;
using Swashbuckle.AspNetCore.Annotations;
using System.Drawing.Imaging;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace E_commerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly SignInManager<ApplicationUser> signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.signInManager = signInManager;
        }

        [HttpPost("/api/register")]
        public async Task<IActionResult> Register(ApplicationUserInsertDTO userInsertDTO)
        {
            if (userInsertDTO == null)
            {
                return BadRequest();
            }

            var userEmail = await userManager.FindByEmailAsync(userInsertDTO.Email);
            if (userEmail != null)
            {
                return Conflict();
            }

            var user = new ApplicationUser()
            {
                UserName = $"{userInsertDTO.FirstName}_{userInsertDTO.LastName}",
                Email = userInsertDTO.Email,
                PasswordHash = userInsertDTO.Password,
                PhoneNumber = userInsertDTO.PhoneNumber,
                Address = userInsertDTO.Address
            };

            var adminRole = await roleManager.FindByNameAsync("Admin");
            var userRole = await roleManager.FindByNameAsync("User");
            if (adminRole == null)
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            if (userRole == null)
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            var identityResult = await userManager.CreateAsync(user, userInsertDTO.Password);


            if (identityResult.Succeeded)
            {

                IdentityResult identityResult1;

                if (user.Email == "admin@email.com")
                {
                    identityResult1 = await userManager.AddToRoleAsync(user, UserRoles.Admin.ToString());
                }
                else
                {
                    identityResult1 = await userManager.AddToRoleAsync(user, UserRoles.User.ToString());
                }

                if (identityResult1.Succeeded)
                {
                    var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action(action: "ConfirmEmail", controller: "Account", values: new { userId = user.Id, token = emailToken }, protocol: Request.Scheme, host: Request.Host.Host + ":" + Request.Host.Port);

                    var smtpClient = new SmtpClient();

                    var mailMessage = new MailMessage("email address", user.Email, "Email Confirmation", $"Please confirm your account by <a href=\"{confirmationLink}\">clicking here</a>.");

                    mailMessage.IsBodyHtml = true;

                    smtpClient.Host = "smtp.gmail.com";

                    smtpClient.Port = 587;

                    smtpClient.Credentials = new NetworkCredential("email address", "password");

                    smtpClient.EnableSsl = true;

                    await smtpClient.SendMailAsync(mailMessage);

                    return NoContent();
                }
                return Accepted();
            }

            return BadRequest(identityResult);
        }

        [HttpPost("/api/login")]
        public async Task<ActionResult<ClaimsDTO>> Login(ApplicationUserLoginDTO userLoginDTO)
        {
            if (userLoginDTO == null)
            {
                return BadRequest();
            }

            var user = await userManager.FindByEmailAsync(userLoginDTO.Email);

            if (user == null)
            {
                return BadRequest();
            }

            var claims = await userManager.GetClaimsAsync(user);

            var sKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("One Secret Key To Encrypt and decrypt Information"));

            var signingCreds = new SigningCredentials(sKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: signingCreds
                );

            var givenToken = new JwtSecurityTokenHandler().WriteToken(token);

            IdentityResult identityResult = new IdentityResult();

            var cl = claims.FirstOrDefault(c => c.Type == "Token");

            if (cl != null)
            {
                identityResult = await userManager.RemoveClaimAsync(user, cl);
            }

            identityResult = await userManager.AddClaimAsync(user, new Claim("Token", givenToken));

            if (identityResult.Succeeded)
            {

                await signInManager.SignInAsync(user, userLoginDTO.IsPersistent);

                ClaimsDTO claimsDTO = new ClaimsDTO()
                {
                    Token = givenToken
                };

                return Ok(claimsDTO);
            }

            return Accepted();
        }

        [HttpPost("/api/logout")]
        public async Task<IActionResult> Logout(string UserId)
        {
            var user = await userManager.FindByIdAsync(UserId);

            if (user == null)
            {
                return BadRequest();
            }

            await signInManager.SignOutAsync();

            return NoContent();
        }

        [HttpGet("/api/confirmemail")]
        public async Task<IActionResult> ConfirmEmail( [FromQuery] string userId, [FromQuery] string token)
        {
            if (userId == null || token == null)
            {
                return BadRequest("Please Try Again Later");
            }

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return BadRequest();
            }

            var identityResult = await userManager.ConfirmEmailAsync(user, token);

            if (identityResult.Succeeded)
            {
                return Ok(identityResult);
            }

            return Accepted();
        }

        [HttpPut("{id:alpha}")]
        [SwaggerOperation(
            Summary = "This Endpoint updates the specified user element",
            Description = "")]
        [SwaggerResponse(404, "The id that was given doesn't exist in the db", Type = typeof(void))]
        [SwaggerResponse(400, "The id that was given doesn't equal the id in the given user object", Type = typeof(void))]
        [SwaggerResponse(202, "Something went wrong, please try again later", Type = typeof(void))]
        [SwaggerResponse(204, "Confirms that the user element was updated successfully", Type = typeof(void))]
        public async Task<IActionResult> PutUser(string id, ApplicationUserUpdateDTO userUpdateDTO)
        {
            var user = await userManager.FindByIdAsync(userUpdateDTO.Id);

            if (user == null)
            {
                return NotFound();
            }

            if (id != user.Id)
            {
                return BadRequest();
            }

            user.PhoneNumber = userUpdateDTO.PhoneNumber;
            user.Address = userUpdateDTO.Address;
            user.Email = userUpdateDTO.Email;
            user.UserName = $"{userUpdateDTO.FirstName} {userUpdateDTO.LastName}";

            var identityResult = await userManager.UpdateAsync(user);

            if (identityResult.Succeeded)
            {
                return NoContent();
            }

            return Accepted();
        }

        [HttpPut("/Api/User/{id:alpha}")]
        [SwaggerOperation(
            Summary = "This Endpoint updates the specified user element password",
            Description = "")]
        [SwaggerResponse(404, "The id that was given doesn't exist in the db", Type = typeof(void))]
        [SwaggerResponse(400, "The id that was given doesn't equal the id in the given user object or the current password was wrong", Type = typeof(void))]
        [SwaggerResponse(202, "Something went wrong, please try again later", Type = typeof(void))]
        [SwaggerResponse(204, "Confirms that the user element password was updated successfully", Type = typeof(void))]
        public async Task<IActionResult> ChangePassword(string id, ApplicationUserUpdatePasswordDTO userUpdatePasswordDTO)
        {
            var user = await userManager.FindByIdAsync(userUpdatePasswordDTO.Id);

            if (user == null)
            {
                return NotFound();
            }

            if (id != user.Id)
            {
                return BadRequest();
            }

            var currentRes = await userManager.CheckPasswordAsync(user, userUpdatePasswordDTO.CurrentPassword);

            if (!currentRes)
            {
                return BadRequest("Wrong Current Password");
            }

            var identityResult = await userManager.ChangePasswordAsync(user, userUpdatePasswordDTO.CurrentPassword, userUpdatePasswordDTO.NewPassword);

            if (identityResult.Succeeded)
            {
                return NoContent();
            }

            return Accepted();
        }
    }
}
