using API.Entities;

namespace API.Extensions
{
	public static class QueryableExtensions
	{
		public static IQueryable<Message> MarkUnreadAsRead(this IQueryable<Message> query, string currentUsername)
		{
			IQueryable<Message> unreadMessages = query.Where(m => m.DateRead == null
				&& m.RecipientUsername == currentUsername);

			if (unreadMessages.Any())
			{
				foreach (Message message in unreadMessages)
				{
					message.DateRead = DateTime.UtcNow;
				}
			}

			return query;
		}
	}
}