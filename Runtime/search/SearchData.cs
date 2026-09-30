using Nox.Instances.Runtime.client;
using Cysharp.Threading.Tasks;
using Nox.Search;
using Nox.Worlds;
using UnityEngine;

namespace Nox.Instances.Runtime.search {
	public class SearchData : IResultData {
		public Instance Reference;
		public IWorld   World;

		public int Id
			=> Reference.Identifier.GetHashCode();

		public string[] TitleArguments
			=> new[] { GetTitle() };

		public UniTask<Texture2D> Image
			=> FetchImage();

		public void OnClick(int menuId)
			=> Client.UiAPI?.SendGoto(menuId, InstancePage.GetStaticKey(), "instance", Reference, World);

		/// <summary>
		/// The instance title, falling back on the linked world one (an instance can be
		/// untitled, in which case the world title is used).
		/// </summary>
		private string GetTitle() {
			if (!string.IsNullOrEmpty(Reference.Title))
				return Reference.Title;
			if (!string.IsNullOrEmpty(World?.Title))
				return World.Title;
			return Reference.Id.ToString();
		}

		/// <summary>
		/// The instance thumbnail, falling back on the linked world one (an instance can
		/// have no image, in which case the world thumbnail is used).
		/// </summary>
		private UniTask<Texture2D> FetchImage() {
			var url = !string.IsNullOrEmpty(Reference.Thumbnail)
				? Reference.Thumbnail
				: World?.Thumbnail;
			return string.IsNullOrEmpty(url)
				? UniTask.FromResult<Texture2D>(null)
				: Main.NetworkAPI.FetchTexture(url);
		}
	}
}