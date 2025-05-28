    using AutoMapper;
    using DocumentFormat.OpenXml.InkML;
    using DocumentFormat.OpenXml.Wordprocessing;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;
    using System.Globalization;
    using System.Security.Claims;
    using WebOptimus.Data;
    using WebOptimus.Helpers;
    using WebOptimus.Models;
    using WebOptimus.Models.ViewModel;
    using WebOptimus.Services;
    using WebOptimus.StaticVariables;


    namespace WebOptimus.Controllers
    {

        public class DonorController : BaseController
        {


            private readonly IMapper _mapper;
            private readonly SignInManager<User> _signInManager;
            private readonly RoleManager<IdentityRole> _roleManager;
            private readonly IUserStore<User> _userStore;
            private readonly IDataProtector protector;
            private readonly IPasswordValidator<User> passwordValidator;
            private readonly IUserValidator<User> userValidator;
            private readonly IPasswordHasher<User> passwordHasher;
            private readonly RequestIpHelper ipHelper;
            private readonly HttpClient httpClient;
            private readonly IWebHostEnvironment _hostEnvironment;
            private readonly IPostmarkClient _postmark;
            public DonorController(IMapper mapper, UserManager<User> userManager,
               SignInManager<User> signInManager, IWebHostEnvironment hostEnvironment, RoleManager<IdentityRole> roleManager, IUserStore<User> userStore,
                RequestIpHelper ipHelper,
              HttpClient httpClient,
                 IPostmarkClient postmark,
               IPasswordHasher<User> passwordHash,
               IUserValidator<User> userValid,
               IPasswordValidator<User> passwordVal,
               ApplicationDbContext db) :
               base(userManager, db, ipHelper)
            {
                _mapper = mapper;
                _signInManager = signInManager;
                _roleManager = roleManager;
                _userStore = userStore;
                passwordValidator = passwordVal;
                userValidator = userValid;
                passwordHasher = passwordHash;
                this.ipHelper = ipHelper;
                this.httpClient = httpClient;
                _postmark = postmark;
                _hostEnvironment = hostEnvironment;
            }


           
            [HttpGet]
            [Authorize(Roles = RoleList.GeneralAdmin)]
            public async Task<IActionResult> AddDonation(int id, CancellationToken ct)
            {
                var currentUserEmail = HttpContext.Session.GetString("loginEmail");
                if (currentUserEmail == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

                var model = new OtherDonationViewModel();

                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "AddDonation", "user navigated to AddDonation page", ct);


                return View(model);
            }


      
      

    }
}