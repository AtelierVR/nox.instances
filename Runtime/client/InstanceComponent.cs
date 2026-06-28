using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nox.CCK.Language;
using Nox.CCK.Search;
using Nox.CCK.Sessions;
using Nox.CCK.Utils;
using Nox.Instances;
using Nox.Sessions;
using Nox.Users;
using Nox.Worlds;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace Nox.Instances.Runtime.client {
	public class InstanceComponent : MonoBehaviour {
		public GameObject withThumbnail;
		public GameObject withoutThumbnail;
		public Image thumbnail;
		public TextLanguage title;
		public TextLanguage identifier;
		public TextLanguage label;
		public Image labelIcon;
		public RectTransform content;
		public InstancePage Page;
		private CancellationTokenSource _thumbnailTokenSource;
		private CancellationTokenSource _playerListTokenSource;
		public RectTransform playerList;
		public GameObject playerInfobox;
		public GameObject playerListContainer;
		public GameObject descriptionContainer;
		public TextLanguage descriptionText;
		public RectTransform actions;
		public Image joinIcon;
		public TextLanguage joinLabel;
		public Button joinButton;

		// Cache Logic
		private bool _isCachedHover;
		private string _lastTextureCaching = "icons/0.png";
		public Image cacheIcon;
		public Button cacheButton;
		public Slider cacheProgress;
		public TextLanguage cacheLabel;

		public void UpdateError(string error) {
			title.UpdateText("instance.error");
			identifier.UpdateText("instance.error");
			label.UpdateText("instance.error");
			thumbnail.sprite = null;
			thumbnail.sprite = null;
			withThumbnail.SetActive(false);
			withoutThumbnail.SetActive(true);
			descriptionContainer.SetActive(false);
		}

		public void UpdateLoading() {
			title.UpdateText("instance.loading");
			identifier.UpdateText("instance.loading");
			label.UpdateText("instance.loading");
			thumbnail.sprite = null;
			thumbnail.sprite = null;
			withThumbnail.SetActive(false);
			withoutThumbnail.SetActive(true);
			descriptionContainer.SetActive(false);
		}

		public void UpdateContent(IInstance instance, IWorld world, IWorldAsset asset) {
			if (instance == null)
				return;

			title.UpdateText("instance.title", new[] { instance.Title ?? world?.Title ?? instance.Identifier.ToString() });
			label.UpdateText("instance.about.title", new[] { instance.Title ?? world?.Title ?? instance.Identifier.ToString() });
			identifier.UpdateText(
				"instance.identifier", new[] {
					instance.Identifier.ToString(),
					instance.Id.ToString(),
					instance.Server
				}
			);

			var description = instance.Description;
			if (string.IsNullOrEmpty(description) && world != null)
				description = world.Description;

			if (!string.IsNullOrEmpty(description)) {
				descriptionText.SetMarkdown(description);
				descriptionContainer.SetActive(true);
			} else
				descriptionContainer.SetActive(false);


			UpdateThumbnail(instance, world).Forget();
			UpdatePlayerList(instance).Forget();
			UpdateJoinButton(instance);
			HoverCache(_isCachedHover);
		}

		private async UniTask UpdateThumbnail(IInstance instance, IWorld world) {
			if (_thumbnailTokenSource != null) {
				_thumbnailTokenSource?.Cancel();
				_thumbnailTokenSource?.Dispose();
			}

			_thumbnailTokenSource = new CancellationTokenSource();
			var url = instance?.Thumbnail;
			if (string.IsNullOrEmpty(url) && world != null)
				url = world.Thumbnail;

			if (!string.IsNullOrEmpty(url)) {
				var texture = await Main.NetworkAPI
					.FetchTexture(url)
					.AttachExternalCancellation(_thumbnailTokenSource.Token);
				if (texture) {
					thumbnail.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
					withThumbnail.SetActive(true);
					withoutThumbnail.SetActive(false);
				} else {
					thumbnail.sprite = null;
					withThumbnail.SetActive(false);
					withoutThumbnail.SetActive(true);
				}
			} else {
				thumbnail.sprite = null;
				withThumbnail.SetActive(false);
				withoutThumbnail.SetActive(true);
			}

			_thumbnailTokenSource = null;
		}

		#region Cache Logic

		public void UpdateDownloading((bool, float) download) {
			if (download.Item1) {
				cacheProgress.value = download.Item2;
			} else
				cacheProgress.value = 0;

			HoverCache(_isCachedHover);
		}

		private void HoverCache(bool isHover) {
			_isCachedHover = isHover;
			var texture = ((Page.InCache() ? 1 : 0) << 2)
				| ((Page.IsDownloading().Item1 ? 1 : 0) << 1)
				| ((_isCachedHover ? 1 : 0) << 0);
			if (texture > 5)
				texture -= 4;
			if (!Page.IsDownloading().Item1)
				cacheProgress.value = 0;

			// 0 - | 0 | 0 | 0 | neutral (not hovered, not downloaded)
			// 1 - | 0 | 0 | 1 | can be downloaded (hovered, not downloaded)
			// 2 - | 0 | 1 | 0 | downloading (not hovered, downloading)
			// 3 - | 0 | 1 | 1 | cancel download (hovered, downloading)
			// 4 - | 1 | 0 | 0 | downloaded (not hovered, downloaded)
			// 5 - | 1 | 0 | 1 | remove from cache (hovered, downloaded)
			// 6 - | 1 | 1 | 0 | re-downloading (not hovered, re-downloading) (set to 2)
			// 7 - | 1 | 1 | 1 | cancel re-download (hovered, re-downloading) (set to 3)

			if (_lastTextureCaching != $"ui:icons/cache{texture}.png")
				cacheIcon.sprite = Client.GetAsset<Sprite>(_lastTextureCaching = $"ui:icons/cache{texture}.png");

			cacheLabel.UpdateText(
				"instance.cache."
				+ new[] {
					"none",
					"add",
					"downloading",
					"cancel",
					"downloaded",
					"remove"
				}[texture]
			);
		}

		private void OnCacheClickedAsync() {
			if (Page.IsDownloading().Item1) {
				Page.CancelDownload();
				return;
			}

			if (Page.InCache()) {
				Page.RemoveDownload();
				return;
			}

			Page.DownloadAsset();
			HoverCache(_isCachedHover);
		}

		#endregion

		public void UpdateJoinButton(IInstance instance) {
			if (instance == null) {
				joinButton.interactable = false;
				joinLabel.UpdateText("instance.join.error");
				return;
			}

			// Vérifier si on a des données de connexion
			var connectionData = instance.Connection;
			if (connectionData == null) {
				joinButton.interactable = false;
				joinLabel.UpdateText("instance.join.not_joinable");
				return;
			}

			// Vérifier si on est déjà connecté à cette instance
			ISession session = null;
			foreach (var s in Main.SessionAPI?.GetSessions() ?? Array.Empty<ISession>()) {
				if (!s.GetInstance().Equals(instance.Identifier))
					continue;
				session = s;
				break;
			}

			if (session == null) {
				joinButton.interactable = true;
				joinLabel.UpdateText("instance.join");
				return;
			}

			if (session.State.IsReady()) {
				joinButton.interactable = false;
				joinLabel.UpdateText("instance.join.already_connected");
			} else if (!session.State.IsFinished()) {
				joinButton.interactable = false;
				joinLabel.UpdateText("instance.join.connecting");
			} else {
				joinButton.interactable = true;
				joinLabel.UpdateText("instance.join");
			}
		}

		private void OnJoinClicked() {
			if (Page.Instance == null)
				return;

			// Vérifier si on a des données de connexion
			var connectionData = Page.Instance.Connection;
			if (connectionData == null) {
				Logger.LogWarning("Cannot join instance: no connection data available");
				return;
			}
             
             			// Vérifier si on est déjà connecté à cette instance
             			var sessions = Main.SessionAPI?.GetSessions();
             			if (sessions != null) {
             				foreach (var session in sessions) {
             					var sessionInstance = session.GetInstance();
             					if (!sessionInstance.Equals(Page.Instance.Identifier))
             						continue;
             					Logger.LogWarning("Cannot join instance: already connected to this instance");
             					return;
             				}
             			}

			var th = Page.Instance.Thumbnail;
			if (string.IsNullOrEmpty(th) && Page.World != null)
				th = Page.World.Thumbnail;

			// Tout est OK, on peut joindre
			Main.SessionAPI?.TryMake(
				"external:" + connectionData.GetMethod(),
				new Dictionary<string, object> {
					{ "set_current", true },
					{ "instance", Page.Instance.Identifier }, {
						"title",
						Page.Instance.Title
						?? Page.World?.Title
						?? Page.Instance.Identifier.ToString()
					}, {
						"short_name",
						Page.Instance.Name
						?? Page.Instance.Identifier.ToString()
					}, {
						"thumbnail",
						Main.NetworkAPI.FetchTexture(th)
					},
					{ "data", connectionData.GetData<JObject>() }
				}, out var _
			);
		}

		public static (GameObject, InstanceComponent) Generate(InstancePage instancePage, RectTransform parent) {
			var content              = Instantiate(Client.GetAsset<GameObject>("ui:prefabs/split.prefab"), parent);
			var iconAsset            = Client.GetAsset<GameObject>("ui:prefabs/header_icon.prefab");
			var labelAsset           = Client.GetAsset<GameObject>("ui:prefabs/header_label.prefab");
			var withTitleAsset       = Client.GetAsset<GameObject>("ui:prefabs/with_title.prefab");
			var listAsset            = Client.GetAsset<GameObject>("ui:prefabs/list.prefab");
			var scrollAsset          = Client.GetAsset<GameObject>("ui:prefabs/scroll.prefab");
			var boxAsset             = Client.GetAsset<GameObject>("ui:prefabs/box.prefab");
			var actionButtonAsset    = Client.GetAsset<GameObject>("ui:prefabs/action_button.prefab");
			var actionContainerAsset = Client.GetAsset<GameObject>("ui:prefabs/action_container.prefab");

			var component = content.AddComponent<InstanceComponent>();
			component.Page = instancePage;
			content.name   = $"[{instancePage.GetKey()}_{content.GetEntityId().GetHashCode()}]";

			var splitContent   = Reference.GetComponent<RectTransform>("content", content);
			var containerAsset = Client.GetAsset<GameObject>("ui:prefabs/container.prefab");

			// generate profile
			var container = Instantiate(containerAsset, splitContent);
			var profile = Instantiate(
				Client.GetAsset<GameObject>("prefabs/profile.prefab"),
				Reference.GetComponent<RectTransform>("content", container)
			);
			component.identifier       = Reference.GetComponent<TextLanguage>("identifier", profile);
			component.title            = Reference.GetComponent<TextLanguage>("title", profile);
			component.thumbnail        = Reference.GetComponent<Image>("thumbnail", profile);
			component.withThumbnail    = Reference.GetReference("with_thumbnail", profile);
			component.withoutThumbnail = Reference.GetReference("without_thumbnail", profile);

			// generate dashboard
			container = Instantiate(Client.GetAsset<GameObject>("ui:prefabs/container_full.prefab"), splitContent);
			var withTitle = Instantiate(
				withTitleAsset,
				Reference.GetComponent<RectTransform>("content", container)
			);

			var header = Reference.GetReference("header", withTitle);
			var icon   = Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
			var label  = Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));

			component.labelIcon        = Reference.GetComponent<Image>("image", icon);
			component.label            = Reference.GetComponent<TextLanguage>("text", label);
			component.labelIcon.sprite = Client.GetAsset<Sprite>("ui:icons/location.png");

			var contentDash = Reference.GetComponent<RectTransform>("content", withTitle);
			// setup scroll + list
			var scroll = Instantiate(scrollAsset, contentDash);
			var list   = Instantiate(listAsset, Reference.GetComponent<RectTransform>("content", scroll));
			component.content = Reference.GetComponent<RectTransform>("content", list);

			// add box actions
			var boxActions = Instantiate(boxAsset, component.content);
			Reference.GetComponent<TextLanguage>("text", boxActions).UpdateText("instance.about.actions");
			component.actions = Reference.GetComponent<RectTransform>("content", Instantiate(actionContainerAsset, Reference.GetComponent<RectTransform>("content", boxActions)));

			// Bouton Join
			var join             = Instantiate(actionButtonAsset, component.actions);
			var joinEventTrigger = Reference.GetComponent<EventTrigger>("button", join);
			component.joinButton      = Reference.GetComponent<Button>("button", join);
			component.joinIcon        = Reference.GetComponent<Image>("image", join);
			component.joinLabel       = Reference.GetComponent<TextLanguage>("text", join);
			component.joinIcon.sprite = Client.GetAsset<Sprite>("ui:icons/distance.png");
			component.joinLabel.UpdateText("instance.join");
			SetupEvents(
				joinEventTrigger,
				() => component.OnJoinClicked(),
				() => { }, // Pas d'effet hover pour l'instant
				() => { }
			);

			// Bouton Cache
			var cache             = Instantiate(actionButtonAsset, component.actions);
			var cacheEventTrigger = Reference.GetComponent<EventTrigger>("button", cache);
			component.cacheButton   = Reference.GetComponent<Button>("button", cache);
			component.cacheIcon     = Reference.GetComponent<Image>("image", cache);
			component.cacheLabel    = Reference.GetComponent<TextLanguage>("text", cache);
			component.cacheProgress = Reference.GetComponent<Slider>("progress", cache);
			component.cacheLabel.UpdateText("instance.cache.none");
			component.cacheIcon.sprite = Client.GetAsset<Sprite>("ui:icons/cache0.png");
			SetupEvents(
				cacheEventTrigger,
				() => component.OnCacheClickedAsync(),
				() => component.HoverCache(true),
				() => component.HoverCache(false)
			);

			// add box description
			component.descriptionContainer = Instantiate(boxAsset, component.content);
			Reference.GetComponent<TextLanguage>("text", component.descriptionContainer).UpdateText("instance.about.description");
			component.descriptionText = Reference.GetComponent<TextLanguage>(
				"text", Instantiate(
					Client.GetAsset<GameObject>("ui:prefabs/text.prefab"),
					Reference.GetComponent<RectTransform>("content", component.descriptionContainer)
				)
			);


			// generate instances
			container = Instantiate(containerAsset, splitContent);
			withTitle = Instantiate(withTitleAsset, Reference.GetComponent<RectTransform>("content", container));

			header = Reference.GetReference("header", withTitle);
			icon   = Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
			label  = Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));

			Reference.GetComponent<Image>("image", icon).sprite = Client.GetAsset<Sprite>("ui:icons/group.png");
			Reference.GetComponent<TextLanguage>("text", label).UpdateText("instance.players.title");

			var contentIn = Reference.GetComponent<RectTransform>("content", withTitle);
			component.playerInfobox = Instantiate(Client.GetAsset<GameObject>("ui:prefabs/infobox.prefab"), contentIn);
			Reference.GetComponent<TextLanguage>("text", component.playerInfobox).UpdateText("instance.no_players");
			component.playerListContainer = Instantiate(scrollAsset, contentIn);
			list                          = Instantiate(listAsset, Reference.GetComponent<RectTransform>("content", component.playerListContainer));
			component.playerList          = Reference.GetComponent<RectTransform>("content", list);

			return (content, component);
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private static void SetupEvents(EventTrigger eventTrigger, Action click, Action enter, Action exit) {
			if (!eventTrigger)
				return;
			var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
			entry.callback.AddListener(_ => click());
			eventTrigger.triggers.Add(entry);
			entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
			entry.callback.AddListener(_ => enter());
			eventTrigger.triggers.Add(entry);
			entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
			entry.callback.AddListener(_ => exit());
			eventTrigger.triggers.Add(entry);
		}

		public async UniTask UpdatePlayerList(IInstance instance) {
			if (_playerListTokenSource != null) {
				_playerListTokenSource?.Cancel();
				_playerListTokenSource?.Dispose();
				_playerListTokenSource = null;
			}

			if (instance == null) {
				playerInfobox.SetActive(true);
				playerListContainer.SetActive(false);
				return;
			}


			_playerListTokenSource = new CancellationTokenSource();
			var tasks = new List<UniTask<(IUser, IInstancePlayer)[]>>();

			var players = instance.Players;
			var playersByServer = players
				.GroupBy(p => p.Identifier.Server ?? Identifier.LOCAL_SERVER)
				.ToDictionary(g => g.Key, g => g.ToArray());

			var isEmpty = true;
			var isFirst = true;
			var prefab  = PlayerComponent.PlayerPrefab;
			var action = new Action<(IUser, IInstancePlayer)[]>(
				users => {
					Logger.LogDebug($"Found {users.Length} instances for world {instance.Title} ({instance.Identifier})");
					if (isFirst)
						foreach (Transform child in playerList.transform)
							Destroy(child.gameObject);
					isFirst = false;

					if (users.Length > 0) {
						isEmpty = false;
						playerInfobox.SetActive(false);
						playerListContainer.SetActive(true);
						UniTask.WhenAll(users.Select(user =>
							PlayerComponent.Generate(this, playerList.transform, prefab, user)))
							.ContinueWith(_ => UpdateLayout.UpdateImmediate(playerList))
							.Forget();
					}
				}
			);

			foreach (var (server, users) in playersByServer) {
				if (_playerListTokenSource.IsCancellationRequested) {
					_playerListTokenSource = null;
					return;
				}

				if (users.Length == 0)
					continue;

				if (server == Identifier.LOCAL_SERVER) {
					action(users.Select(u => ((IUser)null, u)).ToArray());
				} else
					tasks.Add(SearchPlayers(users, server, _playerListTokenSource.Token, action));
			}

			await UniTask.WhenAll(tasks);
			if (isEmpty) {
				playerInfobox.SetActive(true);
				playerListContainer.SetActive(false);
			} else
				UpdateLayout.UpdateImmediate(playerList);
		}

		private async UniTask<(IUser, IInstancePlayer)[]> SearchPlayers(IInstancePlayer[] users, string server, CancellationToken token, Action<(IUser, IInstancePlayer)[]> callback = null) {
			if (token.IsCancellationRequested)
				return Array.Empty<(IUser, IInstancePlayer)>();

			var request = Main.UserAPI
				.MakeSearchRequest()
				.SetIds(users.Select(p => p.Identifier).ToArray());

			var response = await Main.UserAPI.Search(request, server)
				.AttachExternalCancellation(token);
			if (token.IsCancellationRequested)
				return Array.Empty<(IUser, IInstancePlayer)>();
			var ress = response == null
				? Array.Empty<IUser>()
				: response.Items;
			if (ress.Length == 0)
				return Array.Empty<(IUser, IInstancePlayer)>();

			var res = new List<(IUser, IInstancePlayer)>();
			foreach (var user in ress) {
				var matchingPlayers = users.Where(p => p.Identifier.Equals(user.Identifier));
				res.AddRange(matchingPlayers.Select(player => (user, player)));
			}

			callback?.Invoke(res.ToArray());
			return res.ToArray();
		}

		public IEnumerable<string> GetSearchableServers()
			=> SearchHelper
				.ServersBy("users")
				.Select(s => s.Address);
	}
}