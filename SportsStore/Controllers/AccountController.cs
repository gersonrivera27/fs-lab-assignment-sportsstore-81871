using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportsStore.Models.ViewModels;
using Serilog;

namespace SportsStore.Controllers {

    public class AccountController : Controller {
        private UserManager<IdentityUser> userManager;
        private SignInManager<IdentityUser> signInManager;

        public AccountController(UserManager<IdentityUser> userMgr,
                SignInManager<IdentityUser> signInMgr) {
            userManager = userMgr;
            signInManager = signInMgr;
        }

        public ViewResult Login(string returnUrl) {
            Log.Information("Login page accessed. ReturnUrl: {ReturnUrl}", returnUrl);
            return View(new LoginModel {
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel loginModel) {
            if (ModelState.IsValid) {
                IdentityUser user =
                    await userManager.FindByNameAsync(loginModel.Name);
                if (user != null) {
                    await signInManager.SignOutAsync();
                    if ((await signInManager.PasswordSignInAsync(user,
                            loginModel.Password, false, false)).Succeeded) {
                        Log.Information("User {Username} logged in successfully. Redirecting to {ReturnUrl}", loginModel.Name, loginModel.ReturnUrl ?? "/Admin");
                        return Redirect(loginModel?.ReturnUrl ?? "/Admin");
                    }
                }
                Log.Warning("Failed login attempt for username {Username}", loginModel.Name);
                ModelState.AddModelError("", "Invalid name or password");
            }
            return View(loginModel);
        }

        [Authorize]
        public async Task<RedirectResult> Logout(string returnUrl = "/") {
            Log.Information("User {Username} logged out", User.Identity?.Name);
            await signInManager.SignOutAsync();
            return Redirect(returnUrl);
        }
    }
}
