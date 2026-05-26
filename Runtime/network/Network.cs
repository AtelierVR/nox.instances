using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Instances;
using Nox.CCK.Network;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace Nox.Instances.Runtime.Networks {
	public class Network {
		private readonly UnityEvent<Instance> _fetchEvent = new();

		private void InvokeFetch(Instance instance) {
			if (instance == null)
				return;
			_fetchEvent.Invoke(instance);
			Main.Instance.CoreAPI.EventAPI.Emit("instance_fetch", instance);
		}
		
		private (string, string) Optimize(Identifier ide) {
			var crt = Main.UserAPI?.Current?.Server;
			if (!string.IsNullOrEmpty(crt))
				return ide.IsLocal(crt)
					? (ide.ToShortString(false), crt)
					: (ide.ToShortString(), crt);
			return (ide.ToShortString(), ide.Server);
		}

		public async UniTask<Instance> Fetch(Identifier ide, CancellationToken cancellationToken = default) {
			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot fetch world {ide} from {address}");
				return null;
			}

			var request = await RequestNode.To(address, $"/instances/{id}");
			if (request == null) {
				Logger.LogError($"Failed to create request for instance {ide}");
				return null;
			}

			await request.Send(cancellationToken);
			var response = await request.Node<Instance>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to fetch instance {ide} from {address}: {response.Error.Message}");
				return null;
			}

			var instance = response.Data;
			InvokeFetch(instance);
			return instance;
		}

		public async UniTask<SearchResponse> Search(SearchRequest data, CancellationToken cancellationToken = default) {
			var address = Main.UserAPI?.Current?.Server ?? data.Server;
			if (data.Server == address)
				data.Server = Identifier.LOCAL_SERVER;
			
			var request = await RequestNode.To(address, $"/instances{data}");
			if (request == null) {
				Logger.LogError($"Failed to create request for instance search");
				return null;
			}

			await request.Send(cancellationToken);
			var response = await request.Node<SearchResponse>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to search instances from {address}: {response.Error.Message}");
				return null;
			}

			var instances = response.Data;
			instances.Request = data;

			foreach (var instance in instances.Items)
				InvokeFetch(instance);

			return instances;
		}
	}
}