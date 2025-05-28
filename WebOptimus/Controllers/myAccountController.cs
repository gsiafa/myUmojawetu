namespace WebOptimus.Controllers
{
    using AutoMapper;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System.Text.Encodings.Web;
    using WebOptimus.Data;
    using WebOptimus.Helpers;
    using WebOptimus.Models;
    using WebOptimus.Models.ViewModel;
    using WebOptimus.Services;


    [ApiController]
    [Route("api/[controller]")]
    public class myAccountController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly SignInManager<User> _signInManager;
        private readonly IPostmarkClient _postmark;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<User> _userStore;
        private readonly IDataProtector protector;
        private readonly IPasswordValidator<User> passwordValidator;
        private readonly IUserValidator<User> userValidator;
        private readonly IPasswordHasher<User> passwordHasher;
        private readonly RequestIpHelper ipHelper;
        private readonly UrlEncoder _urlEncoder;


        public myAccountController(IMapper mapper, UserManager<User> userManager,
            SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager, IUserStore<User> userStore,           
            IDataProtectionProvider dataProtectionProvider, DataProtectionPurposeStrings dataProtectionPurposeStrings,
		   RequestIpHelper ipHelper,
            IPostmarkClient postmark,
            IPasswordHasher<User> passwordHash,
            IUserValidator<User> userValid,
            UrlEncoder urlEncoder,
            IPasswordValidator<User> passwordVal,
            ApplicationDbContext db) :
            base(userManager, db, ipHelper)
        {
            _mapper = mapper;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _userStore = userStore;
            _postmark = postmark;
            passwordValidator = passwordVal;
            userValidator = userValid;
            passwordHasher = passwordHash;
            this.ipHelper = ipHelper;
            _urlEncoder = urlEncoder;
			protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeStrings.EmailAddress);
		}




        [HttpPost]
        [Route("ValidatePassword")]
        public async Task<IActionResult> ValidatePassword(LoginViewModel viewModel, CancellationToken ct)
        {
            var currentUser = await _db.Users.FirstOrDefaultAsync(a=>a.Email == viewModel.Email && a.IsActive == true);
            if (currentUser == null)
            {
                await RecordAuditAsync(null, viewModel.Email, _requestIpHelper.GetRequestIp(),
                    "API Login", "User Account", "User login failed because user could not be found in database.", ct);
                return Unauthorized("Invalid login details.");
            }
            else if (currentUser.IsActive == false)
            {
                
                    await RecordAuditAsync(null, viewModel.Email, _requestIpHelper.GetRequestIp(),
                        "API Login", "User Account", "User login failed because user account is deactivated.", ct);
                    return Unauthorized("Account deactivated.");
                
            }
            var result = await _signInManager.CheckPasswordSignInAsync(currentUser, viewModel.Password, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Login", "User login successfully", ct);
                return Ok("Password is valid.");
            }
            else
            {
                await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Login", "User Account", "User login failed - invalid credentials.", ct);
                return Unauthorized("Invalid login details.");
            }
        }

      
    }
}


