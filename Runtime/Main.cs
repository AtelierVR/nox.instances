using System.Linq;
using api.nox.instance.network;
using api.nox.instance.search;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Nox.CCK.Instances;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Instances;
using Nox.Network;
using Nox.Search;
using Nox.Sessions;
using Nox.Users;
using Nox.Worlds;
using ISearchRequest = Nox.Instances.ISearchRequest;
using ISearchResponse = Nox.Instances.ISearchResponse;

namespace api.nox.instance {
	public class Main : IMainModInitializer, IInstanceAPI {
		static internal Main           Instance;
		internal        IMainModCoreAPI CoreAPI;
		internal        Network        Network;
		private         LanguagePack   _language;
		private         Search         _search;

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

		public async UniTask<IInstance> Fetch(Identifier identifier)
			=> await Network.Fetch(identifier);

		public async UniTask<ISearchResponse> Search(ISearchRequest data)
			=> await Network.Search(SearchRequest.From(data));

		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI   = api;
			Instance  = this;
			_language = CoreAPI.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_language);
			_search = new Search();
			Network = new Network();
		}

		public void OnDisposeMain() {
			LanguageManager.RemovePack(_language);
			_search?.Dispose();
			CoreAPI  = null;
			Instance = null;
		}
	}
}