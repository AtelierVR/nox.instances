using System;
using System.Linq;
using Nox.CCK.Search;
using Nox.Search;
using UnityEngine;

namespace Nox.Instances.Runtime.Search {
	public class SearchHandler : IHandler {
		public string GetId()
			=> Main.Instance.CoreAPI.ModMetadata.GetId();

		public string GetTitleKey()
			=> "instance.search.title";

		public string[] GetTitleArguments()
			=> Array.Empty<string>();

		public string GetPlaceholderKey()
			=> "instance.search.placeholder";

		public string[] GetPlaceholderArguments()
			=> Array.Empty<string>();

		public Texture2D GetIcon()
			=> Main.Instance.CoreAPI.AssetAPI
				.GetAsset<Texture2D>("ui:icons/location.png");

		public string GetDescriptionKey()
			=> "instance.search.description";

		public string[] GetDescriptionArguments()
			=> Array.Empty<string>();

		public IWorker[] GetWorkers()
			=> SearchHelper.ServersBy("instance")
				.Select(s => new SearchWorker { Title = s.Title, Server = s.Address })
				.ToArray<IWorker>();
	}
}