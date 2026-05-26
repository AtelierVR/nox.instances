using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Instances;
using Nox.Users;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace Nox.Instances.Runtime.client {
	public class PlayerComponent : MonoBehaviour {
		public static GameObject PlayerPrefab
			=> Client.GetAsset<GameObject>("players:player.prefab");

		public static async UniTask<(GameObject go, PlayerComponent comp)> Generate(InstanceComponent reference, Transform parent, GameObject playerPrefab = null, (IUser, IInstancePlayer) user = default) {
			playerPrefab ??= PlayerPrefab;
			var instance  = (await InstantiateAsync(playerPrefab, parent)).First();
			var component = instance.AddComponent<PlayerComponent>();
			component.reference = reference;
			component.text      = Reference.GetComponent<TextLanguage>("text", instance);
			component.banner    = Reference.GetComponent<Image>("image", instance);
			component.button    = Reference.GetComponent<Button>("button", instance);
			component.button.onClick.AddListener(component.OnClick);
			component.thumbnail          = Reference.GetComponent<Image>("thumbnail", instance);
			component.thumbnailContainer = Reference.GetComponent<RectTransform>("thumbnail_container", instance);
			if (user != default)
				component.UpdateContent(user);
			return (instance, component);
		}

		public  InstanceComponent       reference;
		public  TextLanguage            text;
		public  Button                  button;
		public  Image                   banner;
		public  Image                   thumbnail;
		public  RectTransform           thumbnailContainer;
		private CancellationTokenSource _bannerTokenSource;
		private CancellationTokenSource _thumbnailTokenSource;
		private (IUser, IInstancePlayer)        _user;

		public void UpdateContent((IUser, IInstancePlayer) user) {
			_user = user;
			Logger.Log($"{user.Item2.Display} {user.Item1?.Display}");
			text.UpdateText(
				"world.instance.text", new[] {
					user.Item2.Display
					?? user.Item1?.Display
					?? "Unknown Player"
				}
			);

			UpdateBanner(user).Forget();
			UpdateThumbnail(user).Forget();
		}

		private void OnClick() {
			Logger.LogDebug($"{_user} ({reference.Page.World}) clicked");
			if (_user.Item1 == null)
				Client.UiAPI?.SendGoto(reference.Page.MId, "users", "identifier", _user.Item2.Identifier);
			else Client.UiAPI?.SendGoto(reference.Page.MId, "users", "user", _user.Item1);
		}

		private async UniTask UpdateBanner((IUser, IInstancePlayer) user) {
			if (_bannerTokenSource != null) {
				_bannerTokenSource?.Cancel();
				_bannerTokenSource?.Dispose();
			}

			_bannerTokenSource = new CancellationTokenSource();
			var url = user.Item1?.Banner;
			if (!string.IsNullOrEmpty(url)) {
				var texture = await Main.NetworkAPI.FetchTexture(url, token: _bannerTokenSource.Token);
				banner.sprite = texture
					? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero)
					: null;
			} else banner.sprite = null;

			_bannerTokenSource = null;
		}

		private async UniTask UpdateThumbnail((IUser, IInstancePlayer) user) {
			if (_thumbnailTokenSource != null) {
				_thumbnailTokenSource?.Cancel();
				_thumbnailTokenSource?.Dispose();
			}

			_thumbnailTokenSource = new CancellationTokenSource();
			var url = user.Item1?.Thumbnail;
			if (!string.IsNullOrEmpty(url)) {
				var texture = await Main.NetworkAPI.FetchTexture(url, token: _thumbnailTokenSource.Token);
				thumbnail.sprite = texture
					? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero)
					: null;
			} else thumbnail.sprite = null;

			thumbnailContainer.gameObject.SetActive(thumbnail.sprite);
			_thumbnailTokenSource = null;
		}
	}
}