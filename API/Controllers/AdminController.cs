using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
	public class AdminController : BaseApiController
	{
		private readonly UserManager<AppUser> _userManager;

        public AdminController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [Authorize(Policy = "RequireAdminRole")]
		[HttpGet("users-with-roles")]
		public async Task<ActionResult> GetUsersWithRoles()
		{
			var users = await _userManager.Users
				.OrderBy(u => u.UserName)
				.Select(u => new
				{
					u.Id,
					Username = u.UserName,
					Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
				})
				.ToListAsync();

			return Ok(users);
		}

		[Authorize(Policy = "RequireAdminRole")]
		[HttpPost("edit-roles/{username}")]
		public async Task<ActionResult> EditRoles(string username, [FromQuery] string roles)
		{
			if (string.IsNullOrEmpty(roles))
				return BadRequest("You need to select at least one role");

			string[] selectedRoles = roles.Split(",").ToArray();

			AppUser user = await _userManager.FindByNameAsync(username);

			if (user is null)
				return NotFound();

			IEnumerable<string> userRoles = await _userManager.GetRolesAsync(user);

			IdentityResult result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

			if (!result.Succeeded)
				return BadRequest("Failed to add to roles");

			result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

			if (!result.Succeeded)
				return BadRequest("Failed to remove from roles");

			return Ok(await _userManager.GetRolesAsync(user));
		}

		[Authorize(Policy = "ModeratePhotoRole")]
		[HttpGet("photos-to-moderate")]
		public ActionResult GetPhotosForModeration()
		{
			return Ok("Admin or moderatos can see this");
		}
	}
}
