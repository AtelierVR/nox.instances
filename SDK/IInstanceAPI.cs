using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace Nox.Instances {
	/// <summary>
	/// API for fetching and searching instances.
	/// </summary>
	public interface IInstanceAPI {
		/// <summary>
		/// Fetches an instance by its identifier.
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		public UniTask<IInstance> Fetch(Identifier identifier);

		/// <summary>
		/// Searches for instances matching the given search request.
		/// Optionally specify a server to search from; if not provided, will attempt to search from the current user's server.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public UniTask<ISearchResponse> Search(ISearchRequest data);
	}
}