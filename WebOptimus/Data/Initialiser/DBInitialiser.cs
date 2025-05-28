namespace WebOptimus.Data.Initialiser
{
    using WebOptimus.Models;
    using WebOptimus.StaticVariables;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;

    public class DBInitialiser : IDBInitialiser
    {
        private readonly ApplicationDbContext _db;

        private readonly UserManager<User> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public DBInitialiser(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _db = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }
        public void Initialise()
        {
            try
            {
                if(_db.Database.GetPendingMigrations().Any())
                {
                    //_db.Database.Migrate();
                }
            }
            catch
            { }

            if (_db.Roles.Any(r => r.Name == RoleList.GeneralAdmin)) return;

            _roleManager.CreateAsync(new IdentityRole(RoleList.GeneralAdmin)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(RoleList.LocalAdmin)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(RoleList.Member)).GetAwaiter().GetResult();

            _userManager.CreateAsync(new User
            {
                UserName = _configuration["DBSetup:Email"],
                NormalizedUserName = _configuration["DBSetup:Email"].ToUpper(),
                NormalizedEmail = _configuration["DBSetup:Email"].ToUpper(),
                Email = _configuration["DBSetup:Email"],
                UserId= Guid.NewGuid(),
                EmailConfirmed = true,              
                FirstName = _configuration["DBSetup:FirstName"],
                Surname = _configuration["DBSetup:LastName"],
                PhoneNumber = "07446193908",
                DateCreated = DateTime.Now,
            }, _configuration["DBSetup:Password"]).GetAwaiter().GetResult();

          



            User user = _db.Users.FirstOrDefault(u => u.Email == _configuration["DBSetup:Email"]);         

            _userManager.AddToRoleAsync(user, RoleList.GeneralAdmin).GetAwaiter().GetResult();          
        }
    }
}
