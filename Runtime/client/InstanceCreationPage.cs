using System;
using Nox.CCK.Utils;
using Nox.Instances;
using Nox.UI;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Instances.Runtime.client {
	public class InstanceCreationPage : IPage {
		public const string ConfigModePath = "instances.create.mode";
		public const string ModeSimple     = "simple";
		public const string ModeAdvanced   = "advanced";

		internal static string GetStaticKey()
			=> "instance_create";

		public string GetKey()
			=> GetStaticKey();

		private int                       _mId;
		private object[]                  _context;
		private GameObject                _content;
		private InstanceCreationComponent _component;

		public  IWorld      World;
		public  IWorldAsset Asset;
		public  string      Server;
		public  ushort      Version = ushort.MaxValue;
		public  string      Mode;
		public  ushort      Capacity;
		public  string[]    Tags;

		// Pre-filled form values.
		public string Title;
		public string Description;
		public string ShortName;

		private static bool T<T>(object[] o, int index, out T value) {
			if (o.Length > index && o[index] is T t) {
				value = t;
				return true;
			}

			value = default;
			return false;
		}

		internal static IPage OnGotoAction(IMenu menu, object[] context) {
			if (!T(context, 0, out IWorld world)) return null;
			var asset = T(context, 2, out IWorldAsset worldAsset) ? worldAsset : null;
			return OnPageByWorldForCreation(menu, context, world, asset);
		}

		/// <summary>
		/// Context contract (arguments after menu id + page key):
		/// 0 IWorld world
		/// 1 ushort version (world version, ushort.MaxValue = auto)
		/// 2 IWorldAsset asset
		/// 3 string mode ("simple" | "advanced")
		/// 4 string server
		/// 5 string title
		/// 6 string description
		/// 7 ushort capacity
		/// 8 string[] tags
		/// 9 string shortName
		/// </summary>
		private static InstanceCreationPage OnPageByWorldForCreation(IMenu menu, object[] context, IWorld world, IWorldAsset asset) {
			var version  = T(context, 1, out ushort v) ? v : ushort.MaxValue;
			var mode     = T(context, 3, out string m) ? m : null;
			var server   = T(context, 4, out string s) ? s : null;
			var title    = T(context, 5, out string t) ? t : null;
			var desc     = T(context, 6, out string d) ? d : null;
			var capacity = T(context, 7, out ushort c) ? c : (ushort)0;
			var tags     = T(context, 8, out string[] tagsArr) ? tagsArr : null;
			var name     = T(context, 9, out string n) ? n : null;

			var page = new InstanceCreationPage {
				_mId        = menu.Id,
				_context    = context,
				World       = world,
				Asset       = asset,
				Server      = server ?? world?.Server,
				Version     = version,
				Mode        = mode,
				Capacity    = capacity,
				Tags        = tags,
				Title       = title,
				Description = desc,
				ShortName   = name
			};

			return page;
		}

		public string GetMode() {
			if (string.IsNullOrEmpty(Mode))
				Mode = Config.Load().Get(ConfigModePath, ModeSimple);
			return Mode == ModeAdvanced ? ModeAdvanced : ModeSimple;
		}

		public void SetMode(string mode) {
			Mode = mode == ModeAdvanced ? ModeAdvanced : ModeSimple;

			var config = Config.Load();
			config.Set(ConfigModePath, Mode);
			config.Save();

			_component?.Refresh();
		}

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(_mId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			Logger.LogDebug($"Creating content for instance creation page", parent);
			(_content, _component) = InstanceCreationComponent.Generate(this, parent);
			Logger.LogDebug($"Created content for instance creation page", parent);
			return _content;
		}

		public void OnOpen(IPage lastPage) {
			// Nothing to subscribe yet.
		}

		public void OnDisplay(IPage lastPage) {
			_component?.Refresh();
		}

		public void OnRemove() {
			// Nothing to clean yet.
		}

		public void OnRefresh()
			=> _component?.Refresh();

		public void GoToInstance(IInstance instance) {
			if (instance == null) return;
			Client.UiAPI?.SendGoto(_mId, InstancePage.GetStaticKey(), "instance", instance, World, Asset);
		}
	}
}