using Nox.Instances.Runtime.client;
using Cysharp.Threading.Tasks;
using Nox.Search;
using Nox.Worlds;

namespace Nox.Instances.Runtime.search {
	public class SearchData : IResultData {
		public Instance Reference;
		public IWorld   World;

		public int Id
			=> Reference.Identifier.GetHashCode();

		public string[] TitleArguments
			=> new[] { GetTitle() };

		public UniTask<ImageSource> Image
			=> UniTask.FromResult(
				ImageSource.FromUrl(
					!string.IsNullOrEmpty(Reference.Thumbnail)
						? Reference.Thumbnail
						: World?.Thumbnail
				)
			);

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
	}
}