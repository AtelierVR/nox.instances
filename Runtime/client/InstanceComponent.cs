using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nox.CCK.Convertors;
using Nox.CCK.Language;
using Nox.CCK.Network;
using Nox.CCK.Search;
using Nox.CCK.Sessions;
using Nox.CCK.Users;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.CCK.Network.Assets;
using Nox.Players;
using Nox.Sessions;
using Nox.Users;
using Nox.Network.Assets;
using Nox.Worlds;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace Nox.Instances.Runtime.client {
	public class InstanceComponent : MonoBehaviour {
		public  InstanceComponent reference;
		public  GameObject         withThumbnail;
		public  GameObject         withoutThumbnail;
		public  Image              thumbnail;
		public  TextLanguage       title;
		public  TextLanguage       identifier;
		public  TextLanguage       label;
		public  Image              labelIcon;
		public  RectTransform      content;
		public  InstancePage       Page;
		private NetworkImage       _thumbnailNetworkImage;
		public RectTransform playerList;
		public GameObject playerInfobox;
		public GameObject playerListContainer;
		public GameObject descriptionContainer;
		public TextLanguage descriptionText;
		public RectTransform actions;
		public Image          joinIcon;
		public TextLanguage   joinLabel;
		public Button         joinButton;
		public Slider         joinProgress;

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

		public void UpdateContent(IInstance instance, IWorld world, IAssetFile asset) {
			if (instance == null)
				return;

			// An instance may be untitled / have no description: fall back on its world.
			var titleValue = GetTitle(instance, world);
			title.UpdateText("instance.title", new[] { titleValue });
			label.UpdateText("instance.about.title", new[] { titleValue });
			identifier.UpdateText(
				"instance.identifier", new[] {
					instance.Identifier.ToString(),
					instance.Id.ToString(),
					instance.Server
				}
			);

			var description = GetDescription(instance, world);

			if (!string.IsNullOrEmpty(description)) {
				descriptionText.SetMarkdown(description);
				descriptionContainer.SetActive(true);
			} else
				descriptionContainer.SetActive(false);


		UpdateThumbnail(instance, world);
			UpdatePlayerList(instance).Forget();
			UpdateJoinButton(instance);
			HoverCache(_isCachedHover);
		}

		/// <summary>
		/// The instance title, falling back on the linked world one when the instance is
		/// untitled (null or empty).
		/// </summary>
		private static string GetTitle(IInstance instance, IWorld world) {
			if (!string.IsNullOrEmpty(instance?.Title))
				return instance.Title;
			var title = world?.Title?.Resolve();
			return !string.IsNullOrEmpty(title) ? title : instance?.Identifier.ToString();
		}

		/// <summary>
		/// The instance description, falling back on the linked world one when the instance
		/// has none (null or empty).
		/// </summary>
		private static string GetDescription(IInstance instance, IWorld world) {
			if (!string.IsNullOrEmpty(instance?.Description))
				return instance.Description;
			return world?.Description?.Resolve();
		}

		/// <summary>
		/// The instance thumbnail url, falling back on the linked world cover when the
		/// instance has none (null or empty).
		/// </summary>
		private static string GetThumbnail(IInstance instance, IWorld world) {
			if (!string.IsNullOrEmpty(instance?.Thumbnail))
				return instance.Thumbnail;
			return world?.BestImage(1f)?.Url;   // square card slot
		}

		private void UpdateThumbnail(IInstance instance, IWorld world) {
			var url = GetThumbnail(instance, world);

			if (string.IsNullOrEmpty(url)) {
				thumbnail.sprite = null;
				withThumbnail.SetActive(false);
				withoutThumbnail.SetActive(true);
				return;
			}

			_thumbnailNetworkImage = thumbnail.GetOrAddComponent<NetworkImage>();
			_thumbnailNetworkImage.Url = url;
			withThumbnail.SetActive(true);
			withoutThumbnail.SetActive(false);
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
				SetJoinProgress(0f);
				return;
			}

			// Already connected: allow rejoining.
			var connected = FindConnectedSession(instance);
			if (connected != null) {
				joinButton.interactable = true;
				joinLabel.UpdateText("instance.join.rejoin");
				SetJoinIcon("ui:icons/refresh.png");
				SetJoinProgress(0f);
				return;
			}

			// Check that we have connection data
			var connectionData = instance.Connection;
			if (connectionData == null) {
				joinButton.interactable = false;
				joinLabel.UpdateText("instance.join.not_joinable");
				SetJoinProgress(0f);
				return;
			}

			// A session exists but is not connected yet.
			// The button allows cancelling it only when the current state allows it,
			// otherwise we just show a connecting text.
			var pending = FindPendingSession(instance);
			if (pending != null) {
				SetJoinProgress(pending.State.Progress);
				if (pending.State.Cancelable) {
					joinButton.interactable = true;
					joinLabel.UpdateText("instance.join.cancel");
					SetJoinIcon("ui:icons/cancel.png");
				} else {
					joinButton.interactable = false;
					joinLabel.UpdateText("instance.join.connecting");
					SetJoinIcon("ui:icons/distance.png");
				}
				return;
			}

			joinButton.interactable = true;
			joinLabel.UpdateText("instance.join");
			SetJoinIcon("ui:icons/distance.png");
			SetJoinProgress(0f);
		}

		/// <summary>
		/// Find the session matching the given instance that is still in progress
		/// (not finished yet). Stale/disposed sessions are ignored.
		/// </summary>
		private static ISession FindPendingSession(IInstance instance) {
			if (instance == null)
				return null;
			foreach (var s in Main.SessionAPI?.GetSessions() ?? Array.Empty<ISession>()) {
				if (!s.GetInstance().Equals(instance.Identifier))
					continue;
				if (_cancelledSessions.Contains(s))
					continue;
				if (s.State.IsFinished())
					continue;
				return s;
			}

			return null;
		}

		/// <summary>
		/// Find the session matching the given instance that is currently connected.
		/// A session whose state is ready but whose network connection is down
		/// (e.g. after being disposed) does not count as connected.
		/// </summary>
		private static ISession FindConnectedSession(IInstance instance) {
			if (instance == null)
				return null;
			foreach (var s in Main.SessionAPI?.GetSessions() ?? Array.Empty<ISession>()) {
				if (!s.GetInstance().Equals(instance.Identifier))
					continue;
				if (_cancelledSessions.Contains(s))
					continue;
				if (!s.State.IsReady())
					continue;
				if (s is INetSession net && !net.IsConnected)
					continue;
				return s;
			}

			return null;
		}

		private static readonly HashSet<ISession> _cancelledSessions = new();

		private string _lastJoinIcon = "ui:icons/distance.png";

		private void SetJoinIcon(string icon) {
			if (_lastJoinIcon == icon)
				return;
			joinIcon.sprite = Client.GetAsset<Sprite>(_lastJoinIcon = icon);
		}

		private float _lastJoinProgress = -1f;

		/// <summary>
		/// Updates the join button progress bar from the session state progress.
		/// <see cref="IState.Progress"/> is -1 when not applicable, which is shown as 0.
		/// </summary>
		private void SetJoinProgress(float progress) {
			if (joinProgress == null)
				return;

			var value = Mathf.Clamp01(progress < 0f ? 0f : progress);
			if (Mathf.Approximately(_lastJoinProgress, value))
				return;

			_lastJoinProgress = value;
			joinProgress.value = value;
		}

		private string _lastPlayerListSessionId;

		/// <summary>
		/// Called when a session event occurs (<c>session_added</c>, <c>session_removed</c>,
		/// <c>session_state_changed</c>). Always refreshes the join button (including the
		/// connection progress), and refreshes the player list only when the connected
		/// session actually changed.
		/// </summary>
		public void OnSessionChanged() {
			var instance = Page?.Instance;
			UpdateJoinButton(instance);

			var session = FindConnectedSession(instance);
			var id      = session?.Id;
			if (_lastPlayerListSessionId == id)
				return;

			_lastPlayerListSessionId = id;
			UpdatePlayerList(instance).Forget();
		}

		private void OnJoinClicked() {
			var instance = Page?.Instance;
			if (instance == null)
				return;

			// Already connected -> rejoin.
			var connected = FindConnectedSession(instance);
			if (connected != null) {
				RejoinSessionAsync(instance, connected);
				return;
			}

			// Connection in progress -> cancel (when the current state allows it).
			var pending = FindPendingSession(instance);
			if (pending != null) {
				if (pending.State.Cancelable)
					CancelSessionAsync(instance, pending).Forget();
				return;
			}

			// Otherwise -> connect.
			Join(instance);
		}

		/// <summary>
		/// Create and connect a new session for the given instance.
		/// </summary>
		private void Join(IInstance instance) {
			if (instance == null)
				return;

			// Check that we have connection data
			var connectionData = instance.Connection;
			if (connectionData == null) {
				Logger.LogWarning("Cannot join instance: no connection data available");
				return;
			}

			var th = GetThumbnail(instance, Page?.World);

			// Everything is fine, we can join
			Main.SessionAPI?.TryMake(
				"external:" + connectionData.GetMethod(),
				new Dictionary<string, object> {
					{ "set_current", true },
					{ "instance", instance.Identifier }, {
						"title",
						GetTitle(instance, Page?.World)
					}, {
						"short_name",
						instance.Name
						?? instance.Identifier.ToString()
					}, {
						"thumbnail",
						Main.NetworkAPI.FetchTexture(th)
					},
					{ "data", connectionData.GetData<JObject>() }
				}, out var _
			);
		}

		/// <summary>
		/// Connect again to the instance. The previous session is not disposed here:
		/// the new one becomes current (<c>set_current</c>) and
		/// <see cref="ISessionAPI.SetCurrent"/> deselects, disposes and unregisters it.
		/// This keeps the rejoin immediate (and therefore cancellable).
		/// </summary>
		private void RejoinSessionAsync(IInstance instance, ISession session) {
			// Just mark the previous session as abandoned so the UI immediately
			// offers the state of the new connection.
			if (session != null)
				_cancelledSessions.Add(session);

			Join(instance);
			UpdateJoinButton(instance);
		}

		/// <summary>
		/// Closes the given session through the session API: a current session is handed
		/// over to <see cref="ISessionAPI.SetCurrent"/>, which disposes and unregisters it;
		/// any other (e.g. in-progress) session is disposed directly.
		/// </summary>
		private static async UniTask CloseSessionAsync(ISession session) {
			if (session == null)
				return;

			_cancelledSessions.Add(session);

			var api = Main.SessionAPI;
			if (api != null)
				await api.Close(session.Id);
		}

		/// <summary>
		/// Cancel/disconnect an in-progress session of the instance.
		/// </summary>
		private async UniTask CancelSessionAsync(IInstance instance, ISession session) {
			await CloseSessionAsync(session);
			UpdateJoinButton(instance);
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
			content.name   = $"[{instancePage.GetKey()}_{content.GetId()}]";

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

			// Join button (Connect / Cancel / Rejoin depending on the state)
			var join             = Instantiate(actionButtonAsset, component.actions);
			var joinEventTrigger = Reference.GetComponent<EventTrigger>("button", join);
			component.joinButton      = Reference.GetComponent<Button>("button", join);
			component.joinIcon        = Reference.GetComponent<Image>("image", join);
			component.joinLabel       = Reference.GetComponent<TextLanguage>("text", join);
			component.joinIcon.sprite = Client.GetAsset<Sprite>("ui:icons/distance.png");
			component.joinLabel.UpdateText("instance.join");
			component.joinProgress = Reference.GetComponent<Slider>("progress", join);
			SetupEvents(
				joinEventTrigger,
				() => component.OnJoinClicked(),
				() => { }, // No hover effect for now
				() => { }
			);

			// Cache button
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

		private CancellationTokenSource _playerListTokenSource;

		public async UniTask UpdatePlayerList(IInstance instance) {
			if (_playerListTokenSource != null) {
				_playerListTokenSource?.Cancel();
				_playerListTokenSource?.Dispose();
				_playerListTokenSource = null;
			}

			if (instance == null) {
				UnsubscribeSessionPlayers();
				playerInfobox.SetActive(true);
				playerListContainer.SetActive(false);
				return;
			}

			_playerListTokenSource = new CancellationTokenSource();
			var token = _playerListTokenSource.Token;

			// When connected to the instance, the player list mirrors the session's
			// and updates automatically (join/leave).
			var session = FindConnectedSession(instance);
			if (session != null) {
				SubscribeSessionPlayers(session);
				await RenderPlayers(instance, GetSessionPlayers(session), token);
				return;
			}

			UnsubscribeSessionPlayers();
			await RenderPlayers(instance, instance.Players, token);
		}

		/// <summary>
		/// Players currently present in the session, exposed as instance players.
		/// </summary>
		private static IInstancePlayer[] GetSessionPlayers(ISession session)
			=> (session.Entities?.GetEntities<IPlayer>() ?? Array.Empty<IPlayer>())
				.Select(p => (IInstancePlayer)new SessionInstancePlayer(p))
				.ToArray();

		private async UniTask RenderPlayers(IInstance instance, IInstancePlayer[] players, CancellationToken token) {
			var tasks = new List<UniTask<(IUser, IInstancePlayer)[]>>();

			var playersByServer = players
				.GroupBy(p => p.Identifier.Server ?? Identifier.LOCAL_SERVER)
				.ToDictionary(g => g.Key, g => g.ToArray());

			var isEmpty = true;
			var isFirst = true;
			var prefab  = PlayerComponent.PlayerPrefab;
			var action = new Action<(IUser, IInstancePlayer)[]>(
				users => {
					Logger.LogDebug($"Found {users.Length} players for world {instance.Title} ({instance.Identifier})");
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
				if (token.IsCancellationRequested)
					return;

				if (users.Length == 0)
					continue;

				if (server == Identifier.LOCAL_SERVER) {
					action(users.Select(u => ((IUser)null, u)).ToArray());
				} else
					tasks.Add(SearchPlayers(users, server, token, action));
			}

			await UniTask.WhenAll(tasks);
			if (token.IsCancellationRequested)
				return;

			if (isEmpty) {
				playerInfobox.SetActive(true);
				playerListContainer.SetActive(false);
			} else
				UpdateLayout.UpdateImmediate(playerList);
		}

		#region Session Players

		private ISession _playerListSession;

		private void SubscribeSessionPlayers(ISession session) {
			if (_playerListSession == session)
				return;
			UnsubscribeSessionPlayers();
			_playerListSession = session;
			session.Entities.OnEntityAdded.AddListener(OnSessionEntityChanged);
			session.Entities.OnEntityRemoved.AddListener(OnSessionEntityChanged);
		}

		private void UnsubscribeSessionPlayers() {
			if (_playerListSession == null)
				return;
			_playerListSession.Entities.OnEntityAdded.RemoveListener(OnSessionEntityChanged);
			_playerListSession.Entities.OnEntityRemoved.RemoveListener(OnSessionEntityChanged);
			_playerListSession = null;
		}

		private void OnSessionEntityChanged(IEntity entity) {
			if (entity is not IPlayer)
				return;
			UpdatePlayerList(Page?.Instance).Forget();
		}

		private void OnDestroy()
			=> UnsubscribeSessionPlayers();

		private sealed class SessionInstancePlayer : IInstancePlayer {
			private readonly IPlayer _player;

			public SessionInstancePlayer(IPlayer player)
				=> _player = player;

			public Identifier Identifier
				=> _player.Identifier;

			public string Display
				=> _player.Display;
		}

		#endregion

		private async UniTask<(IUser, IInstancePlayer)[]> SearchPlayers(IInstancePlayer[] users, string server, CancellationToken token, Action<(IUser, IInstancePlayer)[]> callback = null) {
			if (token.IsCancellationRequested)
				return Array.Empty<(IUser, IInstancePlayer)>();

			var request = new SearchRequest {
				Ids = users.Select(p => p.Identifier).ToArray()
			};

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