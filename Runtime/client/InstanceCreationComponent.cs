using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Instances.Runtime.Networks;
using Nox.Servers;
using Nox.UI;
using Nox.Worlds;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Transform = UnityEngine.Transform;

namespace Nox.Instances.Runtime.client {
	public class InstanceCreationComponent : MonoBehaviour {
		public InstanceCreationPage Page;

		private const int MinCapacity = 128;

		private bool _capacityTouched;

		// Header (left panel)
		public Image        labelIcon;
		public TextLanguage label;
		public Button       headerModeButton;
		public TextLanguage headerModeButtonText;

		// Left content (form fields)
		public RectTransform content;

		// Right panel
		public TextLanguage rightHeaderLabel;
		public RectTransform infoContent;
		public Button       createButton;
		public TextLanguage createButtonText;
		public GameObject   errorBox;
		public TextLanguage errorText;

		// Form fields (rebuilt on refresh)
		public TMP_InputField serverInputField;
		public TMP_InputField worldInputField;
		public TMP_InputField titleField;
		public TMP_InputField descriptionField;
		public Slider       capacitySlider;
		public TextLanguage capacityValue;
		public TextLanguage capacityType;
		public TMP_InputField versionField;
		public TMP_InputField tagsField;
		public TMP_InputField shortNameField;

		private GameObject   _boxAsset;
		private GameObject   _listAsset;
		private GameObject   _inputFieldAsset;
		private GameObject   _textAreaAsset;
		private GameObject   _btnAsset;
		private GameObject   _textAsset;

		#region Generate

		public static (GameObject, InstanceCreationComponent) Generate(InstanceCreationPage page, RectTransform parent) {
			var iconAsset      = Client.GetAsset<GameObject>("ui:prefabs/header_icon.prefab");
			var labelAsset     = Client.GetAsset<GameObject>("ui:prefabs/header_label.prefab");
			var dropdownAsset  = Client.GetAsset<GameObject>("ui:prefabs/header_dropdown.prefab");
			var withTitleAsset = Client.GetAsset<GameObject>("ui:prefabs/with_title.prefab");
			var listAsset      = Client.GetAsset<GameObject>("ui:prefabs/list.prefab");
			var scrollAsset    = Client.GetAsset<GameObject>("ui:prefabs/scroll.prefab");
			var containerAsset = Client.GetAsset<GameObject>("ui:prefabs/container_full.prefab");

			var content = Instantiate(Client.GetAsset<GameObject>("ui:prefabs/split.prefab"), parent);
			var component = content.AddComponent<InstanceCreationComponent>();
			component.Page = page;
			content.name   = $"[{page.GetKey()}_{content.GetEntityId().GetHashCode()}]";

			component._boxAsset        = Client.GetAsset<GameObject>("ui:prefabs/box.prefab");
			component._listAsset       = listAsset;
			component._inputFieldAsset = Client.GetAsset<GameObject>("ui:prefabs/input_field.prefab");
			component._textAreaAsset   = Client.GetAsset<GameObject>("ui:prefabs/text_area.prefab");
			component._btnAsset        = Client.GetAsset<GameObject>("ui:prefabs/btn_icon.prefab");
			component._textAsset       = Client.GetAsset<GameObject>("ui:prefabs/text.prefab");

			var splitContent = Reference.GetComponent<RectTransform>("content", content);

			// Left panel: form
			var container = Instantiate(containerAsset, splitContent);
			var withTitle = Instantiate(withTitleAsset, Reference.GetComponent<RectTransform>("content", container));

			var header = Reference.GetReference("header", withTitle);
			var icon   = Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
			var label  = Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));

			component.labelIcon        = Reference.GetComponent<Image>("image", icon);
			component.label            = Reference.GetComponent<TextLanguage>("text", label);

			var dropdown = Instantiate(dropdownAsset, Reference.GetComponent<RectTransform>("after", header));
			component.headerModeButton     = Reference.GetComponent<Button>("button", dropdown);
			component.headerModeButtonText = Reference.GetComponent<TextLanguage>("text", dropdown);
			component.headerModeButton.onClick.AddListener(component.OpenModeModal);

			var contentDash = Reference.GetComponent<RectTransform>("content", withTitle);
			var scroll      = Instantiate(scrollAsset, contentDash);
			var list        = Instantiate(listAsset, Reference.GetComponent<RectTransform>("content", scroll));
			component.content = Reference.GetComponent<RectTransform>("content", list);

			// Right panel: info + create button
			component.BuildRightPanel(splitContent, iconAsset, labelAsset, listAsset, scrollAsset);

			component.Refresh();
			return (content, component);
		}

		#endregion

		private void BuildRightPanel(RectTransform splitContent, GameObject iconAsset, GameObject labelAsset, GameObject listAsset, GameObject scrollAsset) {
			var container = Instantiate(Client.GetAsset<GameObject>("ui:prefabs/container.prefab"), splitContent);
			var rightContent = Reference.GetComponent<RectTransform>("content", container);

			// Header + content (standard with_title layout, fills the container like the left panel)
			var withTitle = Instantiate(Client.GetAsset<GameObject>("ui:prefabs/with_title.prefab"), rightContent);

			var header = Reference.GetReference("header", withTitle);
			var icon = Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
			Reference.GetComponent<Image>("image", icon).sprite = Client.GetAsset<Sprite>("ui:icons/info.png");
			var label = Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));
			rightHeaderLabel = Reference.GetComponent<TextLanguage>("text", label);
			rightHeaderLabel.UpdateText("instance.create.info");

			var panelContent = Reference.GetComponent<RectTransform>("content", withTitle);

			var layout = panelContent.gameObject.AddComponent<VerticalLayoutGroup>();
			layout.padding = new RectOffset(16, 16, 16, 16);
			layout.spacing = 8;
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			layout.childForceExpandWidth = true;
			layout.childForceExpandHeight = false;

			// Info zone (empty for now, flexible)
			var infoScroll = Instantiate(scrollAsset, panelContent);
			var infoLayout = infoScroll.AddComponent<LayoutElement>();
			infoLayout.flexibleHeight = 1f;
			infoLayout.minHeight = 60f;
			var infoList = Instantiate(listAsset, Reference.GetComponent<RectTransform>("content", infoScroll));
			infoContent = Reference.GetComponent<RectTransform>("content", infoList);

			// Error text (hidden)
			errorBox = Instantiate(_textAsset, panelContent);
			errorText = Reference.GetComponent<TextLanguage>("text", errorBox);
			errorBox.SetActive(false);

			// Create button (full width at bottom)
			var createGo = Instantiate(_btnAsset, panelContent);
			Reference.GetReference("image_container", createGo)?.SetActive(false);
			createButton = Reference.GetComponent<Button>("button", createGo);
			createButtonText = Reference.GetComponent<TextLanguage>("text", createGo);
			createButtonText.UpdateText("instance.create.submit");
			createButton.onClick.AddListener(OnCreateClicked);
			var createLayout = createGo.AddComponent<LayoutElement>();
			createLayout.preferredHeight = 48f;
			createLayout.minHeight = 48f;
		}

		#region Refresh / Builders

		public void Refresh() {
			if (!content) return;

			foreach (Transform child in content)
				Destroy(child.gameObject);

			serverInputField = null;
			worldInputField = null;
			titleField = null;
			descriptionField = null;
			capacitySlider = null;
			capacityValue = null;
			capacityType = null;
			versionField = null;
			tagsField = null;
			shortNameField = null;

			_capacityTouched = false;

			label.UpdateText("instance.create.title");
			labelIcon.sprite = Client.GetAsset<Sprite>("ui:icons/edit_location.png");
			UpdateHeaderModeText();

			if (Page.GetMode() == InstanceCreationPage.ModeAdvanced) {
				BuildServerBox();
				BuildWorldBox();
			}

			BuildTitleBox();
			BuildDescriptionBox();

			if (Page.GetMode() == InstanceCreationPage.ModeAdvanced) {
				BuildCapacityBox();
				BuildVersionBox();
				BuildTagsBox();
				BuildShortNameBox();
			}

			UpdateLayout.UpdateImmediate(content);
		}

		private GameObject MakeBox(string key, out RectTransform boxContent) {
			var box = Instantiate(_boxAsset, content);
			Reference.GetComponent<TextLanguage>("text", box).UpdateText(key);
			var list = Instantiate(_listAsset, Reference.GetComponent<RectTransform>("content", box));
			boxContent = Reference.GetComponent<RectTransform>("content", list);
			return box;
		}

		private void BuildServerBox() {
			MakeBox("instance.create.server", out var boxContent);

			serverInputField = MakeInput(boxContent, "instance.create.server.placeholder", TMP_InputField.ContentType.Standard);
			serverInputField.text = Page.Server ?? Main.UserAPI?.Current?.Server ?? string.Empty;
			serverInputField.onEndEdit.AddListener(_ => OnValidateServer());
		}

		private void BuildWorldBox() {
			MakeBox("instance.create.world", out var boxContent);

			worldInputField = MakeInput(boxContent, "instance.create.world.placeholder", TMP_InputField.ContentType.Standard);
			worldInputField.text = Page.World?.Identifier.ToShortString() ?? string.Empty;
			worldInputField.onEndEdit.AddListener(_ => OnValidateWorld());
		}

		private void BuildTitleBox() {
			MakeBox("instance.create.title.label", out var boxContent);
			titleField = MakeInput(boxContent, "instance.create.title.placeholder", TMP_InputField.ContentType.Standard);
			titleField.text = Page.Title ?? (Page.World?.Title ?? string.Empty);
		}

		private void BuildDescriptionBox() {
			MakeBox("instance.create.description.label", out var boxContent);
			descriptionField = MakeTextArea(boxContent, "instance.create.description.placeholder");
			descriptionField.text = Page.Description ?? (Page.World?.Description ?? string.Empty);
		}

		private void BuildCapacityBox() {
			MakeBox("instance.create.capacity", out var boxContent);

			var range = Instantiate(Client.GetAsset<GameObject>("ui:prefabs/range.prefab"), boxContent);
			capacitySlider = Reference.GetComponent<Slider>("range", range);
			capacityValue  = Reference.GetComponent<TextLanguage>("value", range);
			capacityType   = Reference.GetComponent<TextLanguage>("type", range);

			capacitySlider.minValue     = 0f;
			capacitySlider.maxValue     = GetMaxCapacity();
			capacitySlider.wholeNumbers = true;
			capacitySlider.SetValueWithoutNotify(InitialCapacity());
			capacitySlider.onValueChanged.AddListener(OnCapacityChanged);
			UpdateCapacityValue(capacitySlider.value);
		}

		private void BuildVersionBox() {
			MakeBox("instance.create.version", out var boxContent);
			versionField = MakeInput(boxContent, "instance.create.version.placeholder", TMP_InputField.ContentType.IntegerNumber);
			versionField.text = Page.Version == ushort.MaxValue ? string.Empty : Page.Version.ToString();
		}

		private void BuildTagsBox() {
			MakeBox("instance.create.tags", out var boxContent);
			tagsField = MakeInput(boxContent, "instance.create.tags.placeholder", TMP_InputField.ContentType.Standard);
			tagsField.text = Page.Tags != null ? string.Join(",", Page.Tags) : string.Empty;
		}

		private void BuildShortNameBox() {
			MakeBox("instance.create.short_name", out var boxContent);
			shortNameField = MakeInput(boxContent, "instance.create.short_name.placeholder", TMP_InputField.ContentType.Standard);
			shortNameField.text = Page.ShortName ?? string.Empty;
		}

		private TMP_InputField MakeInput(RectTransform parent, string placeholderKey, TMP_InputField.ContentType type) {
			var go = Instantiate(_inputFieldAsset, parent);
			go.AddComponent<LayoutElement>().preferredHeight = 60f;
			Reference.GetReference("image_container", go)?.SetActive(false);
			var field = Reference.GetComponent<TMP_InputField>("input", go);
			field.contentType = type;
			var placeholder = Reference.GetComponent<TextLanguage>("input_placeholder", go);
			if (placeholder)
				placeholder.UpdateText(placeholderKey);
			return field;
		}

		private TMP_InputField MakeTextArea(RectTransform parent, string placeholderKey) {
			var go = Instantiate(_textAreaAsset, parent);
			go.AddComponent<LayoutElement>().preferredHeight = 360f;
			var field = Reference.GetComponent<TMP_InputField>("input", go);
			field.lineType = TMP_InputField.LineType.MultiLineNewline;
			var placeholder = Reference.GetComponent<TextLanguage>("input_placeholder", go);
			if (placeholder)
				placeholder.UpdateText(placeholderKey);
			return field;
		}

		#endregion

		#region Text updates

		private void UpdateHeaderModeText() {
			if (!headerModeButtonText) return;
			headerModeButtonText.UpdateText(Page.GetMode() == InstanceCreationPage.ModeAdvanced ? "instance.create.mode.advanced" : "instance.create.mode.simple");
		}

		private void UpdateCapacityValue(float value) {
			var v = Mathf.RoundToInt(value);
			if (capacityValue)
				capacityValue.UpdateText(
					v == 0 ? "instance.create.capacity.unlimited" : "instance.create.capacity.slots",
					new[] { v.ToString() });
			if (capacityType)
				capacityType.UpdateText("instance.create.capacity.slots", new[] { GetMaxCapacity().ToString() });
		}

		private void OnCapacityChanged(float value) {
			_capacityTouched = true;
			UpdateCapacityValue(value);
		}

		private float InitialCapacity() {
			var capacity = Page.World?.Capacity ?? Page.Capacity;
			return CapacityToSlider(capacity);
		}

		private int GetMaxCapacity() {
			var worldCapacity = Page.World?.Capacity ?? (ushort)0;
			return Mathf.Min(Mathf.Max(MinCapacity, (int)worldCapacity * 2), ushort.MaxValue);
		}

		private float CapacityToSlider(ushort capacity) {
			if (capacity == 0) return 0f;
			return Mathf.Min((int)capacity, GetMaxCapacity());
		}

		private void ShowError(string key) {
			if (errorBox && errorText) {
				errorText.UpdateText(key);
				errorBox.SetActive(true);
			}
			if (createButton) createButton.interactable = false;
		}

		private void HideError() {
			if (errorBox) errorBox.SetActive(false);
			if (createButton) createButton.interactable = true;
		}

		#endregion

		#region Modals

		private void OpenModeModal() {
			var menu = Page.GetMenu();
			if (menu == null) return;
			var builder = Client.UiAPI.MakeModal(menu);
			if (builder == null) return;

			builder.SetTitle("instance.create.mode");
			builder.SetClosable(true);
			builder.SetContent("empty");
			builder.SetOptions(
				OnModeSelected,
				new Dictionary<string, string[]> {
					{ InstanceCreationPage.ModeSimple, new[] { "instance.create.mode.simple" } },
					{ InstanceCreationPage.ModeAdvanced, new[] { "instance.create.mode.advanced" } }
				}
			);

			var modal = builder.Build();
			modal.OnClose.AddListener(() => modal.Dispose());
			modal.Show();
		}

		private void OnModeSelected(string mode)
			=> Page.SetMode(mode);

		private async void OnValidateServer() {
			if (!serverInputField) return;
			var address = string.IsNullOrWhiteSpace(serverInputField.text) ? null : serverInputField.text.Trim();
			Page.Server = address;
			if (string.IsNullOrEmpty(address)) return;

			HideError();
			await CanHost(address);
		}

		private async UniTask<bool> CanHost(string address) {
			var api = Main.ServerAPI;
			if (api == null) {
				ShowError("instance.create.error.server_not_found");
				return false;
			}

			var server = await api.Fetch(address);
			if (server == null) {
				ShowError("instance.create.error.server_not_found");
				return false;
			}

			var capabilities = server.Capabilities ?? Array.Empty<string>();
			var current       = Main.UserAPI?.Current?.Server;
			var isCurrent     = !string.IsNullOrEmpty(current)
				&& string.Equals(server.Address, current, StringComparison.OrdinalIgnoreCase);

			var allowed = isCurrent
				? capabilities.Contains("allow_instance_creation")
				: capabilities.Contains("allow_instance_creation_by_external");

			if (!allowed)
				ShowError("instance.create.error.server_cannot_host");

			return allowed;
		}

		private async void OnValidateWorld() {
			if (!worldInputField) return;
			var value = worldInputField.text?.Trim();
			if (string.IsNullOrEmpty(value)) return;

			HideError();

			var world = await Main.WorldAPI.Fetch(Identifier.Parse(value));
			if (world == null) {
				ShowError("instance.create.error.world_not_found");
				return;
			}

			Page.World  = world;

			if (world.Release != null && world.Release.Value != ushort.MaxValue)
				Page.Version = world.Release.Value;
			else
				Page.Version = ushort.MaxValue;

			if (versionField)
				versionField.text = Page.Version == ushort.MaxValue ? string.Empty : Page.Version.ToString();

			if (capacitySlider) {
				capacitySlider.maxValue = GetMaxCapacity();
				if (!_capacityTouched)
					capacitySlider.SetValueWithoutNotify(CapacityToSlider(world.Capacity));
				else if (capacitySlider.value > capacitySlider.maxValue)
					capacitySlider.SetValueWithoutNotify(capacitySlider.maxValue);
			}
			UpdateCapacityValue(capacitySlider ? capacitySlider.value : 0f);
		}

		#endregion

		#region Create

		private async void OnCreateClicked() {
			HideError();

			if (Page.World == null) {
				ShowError("instance.create.error.no_world");
				return;
			}

			if (!string.IsNullOrEmpty(Page.Server) && !await CanHost(Page.Server))
				return;

			var request = new CreateInstanceRequest {
				World       = FormatWorldIdentifier(Page.World, Page.Version),
				Capacity    = GetCapacity(),
				Name        = shortNameField ? shortNameField.text : Page.ShortName,
				Title       = titleField ? titleField.text : Page.Title,
				Description = descriptionField ? descriptionField.text : Page.Description,
				Tags        = ParseTags(tagsField ? tagsField.text : (Page.Tags != null ? string.Join(",", Page.Tags) : null))
			};

			if (createButton) createButton.interactable = false;
			var instance = await Main.Instance.Network.Create(request, Page.Server);
			if (createButton) createButton.interactable = true;

			if (instance == null) {
				ShowError("instance.create.error.failed");
				return;
			}

			Page.GoToInstance(instance);
		}

		private ushort GetCapacity() {
			if (!capacitySlider) return Page.Capacity;
			var v = Mathf.RoundToInt(capacitySlider.value);
			if (v <= 0) return 0;
			return (ushort)Mathf.Min(v, GetMaxCapacity());
		}

		private static string FormatWorldIdentifier(IWorld world, ushort version) {
			var shortId = world.Identifier.ToShortString();
			if (version == ushort.MaxValue)
				return shortId;

			var idx = shortId.IndexOf('@');
			return idx >= 0
				? shortId.Insert(idx, $"?v={version}")
				: $"{shortId}?v={version}";
		}

		private static string[] ParseTags(string text) {
			if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
			return text
				.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(t => t.Trim())
				.Where(t => t.Length > 0)
				.ToArray();
		}

		#endregion

	}
}