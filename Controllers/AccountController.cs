using System;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using CSign.Integration.Example.Models;
using CSign.Integration.Example.Client;
using CSign.Integration.Example.Util;

namespace CSign.Integration.Example.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            LoginViewModel  model       = new LoginViewModel();
            Guid            sessionID   = Guid.NewGuid();
            IntegrationServiceClient    csignClient     = new IntegrationServiceClient();
            model.Request       = new ScenarioLoginRequest
            {
                AuthorizationData   = CSignHelper.GetAuthorization(),
                RetUrl              = GetLoginResultUrl(Request, sessionID.ToString()),     ///< A local page on back on this domain where we can trigger a refresh of the parent page.
                Scenario            = 1000,                                                     ///< Any Individual
                LoginUser           = new LoginUser {
                    IndividualParameter = new IndividualParameter {
                        Culture = "sv-SE",                                                      ///< Determines what kind of EID options are available.
                        //SSN     = "",
                    }    
                },
               
                SessionId           = sessionID.ToString(),

            };
            model.Response = csignClient.ScenarioLogin(model.Request);
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult EIDLoginResult(string sessionId)
        {
            IntegrationServiceClient            csignClient     = new IntegrationServiceClient();
            ScenarioLoginCallbackGetRequest     request         = new ScenarioLoginCallbackGetRequest {
                AuthorizationData   = CSignHelper.GetAuthorization(),
                SessionId           = sessionId,
            };
            ScenarioLoginCallbackGetResponse response = csignClient.ScenarioLoginCallbackGet(request);
            if (response.Callback != null && response.Callback.IdentifiedUser != null)
            {
                IdentifiedUser csignUserIdentity =  response.Callback.IdentifiedUser;
                AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(DefaultAuthenticationTypes.ApplicationCookie, ClaimTypes.NameIdentifier, ClaimTypes.Role);
                claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, csignUserIdentity.Individual.SSN, "http://www.w3.org/2001/XMLSchema#string"));
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, string.Format("{0} {1}", 
                                                                                    csignUserIdentity.Individual.FirstName,
                                                                                    csignUserIdentity.Individual.LastName),
                                                                                "http://www.w3.org/2001/XMLSchema#string"));
                claimsIdentity.AddClaim(new Claim("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider", "Custom Identity", "http://www.w3.org/2001/XMLSchema#string"));
                AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = true }, claimsIdentity);
            }
            return View();
        }


        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }



        #region Helpers



        private static string GetLoginResultUrl(HttpRequestBase request, string sessionId)
        {
            return string.Format("{0}://{1}{2}/Account/EIDLoginResult?sessionId={3}",
                                        request.Url.Scheme,
                                        request.Url.Host,
                                        string.Format(":{0}", request.Url.Port),
                                        sessionId
                                        );
        }

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
        #endregion
    }
}