using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
	public class AccountController : BaseApiController
	{
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public AccountController(UserManager<AppUser> userManager, ITokenService tokenService,
            IMapper mapper)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await UserExists(registerDto.UserName))
                return BadRequest("Username is taken");

            AppUser user = _mapper.Map<AppUser>(registerDto);

            IdentityResult userResult = await _userManager.CreateAsync(user, registerDto.Password);

            if (!userResult.Succeeded)
                return BadRequest(userResult.Errors);

            IdentityResult roleResult = await _userManager.AddToRoleAsync(user, "Member");

			if (!userResult.Succeeded)
				return BadRequest(roleResult.Errors);

			return new UserDto
            {
                Username = user.UserName,
                Token = await _tokenService.CreateTokenAsync(user),
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            AppUser user = await _userManager.Users
                .Include(p => p.Photos)
                .SingleOrDefaultAsync(x => x.UserName == loginDto.UserName);

            if (user is null)
                return Unauthorized("Invalid username");

            bool result = await _userManager.CheckPasswordAsync(user, loginDto.Password);

            if (!result)
                return Unauthorized("Invalid password");

			return new UserDto
			{
				Username = user.UserName,
				Token = await _tokenService.CreateTokenAsync(user),
				PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
                KnownAs = user.KnownAs,
                Gender = user.Gender
			};
		}

        private async Task<bool> UserExists(string username)
        {
            return await _userManager.Users.AnyAsync(user => user.UserName == username.ToLower());
        }
    }
}
