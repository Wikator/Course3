using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.Execution;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
	public class UserRepository : IUserRepository
	{
		private readonly DataContext _context;
		private readonly IMapper _mapper;

        public UserRepository(DataContext context, IMapper mapper)
        {
            _context = context;
			_mapper = mapper;
        }
        public async Task<IEnumerable<AppUser>> GetUsersAsync()
		{
			return await _context.Users
				.Include(p => p.Photos)
				.ToListAsync();
		}

		public async Task<AppUser> GetUserByIdAsync(int id)
		{
			return await _context.Users
				.FindAsync(id);
		}

		public async Task<AppUser> GetUserByUsernameAsync(string username)
		{
			return await _context.Users
				.Include(p => p.Photos)
				.SingleOrDefaultAsync(user => user.UserName == username);
		}

		public async Task<bool> SaveAllAsync()
		{
			return await _context.SaveChangesAsync() > 0;
		}

		public void Update(AppUser user)
		{
			_context.Entry(user).State = EntityState.Modified;
		}

		public async Task<PagedList<MemberDto>> GetMembersAsync(UserParams userParams)
		{
			IQueryable<AppUser> query = _context.Users.AsQueryable();

			query = query.Where(user => user.UserName != userParams.CurrentUsername);
			query = query.Where(user => user.Gender == userParams.Gender);

			DateOnly minDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-userParams.MaxAge - 1));
			DateOnly maxDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-userParams.MinAge));

			query = query.Where(user => user.DateOfBirth >= minDob
							&& user.DateOfBirth <= maxDob);

			query = userParams.OrderBy switch
			{
				"created" => query.OrderByDescending(user => user.Created),
				_ => query.OrderByDescending(user => user.LastActive)
			};

			return await PagedList<MemberDto>.CreateAsync(
				query.AsNoTracking().ProjectTo<MemberDto>(_mapper.ConfigurationProvider),
				userParams.PageNumber,
				userParams.PageSize);
		}

		public async Task<MemberDto> GetMemberAsync(string username)
		{
			return await _context.Users
				.Where(x => x.UserName == username)
				.ProjectTo<MemberDto>(_mapper.ConfigurationProvider)
				.SingleOrDefaultAsync();
		}
	}
}
