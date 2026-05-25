using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using Nox.Instances;
using Nox.UI;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.instance.client {
	public class InstancePage : IPage {
		static internal string GetStaticKey()
			=> "instance";

		public string GetKey()
			=> GetStaticKey();

		internal int MId;
		private object[] _context;
		private GameObject _content;
		private InstanceComponent _component;
		private Identifier _identifier;
		public IInstance Instance;
		public IWorldAsset Asset;
		public IWorld World;
		private bool _isLoading;
		private bool _isRefreshing;
		public ushort Version = ushort.MaxValue;

		private EventSubscription[] _events = Array.Empty<EventSubscription>();


		public void OnRefresh()
			=> Refresh(false).Forget();

		private static bool T<T>(object[] o, int index, out T value) {
			if (o.Length > index && o[index] is T t) {
				value = t;
				return true;
			}

			value = default;
			return false;
		}

		static internal IPage OnGotoAction(IMenu menu, object[] context) {
			if (!T(context, 0, out string type))
				return null;
			switch (type) {
				case "id_server" when T(context, 1, out uint id0) && T(context, 2, out string ser0):
					return OnPageByIdentifier(menu, context, new Identifier("i", id0, null, ser0));
				case "identifier" when T(context, 1, out string id2):
					return OnPageByIdentifier(menu, context, Identifier.Parse(id2));
				case "instance" when T(context, 1, out IInstance i0):
					var w0 = T(context, 2, out IWorld world) ? world : null;
					var a0 = T(context, 3, out IWorldAsset asset) ? asset : null;
					return OnPageByInstance(menu, context, i0, w0, a0);
			}

			return null;
		}

		private static InstancePage OnPageByIdentifier(IMenu menu, object[] context, Identifier identifier) {
			var page = new InstancePage {
				MId         = menu.Id,
				_context    = context,
				_identifier = identifier,
				Instance    = null,
				World       = null,
			};
			page.Refresh(true).Forget();
			return page;
		}

		private static InstancePage OnPageByInstance(IMenu menu, object[] context, IInstance instance, IWorld world, IWorldAsset asset) {
			var page = new InstancePage {
				MId         = menu.Id,
				_context    = context,
				_identifier = instance.Identifier,
				Instance    = instance,
				World       = world,
				Asset       = asset,
				Version     = instance.World.GetVersion()
			};
			if (page.World == null)
				page.FetchWorld(true, true).Forget();
			else if (page.Asset == null)
				page.FetchAsset(true).Forget();
			return page;
		}

		private async UniTask FetchAsset(bool update = false) {
			if (_isLoading)
				return;
			_isLoading = true;
			try { await FetchAssetCore(); } finally { _isLoading = false; }
			if (update)
				_component.UpdateContent(Instance, World, Asset);
		}

		private async UniTask Refresh(bool load) {
			if (_isRefreshing)
				return;
			_isRefreshing = true;
			try {
				await FetchInstanceCore();
				await FetchWorldCore();
				await FetchAssetCore();
			} finally {
				_isRefreshing = false;
			}
			if (!load)
				_component.UpdatePlayerList(Instance).Forget();
			_component.UpdateContent(Instance, World, Asset);
		}

		// Core helpers – contain only the raw async work, no _isLoading guard.
		// Called from Refresh (which holds _isRefreshing) or from the guarded
		// public Fetch* methods below.
		private async UniTask FetchInstanceCore() {
			Instance = await Main.Instance.Network.Fetch(_identifier);
		}

		private async UniTask FetchWorldCore() {
			if (Instance == null)
				return;
			World = await Main.WorldAPI.Fetch(Instance.World);
		}

		private async UniTask FetchAssetCore() {
			if (Instance == null)
				return;
			var req = new AssetSearchRequest {
				Engines   = new[] { EngineExtensions.CurrentEngine.GetEngineName() },
				Platforms = new[] { PlatformExtensions.CurrentPlatform.GetPlatformName() },
				Versions  = new[] { Version },
				Limit     = 1
			};
			Asset = (await Main.WorldAPI.SearchAssets(Instance.World, req)).Items.FirstOrDefault();
		}

		private async UniTask FetchInstance(bool update = false) {
			if (_isLoading)
				return;
			_isLoading = true;
			try { await FetchInstanceCore(); } finally { _isLoading = false; }
			if (update)
				_component.UpdateContent(Instance, World, Asset);
		}

		private async UniTask FetchWorld(bool update = false, bool updateAsset = false) {
			if (_isLoading || Instance == null)
				return;
			_isLoading = true;
			try { await FetchWorldCore(); } finally { _isLoading = false; }
			if (updateAsset)
				await FetchAsset(false);
			if (update)
				_component.UpdateContent(Instance, World, Asset);
		}

		public void RemoveDownload() {
			if (!InCache() && !IsDownloading().Item1) {
				Logger.LogWarning("Cannot remove download, asset is not in cache.");
				return;
			}

			Main.WorldAPI?.RemoveFromCache(Asset.Hash);
			Logger.Log($"Removed asset from cache: {Asset.Hash}");
		}

		public void CancelDownload()
			=> GetDownload()?.Cancel();

		public void DownloadAsset() {
			if (IsDownloading().Item1) {
				Logger.Log("Asset is already downloading, no need to start again.");
				return;
			}

			if (InCache()) {
				Logger.Log("Asset is already in cache, no need to download.");
				return;
			}

			Main.WorldAPI?.DownloadToCache(Asset.Url, Asset.Hash)?.Start().Forget();
		}

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(MId);

		public GameObject GetContent(RectTransform parent) {
			if (_content)
				return _content;
			Logger.LogDebug("Creating content for instance page with identifier", parent);
			(_content, _component) = InstanceComponent.Generate(this, parent);
			Logger.LogDebug("Created content for instance page with identifier", parent);
			_component.UpdateLoading();
			return _content;
		}

		public void OnOpen(IPage lastPage) {
			_events = new[] {
				Main.Instance.CoreAPI.EventAPI.Subscribe("world_cache_added", OnCacheUpdate),
				Main.Instance.CoreAPI.EventAPI.Subscribe("world_cache_download", OnCacheUpdate),
				Main.Instance.CoreAPI.EventAPI.Subscribe("world_cache_removed", OnCacheUpdate),
				Main.Instance.CoreAPI.EventAPI.Subscribe("session_added", OnSessionUpdate),
				Main.Instance.CoreAPI.EventAPI.Subscribe("session_removed", OnSessionUpdate),
				Main.Instance.CoreAPI.EventAPI.Subscribe("session_state_changed", OnSessionUpdate),
			};
			_component.UpdatePlayerList(Instance).Forget();
		}

		private void OnCacheUpdate(EventData context)
			=> _component.UpdateDownloading(IsDownloading());

		private void OnSessionUpdate(EventData context)
			=> _component.UpdateJoinButton(Instance);

		public void OnDisplay(IPage lastPage) {
			if (Instance != null) {
				_component.UpdateContent(Instance, World, Asset);
				_component.UpdateDownloading(IsDownloading());
			} else if (_isLoading)
				_component.UpdateLoading();
			else
				_component.UpdateError("Instance not found or loading failed.");
		}

		public void OnRemove() {
			foreach (var ev in _events)
				Main.Instance.CoreAPI.EventAPI.Unsubscribe(ev);
			CancelDownload();
		}

		public bool InCache()
			=> Asset != null && Main.WorldAPI?.HasInCache(Asset.Hash) == true;

		private ICaching GetDownload()
			=> Asset != null ? Main.WorldAPI?.GetDownload(Asset.Url, Asset.Hash) : null;

		public (bool, float) IsDownloading() {
			var cache = GetDownload();
			if (cache == null)
				return (false, 0f);
			return cache.IsRunning ? (true, cache.Progress) : (false, 1f);
		}





	}
}