using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
	public class LikesController : BaseApiController
	{
		private readonly IUserRepository _userRepository;
		private readonly ILikesRepository _likesRepository;

        public LikesController(IUserRepository userRepository, ILikesRepository likesRepository)
        {
            _userRepository = userRepository;
            _likesRepository = likesRepository;
        }

        [HttpPost("like/{username}")]
        public async Task<ActionResult> AddLike(string username)
        {
            int sourceUserId = User.GetUserId();
            AppUser likedUser = await _userRepository.GetUserByUsernameAsync(username);
            AppUser sourceUser = await _likesRepository.GetUserWithLikes(sourceUserId);

            if (likedUser is null)
                return NotFound();

            if (sourceUser.UserName == username)
                return BadRequest("You cannot like yourself");

            UserLike userLike = await _likesRepository.GetUserLike(sourceUserId, likedUser.Id);

            if (userLike is not null)
                return BadRequest("You already like this user");

            userLike = new UserLike
            {
                SourceUserId = sourceUserId,
                TargetUserId = likedUser.Id
            };

            sourceUser.LikedUsers.Add(userLike);
            
            if (await _userRepository.SaveAllAsync())
                return Ok();

            return BadRequest("Failed to like user");
        }

        [HttpDelete("unlike/{username}")]
        public async Task<ActionResult> RemoveLike(string username)
        {
            AppUser sourceUser = await _likesRepository.GetUserWithLikes(User.GetUserId());
            AppUser targetUser = await _userRepository.GetUserByUsernameAsync(username);

            UserLike userLike = await _likesRepository.GetUserLike(sourceUser.Id, targetUser.Id);

            if (userLike is null)
                return NotFound("You currently do not like this user");

            sourceUser.LikedUsers.Remove(userLike);

            if (await _userRepository.SaveAllAsync())
                return Ok();

            return BadRequest("Failed to unlike user");
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<LikeDto>>> GetUserLikes([FromQuery]LikesParams likesParams)
        {
            likesParams.UserId = User.GetUserId();

			PagedList<LikeDto> users = await _likesRepository.GetUserLikes(likesParams);

            Response.AddPaginationHeader(new PaginationHeader(users.CurrentPage,
                users.PageSize, users.TotalCount, users.TotalPages));

			return Ok(users);
		}
    }
}
