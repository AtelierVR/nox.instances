using Nox.Instances.Runtime.client;
using Cysharp.Threading.Tasks;
using Nox.Search;
using UnityEngine;

namespace Nox.Instances.Runtime.search {
	public class SearchData : IResultData {
		public Instance Reference;

		public int Id
			=> Reference.Identifier.GetHashCode();

		public string[] TitleArguments
			=> new[] { Reference.Title ?? Reference.Id.ToString() };

		public UniTask<Texture2D> Image
			=> Main.NetworkAPI.FetchTexture(Reference.Thumbnail);

		public void OnClick(int menuId)
			=> Client.UiAPI?.SendGoto(menuId, InstancePage.GetStaticKey(), "instance", Reference);
	}
}