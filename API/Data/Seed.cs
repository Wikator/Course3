using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace API.Data
{
	public class Seed
	{
		public static async Task SeedUsers(UserManager<AppUser> userManager,
			RoleManager<AppRole> roleManager)
		{
			if (await userManager.Users.AnyAsync())
				return;

			string userData = await File.ReadAllTextAsync("Data/UserSeedData.json");

			JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

			List<AppUser> users = JsonSerializer.Deserialize<List<AppUser>>(userData, options);

			List<AppRole> roles = new()
			{
				new AppRole() { Name = "Member" },
				new AppRole() { Name = "Admin" },
				new AppRole() { Name = "Moderator" }
			};

			foreach (AppRole role in roles)
			{
				await roleManager.CreateAsync(role);
			}

			foreach (AppUser user in users)
			{
				user.UserName = user.UserName.ToLower();
				await userManager.CreateAsync(user, "Pa$$w0rd");
				await userManager.AddToRoleAsync(user, "Member");
			}

			AppUser admin = new()
			{
				UserName = "admin"
			};

			await userManager.CreateAsync(admin, "Pa$$w0rd");
			await userManager.AddToRolesAsync(admin, new string[] { "Admin", "Moderator" });
		}
	}
}
