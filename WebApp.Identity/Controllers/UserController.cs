using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Identity.Models;

namespace WebApp.Identity.Controllers {
    public class UserController : Controller {
        private readonly UserManager<User> _userManager;
        private readonly IUserClaimsPrincipalFactory<User> _userClaimsPrincipalFactory;

        public UserController(UserManager<User> userManager, IUserClaimsPrincipalFactory<User> userClaimsPrincipalFactory) {
            this._userManager = userManager;
            this._userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        }

        public IActionResult Index() {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult About() {
            return View();
        }

        public IActionResult Privacy() {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(Login model) {
            if (ModelState.IsValid) {
                var user = await _userManager.FindByNameAsync(model.UserName);

                if (user != null && !await _userManager.IsLockedOutAsync(user)) {
                    if (await _userManager.CheckPasswordAsync(user, model.Password)) {
                        if (!await _userManager.IsEmailConfirmedAsync(user)) {
                            ModelState.AddModelError("", "Invalid email.");

                            return View();
                        }

                        // resets the counting of incorrect attempts
                        await _userManager.ResetAccessFailedCountAsync(user);

                        if (await _userManager.GetTwoFactorEnabledAsync(user)) {
                            var validator = await _userManager.GetValidTwoFactorProvidersAsync(user);

                            if (validator.Contains("Email")) {
                                var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                                System.IO.File.WriteAllText("TwoFactorAuth.txt", token);
                                await HttpContext.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, Store2FA(user.Id, "Email"));
                                return RedirectToAction("TwoFactor");
                            }
                        }

                        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

                        await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);

                        return RedirectToAction("About");
                    }

                    await _userManager.AccessFailedAsync(user);

                    if (await _userManager.IsLockedOutAsync(user)) {
                        // tell user to change password or something
                    }
                }

                ModelState.AddModelError("", "User or password invalid.");

                return View();
            }

            return View();
        }

        private ClaimsPrincipal Store2FA(string userId, string provider) {
            var identity = new ClaimsIdentity(new List<Claim> {
                new Claim("sub", userId),
                new Claim("amr", provider)
            }, IdentityConstants.TwoFactorUserIdScheme);

            return new ClaimsPrincipal(identity);
        }

        [HttpGet]
        public IActionResult Login() {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Register model) {
            if (ModelState.IsValid) {
                var user = await _userManager.FindByNameAsync(model.UserName);

                if (user == null) {
                    user = new User() {
                    UserName = model.UserName,
                    Email = model.UserName
                    };

                    var result = await _userManager.CreateAsync(
                        user, model.Password);

                    if (result.Succeeded) {
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var emailConfirmation = Url.Action("ConfirmEmail", "User",
                            new { token, email = user.Email }, Request.Scheme);

                        System.IO.File.WriteAllText("ConfirmEmail.txt", emailConfirmation);
                    } else {
                        foreach (var error in result.Errors)
                            ModelState.AddModelError("", error.Description);

                        return View();
                    }
                }

                return View("Success");
            }

            return View();
        }

        [HttpGet]
        public IActionResult Register() {
            return View();
        }

        [HttpGet]
        public IActionResult Success() {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email) {
            return View(new ResetPassword { Token = token, Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPassword model) {
            if (ModelState.IsValid) {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null) {
                    var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

                    if (!result.Succeeded) {
                        foreach (var error in result.Errors)
                            ModelState.AddModelError("", error.Description);

                        return View();
                    }

                    return View("Success");
                }
                ModelState.AddModelError("", "Invalid Request");
            }
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword() {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPassword model) {
            if (ModelState.IsValid) {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null) {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var resetUrl = Url.Action("ResetPassword", "User",
                        new { token, email = model.Email }, Request.Scheme);

                    System.IO.File.WriteAllText("ResetPassword.txt", resetUrl);

                    return View("Success");
                } else {
                    // user not found ...
                }
            }

            return View();
        }

        [HttpGet]
        public IActionResult TwoFactor() {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TwoFactor(TwoFactor model) {
            var result = await HttpContext.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);

            if (!result.Succeeded) {
                ModelState.AddModelError("", "Expired Token.");
                return View();
            }

            if (ModelState.IsValid) {
                var user = await _userManager.FindByIdAsync(result.Principal.FindFirstValue("sub"));

                if (user != null) {
                    var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                        user, result.Principal.FindFirstValue("amr"), model.Token);

                    if (isValid) {
                        await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
                        var claimsPrincipal = await _userClaimsPrincipalFactory.CreateAsync(user);
                        await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, claimsPrincipal);
                        return RedirectToAction("About");
                    }

                    ModelState.AddModelError("", "Invalid Token");
                    return View();
                }

                ModelState.AddModelError("", "Invalid Request");
                return View();
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string token, string email) {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null) {
                var result = await _userManager.ConfirmEmailAsync(user, token);

                if (result.Succeeded) {
                    return View("Success");
                }
            }

            return View("Error");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}