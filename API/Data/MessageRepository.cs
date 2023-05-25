using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
	public class MessageRepository : IMessageRepository
	{
		private readonly DataContext _context;
		private readonly IMapper _mapper;

        public MessageRepository(DataContext context, IMapper mapper)
        {
			_context = context;
			_mapper = mapper;
        }

		public void AddGroup(Group group)
		{
			_context.Groups.Add(group);
		}

		public void AddMessage(Message message)
		{
			_context.Messages.Add(message);
		}

		public void DeleteMessage(Message message)
		{
			_context.Messages.Remove(message);
		}

		public async Task<Connection> GetConnection(string connectionId)
		{
			return await _context.Connections.FindAsync(connectionId);
		}

		public async Task<Group> GetGroupForConnection(string connectionId)
		{
			return await _context.Groups
				.Include(x => x.Connections)
				.Where(x => x.Connections.Any(c => c.ConnectionId == connectionId))
				.FirstOrDefaultAsync();
		}

		public async Task<Message> GetMessage(int id)
		{
			return await _context.Messages.FindAsync(id);
		}

		public async Task<Group> GetMessageGroup(string groupName)
		{
			return await _context.Groups
				.Include(x => x.Connections)
				.FirstOrDefaultAsync(x => x.Name == groupName);
		}

		public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
		{
			IQueryable<Message> query = _context.Messages
				.OrderByDescending(x => x.MessageSent)
				.AsQueryable();

			query = messageParams.Container switch
			{
				"Inbox" => query.Where(u => u.RecipientUsername == messageParams.Username && !u.RecipientDeleted),
				"Outbox" => query.Where(u => u.SenderUsername == messageParams.Username && !u.SenderDeleted),
				_ => query.Where(u => u.RecipientUsername == messageParams.Username && !u.RecipientDeleted && u.DateRead == null)
			};

			IQueryable<MessageDto> queryDto = query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider);

			return await PagedList<MessageDto>
				.CreateAsync(queryDto, messageParams.PageNumber, messageParams.PageSize);
		}

		public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUserName, string recipientUserName)
		{
			IEnumerable<MessageDto> messages = await _context.Messages
				.Where(
					m => m.RecipientUsername == currentUserName && !m.RecipientDeleted &&
					m.SenderUsername == recipientUserName ||
					m.RecipientUsername == recipientUserName && !m.SenderDeleted &&
					m.SenderUsername == currentUserName
				)
				.MarkUnreadAsRead(currentUserName)
				.OrderBy(m => m.MessageSent)
				.ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
				.ToListAsync();

			await _context.SaveChangesAsync();

			return messages;
		}

		public async Task<int> GetNumberOfUnreadMessages(string currentUser)
		{
			return await _context.Messages
				.Where(u => u.RecipientUsername == currentUser && u.DateRead == null)
				.CountAsync();
		}

		public void RemoveConnection(Connection connection)
		{
			_context.Connections.Remove(connection);
		}

		public async Task<bool> SaveAllAsync()
		{
			return await _context.SaveChangesAsync() > 0;
		}
	}
}
