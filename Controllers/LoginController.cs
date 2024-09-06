using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Authentication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Google;
using Authentication.Context;

namespace Authentication.Controllers
{
    public class LoginController : Controller
    {
        private readonly DataContext _context; 
        private readonly AuthService _authService;
        private readonly ILogger<LoginController> _logger;

        public LoginController(DataContext context, ILogger<LoginController> logger, AuthService authService)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        [Authorize]
        public IActionResult Home()
        {
            return View();
        }

        public IActionResult Login()
        {
            if (TempData.ContainsKey("LoginMessage"))
            {
                ModelState.AddModelError("", TempData["LoginMessage"].ToString());
                TempData.Remove("LoginMessage");
            }


            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserLoginRequest model)
        { 
            if (string.IsNullOrEmpty(model.Password) || model.Password.Length < 6) 
            {
                return Login();
            }

            var result = await _authService.Login(model);


            if (!result.success)
            {

                ModelState.AddModelError("", result.message);
                return Login();
            }

            var user = await _context.GetUser(model.EmailOrUsername);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, "User")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            ModelState.AddModelError("", result.message);
            return RedirectToAction("Home");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(UserRegisterRequest model)
        {
            if (string.IsNullOrEmpty(model.Password) || model.Password.Length < 6)
            {
                return Login();
            }

            var result = await _authService.Register(model);

            if (!result.success)
            {
                ModelState.AddModelError("", result.message);
                return Login();
            }


            TempData["LoginMessage"] = "Account created, check your email for verification link.";

            return RedirectToAction("Login");

        }

        public IActionResult ForgotPassword() 
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest model)
        {

            var result = await _authService.ForgotPassword(model);

            if (!result.success)
            {
                ModelState.AddModelError("", result.message);
                return View();
            }

            ModelState.AddModelError("", result.message);
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            ResetPasswordRequest model = new ResetPasswordRequest { Token = token };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest model)
        {

            var result = await _authService.ResetPassword(model);

            if (!result.success)
            {
                ModelState.AddModelError("", result.message);
                return View();
            }

            TempData["LoginMessage"] = "Password reset, please log in.";
            return RedirectToAction("Login");
        }
        public IActionResult Verification()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Verification(string token)
        {
            _logger.Log(LogLevel.Warning, "verifying with token");

            var result = await _authService.Verify(token);

            if (result.success)
            {
                TempData["LoginMessage"] = "Account verified. Please log in.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Invalid or expired token.");
            return View("Error");
        }

        public async Task LoginGoogle()
        {
            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action("GoogleResponse")
                });
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            var claimsIdentity = result.Principal?.Identities.FirstOrDefault();
            var emailClaim = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var nameClaim = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

            if (emailClaim == null || nameClaim == null)
            {
                return RedirectToAction("Login");
            }

            var email = emailClaim.Value;
            var userName = nameClaim.Value;

            var user = await _context.GetUser(email);

            if (user != null && user.AccountType == AccountType.Manual)
            {
                TempData["LoginMessage"] = "Account is associated with a non google account, must log in manually.";
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login");
            }

            if (user == null)
            {
                var registerResult = await _authService.Register(new UserRegisterRequest()
                {
                    Email = email,
                    Username = userName
                });

                if (!registerResult.success)
                {
                    ModelState.AddModelError("", registerResult.message);
                    return Login();
                }
            }

            var _user = await _context.GetUser(email);

            var claims = new List<Claim>()
            {
                 new Claim(ClaimTypes.Name, _user.Username),
                 new Claim(ClaimTypes.Role, "User"),
            };
            
            var _claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(_claimsIdentity));

            return RedirectToAction("Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
