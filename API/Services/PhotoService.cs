using API.Helpers;
using API.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace API.Services
{
	public class PhotoService : IPhotoService
	{
		private readonly Cloudinary _cloudinary;

		public PhotoService(IOptions<CloudinarySettings> config)
		{
			Account acc = new(
				config.Value.CloudName,
				config.Value.ApiKey,
				config.Value.ApiSecret
				);

			_cloudinary = new Cloudinary(acc);
		}

		public async Task<ImageUploadResult> AddPhotoAsync(IFormFile file)
		{
			ImageUploadResult uploadResult = new();

			if (file.Length > 0)
			{
				using Stream stream = file.OpenReadStream();
				ImageUploadParams uploadParams = new()
				{
					File = new FileDescription(file.FileName, stream),
					Transformation = new Transformation()
						.Height(500)
						.Width(500)
						.Crop("fill")
						.Gravity("face"),
					Folder = "da-net7"
				};

				uploadResult = await _cloudinary.UploadAsync(uploadParams);
			}

			return uploadResult;
		}

		public async Task<DeletionResult> DeletePhotoAsync(string publicId)
		{
			DeletionParams deteleParams = new(publicId);

			return await _cloudinary.DestroyAsync(deteleParams);
		}
	}
}
