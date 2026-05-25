using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nox.CCK.Utils;
using Nox.Instances;

namespace api.nox.instance {
	[Serializable]
	// ReSharper disable InconsistentNaming
	public class Connection : IConnection, INoxObject {
		public string method;
		public string data;

		public string GetMethod()
			=> method;

		public T GetData<T>() where T : class {
			if (string.IsNullOrEmpty(data))
				return null;
			try {
				var json = Encoding.UTF8.GetString(Convert.FromBase64String(data));
				if (typeof(T) == typeof(JObject))
					return JObject.Parse(json) as T;
				return JsonConvert.DeserializeObject<T>(json);
			} catch (Exception e) {
				Logger.LogError(e);
				return null;
			}
		}
	}
}