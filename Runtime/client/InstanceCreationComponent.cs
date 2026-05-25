using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.instance.client {
	public class InstanceCreationComponent : MonoBehaviour {
		public InstanceCreationPage Page;

		public static (GameObject, InstanceCreationComponent) Generate(InstanceCreationPage page, RectTransform parent) {
			var content        = Instantiate(Client.GetAsset<GameObject>("ui:prefabs/split.prefab"), parent);
			var withTitleAsset = Client.GetAsset<GameObject>("ui:prefabs/with_title.prefab");

			var component = content.AddComponent<InstanceCreationComponent>();
			component.Page = page;
			content.name   = $"[{page.GetKey()}_{content.GetEntityId().GetHashCode()}]";

			var splitContent   = Reference.GetComponent<RectTransform>("content", content);
			var containerAsset = Client.GetAsset<GameObject>("ui:prefabs/container.prefab");

			// generate dashboard
			var container = Instantiate(Client.GetAsset<GameObject>("ui:prefabs/container_full.prefab"), splitContent);
			var withTitle = Instantiate(
				withTitleAsset,
				Reference.GetComponent<RectTransform>("content", container)
			);
			return (content, component);
		}
	}
}