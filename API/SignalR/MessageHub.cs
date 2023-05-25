using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
	[Authorize]
	public class MessageHub : Hub
	{
        private readonly IMessageRepository _messageRepository;
		private readonly IUserRepository _userRepository;
		private readonly IMapper _mapper;
		private readonly IHubContext<PresenceHub> _presenceHub;

        public MessageHub(IMessageRepository messageRepository, IUserRepository userRepository,
			IMapper mapper, IHubContext<PresenceHub> presenceHub)
        {
            _messageRepository = messageRepository;
			_userRepository = userRepository;
			_mapper = mapper;
			_presenceHub = presenceHub;
        }

        public override async Task OnConnectedAsync()
        {
            HttpContext httpContext = Context.GetHttpContext();
            string otherUser = httpContext.Request.Query["user"];
            string groupName = GetGroupName(Context.User.GetUsername(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
			Group group = await AddToGroup(groupName);

			await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

            IEnumerable<MessageDto> messages = await _messageRepository
                .GetMessageThread(Context.User.GetUsername(), otherUser);

            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);

			List<string> connections = await PresenceTracker.GetConnectionsForUser(Context.User.GetUsername());
			if (connections is not null)
			{
				await _presenceHub.Clients.Clients(connections).SendAsync("GetNumberOfUnreadMessages",
					await _messageRepository.GetNumberOfUnreadMessages(Context.User.GetUsername()));
			}
		}

        public override async Task OnDisconnectedAsync(Exception exception)
        {
			Group group = await RemoveFromMessageGroup();
			await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
			await base.OnDisconnectedAsync(exception);
		}

        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
			string username = Context.User.GetUsername();

			if (username == createMessageDto.RecipientUsername.ToLower())
				throw new HubException("You cannot send messages to yourself");

			AppUser sender = await _userRepository.GetUserByUsernameAsync(username);
			AppUser recipient = await _userRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername)
				?? throw new HubException("Not found user");

			Message message = new()
			{
				Sender = sender,
				Recipient = recipient,
				SenderUsername = sender.UserName,
				RecipientUsername = recipient.UserName,
				Content = createMessageDto.Content
			};

			string groupName = GetGroupName(sender.UserName, recipient.UserName);
			Group group = await _messageRepository.GetMessageGroup(groupName);

			if (group.Connections.Any(x => x.Username == recipient.UserName))
			{
				message.DateRead = DateTime.UtcNow;
			}
			else
			{
				List<string> connections = await PresenceTracker.GetConnectionsForUser(recipient.UserName);
				if (connections is not null)
				{
					await _presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived",
						new { username = sender.UserName, knownAs = sender.KnownAs });
				}
			}

			_messageRepository.AddMessage(message);

			if (await _messageRepository.SaveAllAsync())
			{
				await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDto>(message));
				List<string> connections = await PresenceTracker.GetConnectionsForUser(recipient.UserName);
				if (connections is not null)
				{
					await _presenceHub.Clients.Clients(connections).SendAsync("GetNumberOfUnreadMessages",
						await _messageRepository.GetNumberOfUnreadMessages(recipient.UserName));
				}
			}
		}

        private static string GetGroupName(string caller, string other)
        {
			bool stringCompare = string.CompareOrdinal(caller, other) < 0;
			return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
		}

		private async Task<Group> AddToGroup(string groupName)
		{
			Group group = await _messageRepository.GetMessageGroup(groupName);
			Connection connection = new(Context.ConnectionId, Context.User.GetUsername());

			if (group is null)
			{
				group = new Group(groupName);
				_messageRepository.AddGroup(group);
			}

			group.Connections.Add(connection);

			if (await _messageRepository.SaveAllAsync())
				return group;

			throw new HubException("Failed to join group");
		}

		private async Task<Group> RemoveFromMessageGroup()
		{
			Group group = await _messageRepository.GetGroupForConnection(Context.ConnectionId);
			Connection connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
			_messageRepository.RemoveConnection(connection);

			if (await _messageRepository.SaveAllAsync())
				return group;

			throw new HubException("Failed to remove from group");
		}
    }
}
