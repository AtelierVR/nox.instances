using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Network;
using Nox.CCK.Utils;
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

		public  InstanceComponent          reference;
		public  TextLanguage               text;
		public  Button                     button;
		public  Image                      banner;
		public  Image                      thumbnail;
		public  RectTransform              thumbnailContainer;
		private NetworkImage               _bannerNetworkImage;
		private NetworkImage               _thumbnailNetworkImage;
		private (IUser, IInstancePlayer)   _user;

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

			UpdateBanner(user);
			UpdateThumbnail(user);
		}

		private void OnClick() {
			Logger.LogDebug($"{_user} ({reference.Page.World}) clicked");
			if (_user.Item1 == null)
				Client.UiAPI?.SendGoto(reference.Page.MId, "users", "identifier", _user.Item2.Identifier);
			else Client.UiAPI?.SendGoto(reference.Page.MId, "users", "user", _user.Item1);
		}

		private void UpdateBanner((IUser, IInstancePlayer) user) {
			var url = user.Item1?.Banner;
			if (string.IsNullOrEmpty(url)) {
				banner.sprite = null;
				return;
			}

			_bannerNetworkImage = banner.GetOrAddComponent<NetworkImage>();
			_bannerNetworkImage.Url = url;
		}

		private void UpdateThumbnail((IUser, IInstancePlayer) user) {
			var url = user.Item1?.Thumbnail;
			if (string.IsNullOrEmpty(url)) {
				thumbnail.sprite = null;
				thumbnailContainer.gameObject.SetActive(false);
				return;
			}

			_thumbnailNetworkImage = thumbnail.GetOrAddComponent<NetworkImage>();
			_thumbnailNetworkImage.Url = url;
			thumbnailContainer.gameObject.SetActive(true);
		}
	}
}