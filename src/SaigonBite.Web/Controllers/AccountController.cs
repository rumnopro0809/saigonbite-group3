using System.Web.Mvc;
using SaigonBite.Application.Auth;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;
using SaigonBite.Infrastructure.Repositories;
using SaigonBite.Web.Infrastructure;
using SaigonBite.Web.Models;

namespace SaigonBite.Web.Controllers
{
    // HT01 Registration (Local/Tourist BR18), Login/Logout and Admin User management (FR12, FR13).
    // Author: 524H0189.
    public class AccountController : BaseController
    {
        public ActionResult Login()
        {
            return View(new LoginViewModel { Lang = CurrentActorHelper.ResolveLanguage(Request) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            model.Lang = CurrentActorHelper.ResolveLanguage(Request);
            if (!ModelState.IsValid) return View(model);

            using (var db = new SaigonBiteDbContext())
            {
                var service = new AuthService(new EfUserRepository(db), new Sha256PasswordHasher());
                var result = service.Login(model.Identifier, model.Password);
                if (!result.Succeeded)
                {
                    foreach (var err in result.FieldErrors)
                    {
                        ModelState.AddModelError(err.Key, err.Value);
                    }
                    return View(model);
                }

                User user = result.Data;
                Session[CurrentActorHelper.UserIdSessionKey] = user.Id;
                Session["SaigonBite.UserName"] = user.FullName;

                TempData["StatusMessage"] = model.Lang == "vi"
                    ? "Đăng nhập thành công: " + user.FullName + " (" + user.Role + ")."
                    : "Signed in as " + user.FullName + " (" + user.Role + ").";

                return user.Role == ActorRole.Admin
                    ? RedirectToAction("Index", "Menu")
                    : RedirectToAction("Browse", "Menu");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public ActionResult RegisterLocal()
        {
            return View(new RegisterLocalViewModel
            {
                Lang = CurrentActorHelper.ResolveLanguage(Request),
                OtpCode = AuthService.SimulatedOtpCode
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterLocal(RegisterLocalViewModel model)
        {
            model.Lang = CurrentActorHelper.ResolveLanguage(Request);
            if (!ModelState.IsValid) return View(model);

            using (var db = new SaigonBiteDbContext())
            {
                var service = new AuthService(new EfUserRepository(db), new Sha256PasswordHasher());
                var result = service.RegisterLocal(new LocalRegisterInput
                {
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Password = model.Password,
                    OtpCode = model.OtpCode
                });

                if (!result.Succeeded)
                {
                    foreach (var err in result.FieldErrors)
                    {
                        ModelState.AddModelError(err.Key, err.Value);
                    }
                    return View(model);
                }

                Session[CurrentActorHelper.UserIdSessionKey] = result.Data.Id;
                Session["SaigonBite.UserName"] = result.Data.FullName;

                TempData["StatusMessage"] = model.Lang == "vi"
                    ? "Đăng ký tài khoản Khách nội địa thành công (BR18)."
                    : "Local Customer account created successfully (BR18).";
                return RedirectToAction("Browse", "Menu");
            }
        }

        public ActionResult RegisterTourist()
        {
            return View(new RegisterTouristViewModel
            {
                Lang = CurrentActorHelper.ResolveLanguage(Request),
                EmailCode = AuthService.SimulatedEmailCode
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterTourist(RegisterTouristViewModel model)
        {
            model.Lang = CurrentActorHelper.ResolveLanguage(Request);
            if (!ModelState.IsValid) return View(model);

            using (var db = new SaigonBiteDbContext())
            {
                var service = new AuthService(new EfUserRepository(db), new Sha256PasswordHasher());
                var result = service.RegisterTourist(new TouristRegisterInput
                {
                    FullName = model.FullName,
                    PassportNumber = model.PassportNumber,
                    Nationality = model.Nationality,
                    Email = model.Email,
                    Password = model.Password,
                    EmailCode = model.EmailCode
                });

                if (!result.Succeeded)
                {
                    foreach (var err in result.FieldErrors)
                    {
                        ModelState.AddModelError(err.Key, err.Value);
                    }
                    return View(model);
                }

                Session[CurrentActorHelper.UserIdSessionKey] = result.Data.Id;
                Session["SaigonBite.UserName"] = result.Data.FullName;

                TempData["StatusMessage"] = model.Lang == "vi"
                    ? "Đăng ký tài khoản Khách du lịch thành công (BR18)."
                    : "Foreign Tourist account created successfully (BR18).";
                return RedirectToAction("Browse", "Menu");
            }
        }

        // Admin user management (FR12, FR13)
        public ActionResult Users(bool includeDeleted = false)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role;
                ActionResult denied = CurrentActorHelper.RequireAdmin(this, db, out role);
                if (denied != null) return denied;

                var service = new AuthService(new EfUserRepository(db), new Sha256PasswordHasher());
                var model = new AdminUsersViewModel
                {
                    Lang = CurrentActorHelper.ResolveLanguage(Request),
                    IncludeDeleted = includeDeleted,
                    Users = service.GetAdminUsers(role, includeDeleted)
                };
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleUserActive(int id, bool isActive)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role;
                ActionResult denied = CurrentActorHelper.RequireAdmin(this, db, out role);
                if (denied != null) return denied;

                var service = new AuthService(new EfUserRepository(db), new Sha256PasswordHasher());
                var result = service.SetUserActive(role, id, isActive);
                if (!result.Succeeded) return HttpNotFound();

                return RedirectToAction("Users");
            }
        }
    }
}
