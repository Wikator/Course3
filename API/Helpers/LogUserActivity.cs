using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Helpers
{
	public class LogUserActivity : IAsyncActionFilter
	{
		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			ActionExecutedContext resultContext = await next();

			if (!resultContext.HttpContext.User.Identity.IsAuthenticated)
				return;

			string userId = resultContext.HttpContext.User.GetUserId();

			IUserRepository userRepository = resultContext.HttpContext.RequestServices.GetService<IUserRepository>();

			AppUser user = await userRepository.GetUserByIdAsync(int.Parse(userId));
			user.LastActive = DateTime.UtcNow;
			await userRepository.SaveAllAsync();
		}
	}
}
