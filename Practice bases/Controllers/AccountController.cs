using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Practice_bases.Models;
using Practice_bases.ViewModel;

namespace Practice_bases.Controllers;



public class AccountController : Controller
{
    private ApplicationContext _db;
    public AccountController(ApplicationContext context)
    {
        _db = context;
    }
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [Authorize]
    public IActionResult Index()
    {
        String s = User.Identity.Name;
        User user = _db.Users
            .Include(x => x.Role)
            .FirstOrDefault(x => x.Email.Equals(s));
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (ModelState.IsValid)
        {
            User user = await _db.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(u => u.Email == model.Email 
                && u.Password == model.Password);
            if (user != null)
            {
                await Authenticate(user); // аутентификация
 
                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("", "Некорректные логин и(или) пароль");
        }
        return View(model);
    }
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        if (ModelState.IsValid)
        {
            User _user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (_user == null)
            {
                string email = model.Email;
                int mailSymbol = email.IndexOf('@');
                string domain = email.Substring(mailSymbol);
                if (domain.Equals("@mpt.ru"))
                {
                    string key = MailHelper.Generate();
                    MailHelper.SendEmailAsync(email, key).GetAwaiter();
                    Role role = _db.Roles.FirstOrDefault(x => x.Name.Equals("Unconfirmed"));

                    var user = new User()
                    {
                        Email = model.Email,
                        Password = model.Password,
                        Login = model.Login,
                        Role = role,
                        Key = key
                    };
                    // добавляем пользователя в бд
                    _db.Users.Add(user);
                    await _db.SaveChangesAsync();
 
                    await Authenticate(user); // аутентификация
 
                    return RedirectToAction("Index", "Home");
                }
            }
            else
                ModelState.AddModelError("", "Такой пользователь уже существует");
        }
        return View(model);
    }
 
    
    private async Task Authenticate(User user)
    {
        // создаем один claim
        var claims = new List<Claim>
        {
            new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email),
            new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role?.Name)
        };
        // создаем объект ClaimsIdentity
        ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType,
            ClaimsIdentity.DefaultRoleClaimType);
        // установка аутентификационных куки
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
    }
 
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public async Task<IActionResult> Activation(string key)
    {
        var user = _db.Users.FirstOrDefault(x => x.Key.Equals(key));
        Role role = _db.Roles.FirstOrDefault(x => x.Name.Equals("User"));
        if (user != null)
        {
            user.Role = role;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            Authenticate(user);
        }
        return RedirectToAction("Index");
    }

    public IActionResult AccessDenied(string ReturnUrl)
    {
        return View();
    }
    
    
  
}