using System.Linq;
using Nox.Instances.Runtime.client;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.UI;

namespace Nox.Instances.Runtime {
	public class Client : IClientModInitializer {
		internal static IUiAPI UiAPI
			=> Main.Instance.CoreAPI.ModAPI
				.GetMod("ui")
				.GetInstance<IUiAPI>();

		public static T GetAsset<T>(ResourceIdentifier path) where T : UnityEngine.Object
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<T>(path);

		private EventSubscription _event;

		public void OnInitializeClient(IClientModCoreAPI api) {
			_event = Main.Instance.CoreAPI.EventAPI.Subscribe("menu_goto", OnGoto);
		}

	private void OnGoto(EventData context) {
		if (!context.TryGet(0, out int mid)) return;
		if (!context.TryGet(1, out string key)) return;
		var menu = UiAPI?.Get<IMenu>(mid);
		if (menu == null) return;
		IPage page = null;
		if (InstancePage.GetStaticKey() == key)
			page = InstancePage.OnGotoAction(menu, context.Data[2..]);
		else if (InstanceCreationPage.GetStaticKey() == key)
			page = InstanceCreationPage.OnGotoAction(menu, context.Data[2..]);
		if (page == null) return;
		Main.Instance.CoreAPI.EventAPI.Emit("menu_display", menu.Id, page);
	}

		public void OnDisposeClient() {
			Main.Instance.CoreAPI.EventAPI.Unsubscribe(_event);
			_event = null;
		}
	}
}