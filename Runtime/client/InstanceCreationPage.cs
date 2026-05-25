using System;
using Nox.Instances;
using Nox.UI;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Instances.Runtime.client {
	public class InstanceCreationPage : IPage {
		internal static string GetStaticKey()
			=> "instance_create";

		public string GetKey()
			=> GetStaticKey();

		private int                       _mId;
		private object[]                  _context;
		private GameObject                _content;
		private InstanceCreationComponent _component;
		public  IWorld                    World;
		public  IWorldAsset               Asset;

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

		private static InstanceCreationPage OnPageByWorldForCreation(IMenu menu, object[] context, IWorld world, IWorldAsset asset) {
			var page = new InstanceCreationPage {
				_mId     = menu.Id,
				_context = context,
				World    = world,
				Asset    = asset
			};
			return page;
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
			// Handle page opening logic if needed
		}

		public void OnDisplay(IPage lastPage) {
			// Display the content of the page
		}

		public void OnRemove() {
			// Handle cleanup if needed
		}

		public void OnRefresh() {
			// Handle refresh logic if needed
		}
	}
}