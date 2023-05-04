using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;

        public UsersController(IUserRepository userRepository, IMapper mapper,
            IPhotoService photoService)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _photoService = photoService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers()
        {
            IEnumerable<MemberDto> users = await _userRepository.GetMembersAsync();


            return Ok(users);
        }

        [HttpGet("{username}")]
        public async Task<ActionResult<MemberDto>> GetUser(string username)
        {
            return await _userRepository.GetMemberAsync(username);

        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {;
            AppUser user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            if (user == null)
                return NotFound();

            _mapper.Map(memberUpdateDto, user);

            if (await _userRepository.SaveAllAsync())
                return NoContent();

            return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            AppUser user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            if (user is null)
                return NotFound();

            ImageUploadResult result = await _photoService.AddPhotoAsync(file);

            if (result.Error is not null)
                return BadRequest(result.Error.Message);

            Photo photo = new()
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId,
                IsMain = user.Photos.Count == 0
            };

            user.Photos.Add(photo);

            if (await _userRepository.SaveAllAsync())
                return CreatedAtAction(nameof(GetUser), new { username = user.UserName },
                    _mapper.Map<PhotoDto>(photo));

            return BadRequest("Problem adding photo");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            AppUser user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            if (user is null)
                return NotFound();

            Photo photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if (photo is null)
				return NotFound();

            if (photo.IsMain)
                return BadRequest("This is already your main photo");

            Photo currentMain = user.Photos.FirstOrDefault(x => x.IsMain);

            if (currentMain is not null)
				currentMain.IsMain = false;

            photo.IsMain = true;

			if (await _userRepository.SaveAllAsync())
				return NoContent();

			return BadRequest("Failed to set main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            AppUser user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            if (user is null)
				return NotFound();

            Photo photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if (photo is null)
                return NotFound();

            if (photo.IsMain)
                return BadRequest("You cannot delete your main photo");

            if (photo.PublicId is not null)
            {
                DeletionResult result = await _photoService.DeletePhotoAsync(photo.PublicId);

                if (result.Error is not null)
                    return BadRequest(result.Error.Message);

            }

            user.Photos.Remove(photo);

            if (await _userRepository.SaveAllAsync())
				return Ok();

            return BadRequest("Problem deleting photo");
        }
    }
}
