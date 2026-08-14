using Cysharp.Threading.Tasks;
using Nox.CCK.Instances;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Network;
using Nox.Search;
using Nox.Servers;
using Nox.Sessions;
using Nox.Users;
using Nox.Worlds;

namespace Nox.Instances.Runtime {
	public class Main : IMainModInitializer, IInstanceAPI {
		static internal Main Instance;
		internal IMainModCoreAPI CoreAPI;
		internal Networks.Network Network;
		private LanguagePack _language;
		private Search.Search _search;

		static internal INetworkAPI NetworkAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("network")
				?.GetInstance<INetworkAPI>();

		static internal IUserAPI UserAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("users")
				?.GetInstance<IUserAPI>();

		static internal IWorldAPI WorldAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("worlds")
				?.GetInstance<IWorldAPI>();

		static internal ISearchAPI SearchAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("search")
				?.GetInstance<ISearchAPI>();

		static internal ISessionAPI SessionAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("session")
				?.GetInstance<ISessionAPI>();

		static internal IServerAPI ServerAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("servers")
				?.GetInstance<IServerAPI>();

		public async UniTask<IInstance> Fetch(Identifier identifier)
			=> await Network.Fetch(identifier);

		public async UniTask<ISearchResponse> Search(ISearchRequest data)
			=> await Network.Search(SearchRequest.From(data));

		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI   = api;
			Instance  = this;
			_language = CoreAPI.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_language);
			_search = new Search.Search();
			Network = new Networks.Network();
		}

		public void OnDisposeMain() {
			LanguageManager.RemovePack(_language);
			_search?.Dispose();
			CoreAPI  = null;
			Instance = null;
		}
	}
}