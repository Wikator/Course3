using API.Helpers;
using System.Text.Json;

namespace API.Extensions
{
	public static class HttpExtensions
	{
		public static void AddPaginationHeader(this HttpResponse response, PaginationHeader header)
		{
			JsonSerializerOptions options = new()
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};

			response.Headers.Add("Pagination", JsonSerializer.Serialize(header, options));
			response.Headers.Add("Access-Control-Expose-Headers", "Pagination");
		}
	}
}
