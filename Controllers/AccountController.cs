using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagement.Contexts;
using TaskManagement.Models;

namespace TaskManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AppDbContext appDbContext, ILogger<AccountController> logger) 
        {
            _context = appDbContext;
            _logger = logger;
        }

        [Authorize]
        public IActionResult Home()
        {
            ViewBag.Name = HttpContext.User.Identity.Name;
            return View();
        }

        public IActionResult Registration() 
        {
            if (HttpContext.User.Identity.Name != null)
            {
                return RedirectToAction("Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Registration(RegistrationViewModel model)
        {
            if (ModelState.IsValid) 
            {
                if (_context.IsEmailOrUsernameUsed(model.UserName)) 
                {
                    ModelState.AddModelError("", "Username is already taken.");
                    return View(model);
                }
                if (_context.IsEmailOrUsernameUsed(model.Email))
                {
                    ModelState.AddModelError("", "Email is already taken.");
                    return View(model);
                }

                UserAccount account = new UserAccount();
                account.Email = model.Email;
                account.Password = model.Password;
                account.UserName = model.UserName;
                try
                {
                    _context.UserAccounts.Add(account); 
                    _context.SaveChanges();

                    ModelState.Clear();

                    ViewBag.Message = $"{account.UserName} registered. Logging you in...";
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("","Please enter unique email or password");
                    return View(model);
                }

                //success
                LoginViewModel loginModel = new LoginViewModel();   
                loginModel.UserNameOrEmail = model.UserName;
                loginModel.Password = model.Password;

                return Login(loginModel).Result;
            }

            return View(model);
        }


        public async Task<IActionResult> Login() 
        {
            if (HttpContext.User.Identity.Name != null) 
            {
                return RedirectToAction("Home");
            }

            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("ErrorMessage")))
            {
                ModelState.AddModelError("", HttpContext.Session.GetString("ErrorMessage"));
                HttpContext.Session.SetString("ErrorMessage", "");
            }
            return View();
        }

        [HttpPost]

        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (_context.IsEmailOrUsernameUsed(model.UserNameOrEmail) && _context.IsAccountGoogleAuth(model.UserNameOrEmail)) 
                {
                    ModelState.AddModelError("", "This email already has an account signed in through google.");
                    return View(model);
                }

                var user = _context.UserAccounts.Where(x => (x.UserName == model.UserNameOrEmail || x.Email == model.UserNameOrEmail ) && x.Password == model.Password).FirstOrDefault();

                if (user != null)
                {

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.UserName),
                            new Claim(ClaimTypes.Role, "User")
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                        return RedirectToAction("Home");

                }
                else 
                {
                    ModelState.AddModelError("", "Username/Email or Password is incorrect.");
                }
            }

            return View(model);
        }

        public IActionResult LogOut()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
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
            var result = await  HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            var claimsIdentity = result.Principal?.Identities.FirstOrDefault();
            var emailClaim = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var nameClaim = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

            if (emailClaim == null || nameClaim == null)
            {
                return RedirectToAction("Login");
            }

            var email = emailClaim.Value;
            var userName = nameClaim.Value;

            if (_context.IsEmailOrUsernameUsed(email) && !_context.IsAccountGoogleAuth(email))
            {
                HttpContext.Session.SetString("ErrorMessage", "This email is associated with a non-Google account. Please log in manually.");
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login");
            }

            var user = _context.UserAccounts.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                user = new UserAccount
                {
                    UserName = userName,
                    Email = email,
                    Password = "password",
                };

                _context.UserAccounts.Add(user);
                await _context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, "User")
            };

            var _claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(_claimsIdentity));

            return RedirectToAction("Home");
        }
    }
}
