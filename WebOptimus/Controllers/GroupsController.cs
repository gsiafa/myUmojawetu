using AutoMapper;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebOptimus.Controllers;
using WebOptimus.Data;
using WebOptimus.Helpers;
using WebOptimus.Migrations;
using WebOptimus.Models;
using WebOptimus.Models.ViewModel;
using WebOptimus.Services;
using GroupMember = WebOptimus.Models.GroupMember;

public class GroupsController : BaseController
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
    public GroupsController(IMapper mapper, UserManager<User> userManager,
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
    [Authorize]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            // Fetch groups where the user is a confirmed member or admin
            var userGroups = await _db.Groups
                .Select(g => new PaymentGroup
                {
                    Id = g.Id,
                    GroupName = g.GroupName,
                    CreatedBy = g.CreatedBy,
                    DateCreated = g.DateCreated,
                    TotalMembers = _db.GroupMembers.Count(m => m.GroupId == g.Id && m.Status == "Confirmed"),
                    IsAdmin = _db.GroupMembers.Any(m => m.GroupId == g.Id && m.PersonRegNumber == currentUser.PersonRegNumber && m.IsAdmin),
                    IsCreator = g.personRegNumber == currentUser.PersonRegNumber,
                    IsConfirmedMember = _db.GroupMembers.Any(m => m.GroupId == g.Id && m.PersonRegNumber == currentUser.PersonRegNumber && m.Status == "Confirmed")
                })
                .ToListAsync(ct);

            // Separate query for pending requests
            var pendingRequests = await _db.GroupMembers
                .Where(m => m.Status == "Pending" &&
                            _db.GroupMembers.Any(gm => gm.GroupId == m.GroupId && gm.PersonRegNumber == currentUser.PersonRegNumber && gm.IsAdmin))
                .GroupBy(m => m.GroupId)
                .Select(g => new
                {
                    GroupId = g.Key,
                    PendingCount = g.Count()
                })
                .ToListAsync(ct);

            // Add pending request counts to the relevant groups
            foreach (var group in userGroups)
            {
                var groupPending = pendingRequests.FirstOrDefault(pr => pr.GroupId == group.Id);
                group.PendingRequests = groupPending?.PendingCount ?? 0;
            }

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups", "Viewed groups list", ct);

            return View(userGroups);
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Groups", $"Error viewing groups: {ex.Message}", ct);
            TempData["Error"] = "An error occurred while loading groups.";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup(string groupName, CancellationToken ct)
    {
        try
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _db.Users.FirstOrDefaultAsync(a => a.Email == email && a.IsActive == true);

            // Check if group name is already taken
            var existingGroup = await _db.Groups
                .FirstOrDefaultAsync(g => g.GroupName.ToLower() == groupName.ToLower(), ct);

            if (existingGroup != null)
            {
                TempData["Error"] = $"A group with the name '{groupName}' already exists. Please choose a different name or join an existing group instead.";
                return RedirectToAction("Index");
            }

            var group = new PaymentGroup
            {
                GroupName = groupName,
                DependentId = currentUser.DependentId,
                personRegNumber = currentUser.PersonRegNumber,
                CreatedBy = currentUser.Email,
                DateCreated = DateTime.UtcNow
            };

            _db.Groups.Add(group);
            await _db.SaveChangesAsync(ct);

            // Add the creator separately as an Admin
            var creatorMember = new GroupMember
            {
                GroupId = group.Id,
                UserId = currentUser.UserId,
                DependentId = currentUser.DependentId, 
                PersonRegNumber = currentUser.PersonRegNumber,
                Status = "Confirmed",
                DateConfirmed = DateTime.UtcNow,
                IsAdmin = true // Creator is always admin
            };
            _db.GroupMembers.Add(creatorMember);
            await _db.SaveChangesAsync(ct);

            // Fetch dependents, but EXCLUDE the creator's DependentId as well
            var dependents = await _db.Dependants
                .Where(d => d.UserId == currentUser.UserId
                            && d.IsActive == true
                            && d.PersonRegNumber != currentUser.PersonRegNumber
                            && d.Id != currentUser.DependentId) // <-- Exclude creator's DependentId
                .ToListAsync(ct);

            // Log dependents for debugging
            Console.WriteLine($"Found {dependents.Count} dependents for {currentUser.PersonRegNumber}");

            // Add each dependent as a group member
            foreach (var dependent in dependents)
            {
                Console.WriteLine($"Adding dependent: {dependent.PersonRegNumber} (DependentId: {dependent.Id})");

                var groupMember = new GroupMember
                {
                    GroupId = group.Id,
                    UserId = currentUser.UserId,
                    IsFamilyInvited = true,
                    DependentId = dependent.Id, // Correct DependentId
                    PersonRegNumber = dependent.PersonRegNumber,
                    Status = "Confirmed",
                    DateConfirmed = DateTime.UtcNow,
                    IsAdmin = false // Dependents are never admins
                };

                _db.GroupMembers.Add(groupMember);
            }

            await _db.SaveChangesAsync(ct);

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups", $"Created group '{groupName}'", ct);

            TempData["Success"] = "Group created successfully!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Groups", $"Error creating group: {ex.Message}", ct);
            TempData["Error"] = "An error occurred while creating the group.";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> InviteMember(int groupId, string memberEmailOrRegNumber, bool includeFamily, CancellationToken ct)
    {
        try
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);
            var groupMember = await _db.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.PersonRegNumber == currentUser.PersonRegNumber && gm.IsAdmin, ct);

            if (groupMember == null)
            {
                TempData["Error"] = "You do not have permission to invite members to this group.";
                return RedirectToAction("Index");
            }

            var dependent = await _db.Dependants
                .FirstOrDefaultAsync(d => d.Email == memberEmailOrRegNumber || d.PersonRegNumber == memberEmailOrRegNumber, ct);

            if (dependent == null)
            {
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups",
                    $"Error: Dependent not found for email/registration number '{memberEmailOrRegNumber}'", ct);

                TempData["Error"] = "Sorry. Invitation not sent. Please check the Email/Reg number and try again.";
                return RedirectToAction("GroupMembers", new { groupId });
            }

            var existingMember = await _db.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.PersonRegNumber == dependent.PersonRegNumber, ct);

            if (existingMember != null)
            {
                TempData["Error"] = "This person is already a member of the group.";
                return RedirectToAction("GroupMembers", new { groupId });
            }

            var newGroupMember = new GroupMember
            {
                GroupId = groupId,
                PersonRegNumber = dependent.PersonRegNumber,
                UserId = dependent.UserId,
                Status = "Invited",
                DateInvited = DateTime.UtcNow,
                IsAdmin = false,
                IsFamilyInvited = includeFamily
            };
            _db.GroupMembers.Add(newGroupMember);
            await _db.SaveChangesAsync(ct);

            var token = Guid.NewGuid().ToString();
            var expirationTime = DateTime.UtcNow.AddHours(24); // Set expiration to 24 hours

            var emailVerification = new EmailVerification
            {
                UserId = dependent.UserId,
                Token = token,
                ExpirationTime = expirationTime,
                PersonRegNumber = dependent.PersonRegNumber,
                Used = false,
                DateCreated = DateTime.UtcNow,
                CreatedBy = currentUser.Email,
                UpdatedOn = DateTime.UtcNow
            };
            _db.EmailVerifications.Add(emailVerification);
            await _db.SaveChangesAsync(ct);

            var groupname = await _db.Groups.FirstOrDefaultAsync(a => a.Id == groupId, ct);
            var confirmUrl = Url.Action("ConfirmInvite", "Groups", new { groupId, personRegNumber = dependent.PersonRegNumber, token }, Request.Scheme);
            var declineUrl = Url.Action("DeclineInvite", "Groups", new { groupId, personRegNumber = dependent.PersonRegNumber, token }, Request.Scheme);

            const string subject = "You’ve been invited to join a group!";
            const string pathToFile = @"EmailTemplate/GroupInvitation.html";

            string htmlBody;
            using (var reader = System.IO.File.OpenText(pathToFile))
            {
                htmlBody = await reader.ReadToEndAsync(ct);
                htmlBody = htmlBody.Replace("{{userName}}", dependent.PersonName)
                                   .Replace("{{GroupName}}", groupname.GroupName)
                                   .Replace("{{ConfirmUrl}}", confirmUrl)
                                   .Replace("{{DeclineUrl}}", declineUrl)
                                   .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString());
            }

            var message = new PostmarkMessage
            {
                To = dependent.Email,
                Subject = subject,
                HtmlBody = htmlBody,
                From = "info@umojawetu.com"
            };
            await _postmark.SendMessageAsync(message, ct);

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups",
                $"Successfully invited '{dependent.PersonName}' to group '{groupname.GroupName}'", ct);

            TempData["Success"] = $"Successfully invited '{dependent.PersonName}' to join '{groupname.GroupName}' group.";
            return RedirectToAction("GroupMembers", new { groupId });
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Groups", $"Error inviting member: {ex.Message}", ct);
            TempData["Error"] = "An error occurred while inviting the member.";
            return RedirectToAction("GroupMembers", new { groupId });
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GroupMembers(int groupId, CancellationToken ct)
    {
        try
        {
            //  Step 1: Get the logged-in user
            var email = HttpContext.Session.GetString("loginEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _db.Users
                .FirstOrDefaultAsync(a => a.Email == email && a.IsActive == true, ct);

            if (currentUser == null)
            {
                return Unauthorized(); //  Return 401 if user is not found
            }

            //  Step 2: Validate group existence
            var group = await _db.Groups.FindAsync(groupId);
            if (group == null)
            {
                TempData["Error"] = "Group not found.";
                return RedirectToAction("Index");
            }

            //  Step 3: Check if user is a member of this group
            var userGroupMembership = await _db.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.PersonRegNumber == currentUser.PersonRegNumber && gm.Status == "Confirmed", ct);

            if (!userGroupMembership)
            {
                return Forbid(); //  Return 403 Forbidden instead of redirect
            }

            //  Step 4: Fetch all group members and join with Dependants table in one query
            var members = await _db.GroupMembers
                .Where(gm => gm.GroupId == groupId)
                .Join(_db.Dependants, gm => gm.PersonRegNumber, d => d.PersonRegNumber, (gm, d) => new GroupMemberViewModel
                {
                    DependentId = gm.DependentId,
                    FullName = d.PersonName,  //  Ensure correct FullName
                    PersonRegNumber = gm.PersonRegNumber,
                    Status = gm.Status,
                    DateConfirmed = gm.DateConfirmed,
                    GroupId = gm.GroupId,
                    IsAdmin = gm.IsAdmin
                })
                .ToListAsync(ct);

            //  Step 5: Determine if the current user is an admin in the group
            var currentGroupMember = members.FirstOrDefault(m => m.PersonRegNumber == currentUser.PersonRegNumber);
            ViewBag.IsAdmin = currentGroupMember?.IsAdmin ?? false;
            ViewBag.GroupId = groupId;
            ViewBag.CurrentDependentId = currentUser.PersonRegNumber; //  Ensure it's not null

            //  Step 6: Record Audit Log
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups",
                $"Viewed group name: '{group.GroupName}' ID {groupId}", ct);

            return View(members);
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Groups",
                $"Error viewing group members for group ID '{groupId}': {ex.Message}", ct);

            TempData["Error"] = "An error occurred while loading group members.";
            return RedirectToAction("Index");
        }
    }


    [HttpPost]
    public async Task<IActionResult> EditGroupName(int groupId, string groupName, CancellationToken ct)
    {
        try
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            // Fetch the group
            var group = await _db.Groups.FindAsync(groupId);
            if (group == null)
            {
                TempData["Error"] = "Group not found.";
                return RedirectToAction("Index");
            }

            // Log change only if the name has changed
            if (group.GroupName != groupName)
            {
                var oldName = group.GroupName;

                // Update the group name
                group.GroupName = groupName;
                _db.Groups.Update(group);

                // Save the change
                await _db.SaveChangesAsync(ct);

                // Record audit log
                await RecordAuditAsync(
                    currentUser,
                    _requestIpHelper.GetRequestIp(),
                    "EditGroupName",
                    $"Changed group name from '{oldName}' to '{groupName}' for group ID '{groupId}'",
                    ct
                );

                TempData["Success"] = "Group name updated successfully!";
            }
            else
            {
                TempData["Info"] = "No changes were made to the group name.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // Log the error
            await RecordAuditAsync(
                null,
                _requestIpHelper.GetRequestIp(),
                "EditGroupName",
                $"Error updating group name for group ID '{groupId}': {ex.Message}",
                ct
            );

            TempData["Error"] = "An error occurred while updating the group name.";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteGroup(int groupId, CancellationToken ct)
    {
        try
        {
            var group = await _db.Groups.FindAsync(groupId);
            if (group == null)
            {
                TempData["Error"] = "Group not found.";
                return RedirectToAction("Index");
            }

            // Remove all group members
            var members = _db.GroupMembers.Where(m => m.GroupId == groupId);
            _db.GroupMembers.RemoveRange(members);

            // Remove the group
            _db.Groups.Remove(group);
            await _db.SaveChangesAsync(ct);

            TempData["Success"] = "Group deleted successfully.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"An error occurred while deleting the group: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateAdminStatus(int groupId, Guid userId, bool isAdmin, CancellationToken ct)
    {
        try
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);
            var adminMember = await _db.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == currentUser.UserId && gm.IsAdmin, ct);

            if (adminMember == null)
            {
                TempData["Error"] = "You do not have permission to manage admin rights for this group.";
                return RedirectToAction("GroupMembers", new { groupId });
            }

            var memberToUpdate = await _db.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId, ct);

            if (memberToUpdate == null)
            {
                TempData["Error"] = "Group member not found.";
                return RedirectToAction("GroupMembers", new { groupId });
            }

            memberToUpdate.IsAdmin = isAdmin;
            _db.GroupMembers.Update(memberToUpdate);
            await _db.SaveChangesAsync(ct);

            var action = isAdmin ? "granted" : "revoked";
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups", $"{action} admin rights for user '{userId}' in group '{groupId}'", ct);

            TempData["Success"] = $"Admin rights have been {action} successfully.";
            return RedirectToAction("GroupMembers", new { groupId });
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Groups", $"Error updating admin rights: {ex.Message}", ct);
            TempData["Error"] = "An error occurred while updating admin rights.";
            return RedirectToAction("GroupMembers", new { groupId });
        }
    }


    [HttpPost]
    public async Task<IActionResult> RequestToJoinGroup(string groupName, CancellationToken ct)
    {
        try
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            // Find the group by name
            var group = await _db.Groups.FirstOrDefaultAsync(g => g.GroupName == groupName, ct);

            if (group == null)
            {
                TempData["Error"] = $"Group '{groupName}' not found.";
                return RedirectToAction("Index");
            }

            // Check if the user is already a member of the group
            var existingMember = await _db.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == group.Id && gm.PersonRegNumber == currentUser.PersonRegNumber, ct);

            if (existingMember != null)
            {
                TempData["Error"] = "You are already a member of this group or have a pending request.";
                return RedirectToAction("Index");
            }

            // Add request as a pending group member
            var groupMemberRequest = new GroupMember
            {
                GroupId = group.Id,
                UserId = currentUser.UserId,
                PersonRegNumber = currentUser.PersonRegNumber,
                Status = "Pending",
                DateInvited = DateTime.UtcNow,
                IsAdmin = false
            };

            _db.GroupMembers.Add(groupMemberRequest);
            await _db.SaveChangesAsync(ct);

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups",
                $"Requested to join group '{groupName}'", ct);

            TempData["Success"] = $"Request to join group '{groupName}' sent successfully. Group Admin will need to approve. ";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Groups",
                $"Error requesting to join group: {ex.Message}", ct);
            TempData["Error"] = "An error occurred while sending the request.";
            return RedirectToAction("Index");
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> PendingRequests(int? groupId, CancellationToken ct)
    {
        try
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            // Determine if the current user is an admin for any groups
            var adminGroups = await _db.GroupMembers
                .Where(gm => gm.PersonRegNumber == currentUser.PersonRegNumber && gm.IsAdmin)
                .Select(gm => gm.GroupId)
                .ToListAsync(ct);

            // Fetch pending requests visible to the user
            var pendingRequests = await _db.GroupMembers
                .Where(gm =>
                    gm.Status == "Pending" &&
                    (adminGroups.Contains(gm.GroupId) || gm.PersonRegNumber == currentUser.PersonRegNumber))
                .Join(
                    _db.Dependants,
                    gm => gm.PersonRegNumber,
                    d => d.PersonRegNumber,
                    (gm, d) => new GroupMemberViewModel
                    {
                        Id = gm.Id,
                        FullName = d.PersonName,
                        Email = d.Email,
                        GroupId = gm.GroupId,
                        GroupName = _db.Groups.FirstOrDefault(g => g.Id == gm.GroupId).GroupName,
                        Status = gm.Status,
                        DateInvited = gm.DateInvited,
                        CanRevoke = gm.PersonRegNumber == currentUser.PersonRegNumber,
                        CanApproveOrDecline = adminGroups.Contains(gm.GroupId)
                    })
                .ToListAsync(ct);

            // Log the access
            var logMessage = adminGroups.Any()
                ? $"User viewed pending requests for their groups (Admin for groups: {string.Join(", ", adminGroups)})."
                : $"User viewed their own pending requests.";
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "PendingRequests", logMessage, ct);

            return View(pendingRequests);
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "PendingRequests", $"Error: {ex.Message}", ct);
            TempData["Error"] = $"An error occurred while loading pending requests: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> ApproveRequest(int requestId, CancellationToken ct)
    {
        try
        {
            var request = await _db.GroupMembers.FirstOrDefaultAsync(r => r.Id == requestId && r.Status == "Pending", ct);

            if (request == null)
            {
                TempData["Error"] = "Request not found.";
                return RedirectToAction("GroupMembers", new { groupId = request.GroupId });
            }

            request.Status = "Confirmed";
            request.DateConfirmed = DateTime.UtcNow;

            _db.GroupMembers.Update(request);
            await _db.SaveChangesAsync(ct);

            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "ApproveRequest", $"Approved request ID {requestId} for group ID {request.GroupId}.", ct);
            TempData["Success"] = "Request approved successfully.";
            return RedirectToAction("GroupMembers", new { groupId = request.GroupId });
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "ApproveRequest", $"Error: {ex.Message}", ct);
            TempData["Error"] = $"An error occurred while approving the request: {ex.Message}";
            return RedirectToAction("GroupMembers");
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeclineRequest(int requestId, CancellationToken ct)
    {
        try
        {
            var request = await _db.GroupMembers.FirstOrDefaultAsync(r => r.Id == requestId && r.Status == "Pending", ct);

            if (request == null)
            {
                TempData["Error"] = "Request not found.";
                return RedirectToAction("GroupMembers", new { groupId = request.GroupId });
            }

            _db.GroupMembers.Remove(request);
            await _db.SaveChangesAsync(ct);

            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "DeclineRequest", $"Declined request ID {requestId} for group ID {request.GroupId}.", ct);
            TempData["Success"] = "Request declined successfully.";
            return RedirectToAction("GroupMembers", new { groupId = request.GroupId });
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "DeclineRequest", $"Error: {ex.Message}", ct);
            TempData["Error"] = $"An error occurred while declining the request: {ex.Message}";
            return RedirectToAction("GroupMembers");
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GroupRequests(CancellationToken ct)
    {
        try
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            // Fetch join requests using a join between GroupMembers and Groups
            var joinRequests = await _db.GroupMembers
                .Where(gm => gm.PersonRegNumber == currentUser.PersonRegNumber && gm.Status == "Pending")
                .Join(
                    _db.Groups,
                    gm => gm.GroupId,
                    g => g.Id,
                    (gm, g) => new GroupRequestViewModel
                    {
                        RequestId = gm.Id,
                        GroupName = g.GroupName,
                        Status = gm.Status,
                        RequestDate = gm.DateInvited
                    }
                )
                .ToListAsync(ct);

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "GroupRequests", "Fetched join requests for the current user.", ct);
            return View(joinRequests);
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "GroupRequests", $"Error: {ex.Message}", ct);
            TempData["Error"] = "An error occurred while loading your join requests.";
            return RedirectToAction("Index", "Home");
        }
    }


    [HttpGet]
    public async Task<IActionResult> GroupOverview(CancellationToken ct)
    {
        try
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            // Fetch user's groups
            var myGroups = await _db.Groups
                .Where(g => g.CreatedBy == currentUser.Email ||
                            _db.GroupMembers.Any(m => m.GroupId == g.Id && m.PersonRegNumber == currentUser.PersonRegNumber && m.Status == "Confirmed"))
                .Select(g => new GroupViewModel
                {
                    Id = g.Id,
                    GroupName = g.GroupName,
                    TotalMembers = _db.GroupMembers.Count(m => m.GroupId == g.Id && m.Status == "Confirmed")
                })
                .ToListAsync(ct);

            // Fetch user's pending requests
            var pendingRequests = await _db.GroupMembers
                .Where(gm => gm.PersonRegNumber == currentUser.PersonRegNumber && gm.Status == "Pending")
                .Join(
                    _db.Groups,
                    gm => gm.GroupId,
                    g => g.Id,
                    (gm, g) => new PendingRequestViewModel
                    {
                        Id = gm.Id,
                        GroupName = g.GroupName,
                        RequestDate = gm.DateInvited,
                        Status = gm.Status
                    })
                .ToListAsync(ct);

            var model = new GroupOverviewViewModel
            {
                MyGroups = myGroups,
                PendingRequests = pendingRequests
            };

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups", "Viewed group overview page.", ct);

            return View(model);
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Groups", $"Error loading group overview: {ex.Message}", ct);
            TempData["Error"] = "An error occurred while loading your groups and requests.";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    public async Task<IActionResult> RevokeRequest(int requestId, CancellationToken ct)
    {
        try
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            var request = await _db.GroupMembers
                .FirstOrDefaultAsync(gm => gm.Id == requestId && gm.PersonRegNumber == currentUser.PersonRegNumber && gm.Status == "Pending", ct);

            if (request == null)
            {
                TempData["Error"] = "Request not found.";
                return RedirectToAction("PendingRequests");
            }

            _db.GroupMembers.Remove(request);
            await _db.SaveChangesAsync(ct);

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups", $"Revoked join request for group ID: {request.GroupId}.", ct);

            TempData["Success"] = "Your join request has been revoked.";
            return RedirectToAction("PendingRequests");
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Groups", $"Error revoking join request: {ex.Message}", ct);
            TempData["Error"] = "An error occurred while revoking your request.";
            return RedirectToAction("PendingRequests");
        }
    }


    [HttpGet]
    public async Task<IActionResult> MyGroup(CancellationToken ct)
    {
        try
        {
            var email = HttpContext.Session.GetString("loginEmail");
            if (email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.FindByEmailAsync(email);

            // Fetch the groups where the user is a member
            var myGroups = await _db.Groups
                .Where(g => _db.GroupMembers.Any(m => m.GroupId == g.Id && m.PersonRegNumber == currentUser.PersonRegNumber))
                .Select(g => new GroupViewModel
                {
                    Id = g.Id,
                    GroupName = g.GroupName,
                    TotalMembers = _db.GroupMembers.Count(m => m.GroupId == g.Id && m.Status == "Confirmed"),
                    IsAdmin = _db.GroupMembers.Any(m => m.GroupId == g.Id && m.PersonRegNumber == currentUser.PersonRegNumber && m.IsAdmin)
                })
                .ToListAsync(ct);

            // Fetch the pending requests for admin-managed groups
            var pendingRequests = await _db.GroupMembers
                .Where(gm => gm.Status == "Pending" && _db.GroupMembers.Any(m => m.GroupId == gm.GroupId && m.PersonRegNumber == currentUser.PersonRegNumber && m.IsAdmin))
                .Join(_db.Groups,
                      gm => gm.GroupId,
                      g => g.Id,
                      (gm, g) => new PendingRequestViewModel
                      {
                          Id = gm.Id,
                          GroupName = g.GroupName,
                          RequestDate = gm.DateInvited,
                          Status = gm.Status
                      })
                .ToListAsync(ct);

            var viewModel = new MyGroupViewModel
            {
                MyGroups = myGroups,
                PendingRequests = pendingRequests
            };

            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "MyGroup", "Viewed groups and pending requests.", ct);
            return View(viewModel);
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "MyGroup", $"Error loading groups: {ex.Message}", ct);
            TempData["Error"] = $"An error occurred while loading your groups: {ex.Message}";
            return RedirectToAction("Index", "Home");
        }
    }


    public async Task<IActionResult> ConfirmInvite(int groupId, string personRegNumber, string token, CancellationToken ct)
    {
        try
        {
            // Validate group and member
            var emailVerification = await _db.EmailVerifications
            .FirstOrDefaultAsync(ev => ev.Token == token && ev.PersonRegNumber == personRegNumber && !ev.Used && ev.ExpirationTime > DateTime.UtcNow, ct);

            if (emailVerification == null)
            {
                TempData["Error"] = "Invalid or expired invitation link.";
                return RedirectToAction("Login", "Account");
            }
            var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == groupId, ct);
            var groupMember = await _db.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.PersonRegNumber == personRegNumber, ct);

            if (group == null || groupMember == null)
            {
                TempData["Error"] = "Invalid group or member.";
                return RedirectToAction("Login", "Account");
            }          

            if(groupMember.IsFamilyInvited == true)
            {
                var dependents = await _db.Dependants
               .Where(d => d.UserId == groupMember.UserId)
               .ToListAsync(ct);

                // Add each dependent as a group member
                foreach (var dependent in dependents)
                {
                    // Check if dependent already exists in the group
                    var existingDependent = await _db.GroupMembers
                        .AnyAsync(gm => gm.GroupId == group.Id && gm.PersonRegNumber == dependent.PersonRegNumber, ct);

                    if (!existingDependent) // Add only if not existing
                    {
                        var newMember = new GroupMember
                        {
                            GroupId = group.Id,
                            UserId = dependent.UserId,
                            IsFamilyInvited = true,
                            PersonRegNumber = dependent.PersonRegNumber,
                            Status = "Confirmed",
                            DateConfirmed = DateTime.UtcNow
                        };

                        _db.GroupMembers.Add(newMember);
                    }
                }
                await _db.SaveChangesAsync(ct);
            }
            else
            {
                // Update status to Confirmed
                groupMember.Status = "Confirmed";
                groupMember.DateConfirmed = DateTime.UtcNow;

                _db.GroupMembers.Update(groupMember);
                await _db.SaveChangesAsync(ct);
            }

            // Mark the token as used
            emailVerification.Used = true;
            _db.EmailVerifications.Update(emailVerification);
            await _db.SaveChangesAsync(ct);
            // Log audit
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups", $"User {currentUserEmail} confirmed invite for group '{group.GroupName}'.", ct);

            TempData["Success"] = "You have successfully joined the group. Please login to continue.";
            return RedirectToAction("Login", "Account");
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Groups", $"Error confirming invite: {ex.Message}", ct);
            TempData["Error"] = "An error occurred while confirming the invite.";
            return RedirectToAction("Login", "Account");
        }
    }

    public async Task<IActionResult> DeclineInvite(int groupId, string personRegNumber, string token, CancellationToken ct)
    {
        try
        {
            // Validate the invitation token
            var emailVerification = await _db.EmailVerifications
                .FirstOrDefaultAsync(ev => ev.Token == token && ev.PersonRegNumber == personRegNumber && !ev.Used && ev.ExpirationTime > DateTime.UtcNow, ct);

            if (emailVerification == null)
            {
                TempData["Error"] = "Invalid or expired invitation link.";
                return RedirectToAction("Login", "Account");
            }

            // Fetch the group and the group member
            var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == groupId, ct);
            var groupMember = await _db.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.PersonRegNumber == personRegNumber, ct);

            if (group == null || groupMember == null)
            {
                TempData["Error"] = "Invalid group or member.";
                return RedirectToAction("Index");
            }

            // Check if family members should also be declined
            if (groupMember.IsFamilyInvited)
            {
                // Fetch all dependents of the invited user
                var dependents = await _db.Dependants
                    .Where(d => d.UserId == groupMember.UserId)
                    .ToListAsync(ct);

                foreach (var dependent in dependents)
                {
                    // Find and decline each dependent's invitation
                    var familyGroupMember = await _db.GroupMembers
                        .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.PersonRegNumber == dependent.PersonRegNumber, ct);

                    if (familyGroupMember != null)
                    {
                        familyGroupMember.Status = "Declined";
                        familyGroupMember.DateConfirmed = DateTime.UtcNow;
                        _db.GroupMembers.Update(familyGroupMember);
                    }
                }
                await _db.SaveChangesAsync(ct);
            }
            else
            {
                // Update the invited user's status to Declined
                groupMember.Status = "Declined";
                groupMember.DateConfirmed = DateTime.UtcNow;
                _db.GroupMembers.Update(groupMember);
                await _db.SaveChangesAsync(ct);
            }

            // Mark the token as used
            emailVerification.Used = true;
            _db.EmailVerifications.Update(emailVerification);
            await _db.SaveChangesAsync(ct);

            // Log the declination
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups",
                $"User {currentUserEmail} declined invite for group '{group.GroupName}' and family members were {(groupMember.IsFamilyInvited ? "also declined" : "not declined")}.", ct);

            TempData["Success"] = "You have successfully declined the group invite.";
            return RedirectToAction("Login", "Account");
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Groups", $"Error declining invite: {ex.Message}", ct);
            TempData["Error"] = "An error occurred while declining the invite.";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> RemoveMember(int groupId, string personRegNumber, CancellationToken ct)
    {
        try
        {
            // Validate group and member
            var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == groupId, ct);
            var groupMember = await _db.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.PersonRegNumber == personRegNumber, ct);

            if (group == null || groupMember == null)
            {
                TempData["Error"] = "Invalid group or member.";
                return RedirectToAction(nameof(GroupMembers), new { groupId = groupId });
            }

            // Remove the member
            _db.GroupMembers.Remove(groupMember);
            await _db.SaveChangesAsync(ct);

            // Log audit
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups", $"User {currentUserEmail} removed personRegNumber {personRegNumber} from group '{group.GroupName}'.", ct);

            TempData["Success"] = "Member removed successfully.";
            return RedirectToAction(nameof(GroupMembers), new { groupId = groupId });
        }
        catch (Exception ex)
        {
            // Log audit in case of error
            var currentUserEmail = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);
            await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups", $"Error removing member: {ex.Message}", ct);

            TempData["Error"] = "An error occurred while removing the member.";
            return RedirectToAction(nameof(GroupMembers), new { groupId = groupId });
        }
    }

    [HttpPost]
    public async Task<IActionResult> LeaveGroup(int groupId, string personRegNumber, bool leaveWithFamily, CancellationToken ct)
    {
        try
        {
            var email = HttpContext.Session.GetString("loginEmail");
            var currentUser = await _userManager.FindByEmailAsync(email);
            // Remove the current member only
            var groupMember = await _db.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.PersonRegNumber == personRegNumber, ct);
            if (groupMember == null)
            {
                TempData["Error"] = "You are not a member of this group.";
                return RedirectToAction(nameof(GroupMembers), new { groupId });
            }
            if (leaveWithFamily)
            {
                // Remove all members associated with the same UserId
                var groupMembers = await _db.GroupMembers
                    .Where(gm => gm.GroupId == groupId && gm.UserId == groupMember.UserId)
                    .ToListAsync(ct);

                if (!groupMembers.Any())
                {
                    TempData["Error"] = "No family members found in this group.";
                    return RedirectToAction(nameof(GroupMembers), new { groupId });
                }

                _db.GroupMembers.RemoveRange(groupMembers);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups", $"User {email} and their family left group ID {groupId}.", ct);
            }
            else
            {
                _db.GroupMembers.Remove(groupMember);
                await RecordAuditAsync(currentUser, _requestIpHelper.GetRequestIp(), "Groups", $"User {email} left group ID {groupId}.", ct);
            }

            await _db.SaveChangesAsync(ct);

            TempData["Success"] = leaveWithFamily
                ? "You and your family have successfully left the group."
                : "You have successfully left the group.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await RecordAuditAsync(null, _requestIpHelper.GetRequestIp(), "Groups", $"Error leaving group: {ex.Message}", ct);

            TempData["Error"] = "An error occurred while trying to leave the group.";
            return RedirectToAction(nameof(GroupMembers), new { groupId });
        }
    }


}
