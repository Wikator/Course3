using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
	[Authorize]
	public class PresenceHub : Hub
	{
		private readonly PresenceTracker _tracker;
		private readonly IMessageRepository _messageRepository;

        public PresenceHub(PresenceTracker tracker, IMessageRepository messageRepository)
        {
			_tracker = tracker;
			_messageRepository = messageRepository;
        }

        public override async Task OnConnectedAsync()
		{
			bool isOnline = await _tracker.UserConnected(Context.User.GetUsername(), Context.ConnectionId);

			if (isOnline)
			{
				await Clients.Others.SendAsync("UserIsOnline", Context.User.GetUsername());
			}

			string[] currentUsers = await _tracker.GetOnlineUsers();
			await Clients.Caller.SendAsync("GetOnlineUsers", currentUsers);
			await Clients.Caller.SendAsync("GetNumberOfUnreadMessages", await _messageRepository.GetNumberOfUnreadMessages(Context.User.GetUsername()));
		}

		public override async Task OnDisconnectedAsync(Exception exception)
		{
			bool isOffline = await _tracker.UserDisconnected(Context.User.GetUsername(), Context.ConnectionId);

			if (isOffline)
			{
				await Clients.Others.SendAsync("UserIsOffline", Context.User.GetUsername());
			}

			await base.OnDisconnectedAsync(exception);
		}
	}
}
