using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
	[Authorize]
	public class LikesRepository : ILikesRepository
	{
		private readonly DataContext _context;
		private readonly IMapper _mapper;

		public LikesRepository(DataContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
		}

		public async Task<UserLike> GetUserLike(int sourceUserId, int targetUserId)
		{
			return await _context.Likes.FindAsync(sourceUserId, targetUserId);
		}

		public async Task<PagedList<LikeDto>> GetUserLikes(LikesParams likesParamas)
		{
			IQueryable<AppUser> users = _context.Users.OrderBy(u => u.UserName).AsQueryable();
			IQueryable<UserLike> likes = _context.Likes.AsQueryable();

			if (likesParamas.Predicate == "liked")
			{
				likes = likes.Where(like => like.SourceUserId == likesParamas.UserId);
				users = likes.Select(like => like.TargetUser);
			}

			if (likesParamas.Predicate == "likedBy")
			{
				likes = likes.Where(like => like.TargetUserId == likesParamas.UserId);
				users = likes.Select(like => like.SourceUser);
			}

			IQueryable<LikeDto> likedUers = users.ProjectTo<LikeDto>(_mapper.ConfigurationProvider);

			return await PagedList<LikeDto>.CreateAsync(likedUers, likesParamas.PageNumber, likesParamas.PageSize);
		}

		public async Task<AppUser> GetUserWithLikes(int userId)
		{
			return await _context.Users
				.Include(x => x.LikedUsers)
				.FirstOrDefaultAsync(x => x.Id == userId);
		}
	}
}
