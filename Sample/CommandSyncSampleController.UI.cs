using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Procedural UI construction: everything created here is parented under _contentRoot (a scroll view's
// content, see SetupScrollableContent), so the dashboard can grow without anything falling off-screen.
public partial class CommandSyncSampleController
{
	// The scene's "Buttons" column was originally sized for a dozen static buttons with no scrolling.
	// The dashboard now has far more content than that (and keeps growing), so turn _buttonsContainer into
	// a scroll view's viewport instead: everything BuildXSection() creates goes into a new "Content" child
	// that grows downward and scrolls within it, so nothing at the bottom ends up permanently off-screen
	// and unreachable/unclickable. Done entirely in code so the scene asset itself is never hand-edited.
	private void SetupScrollableContent()
	{
		var existingLayoutGroup = _buttonsContainer.GetComponent<VerticalLayoutGroup>();
		if (existingLayoutGroup != null)
		{
			Destroy(existingLayoutGroup);
		}

		var viewportRect = (RectTransform)_buttonsContainer;
		if (_buttonsContainer.GetComponent<RectMask2D>() == null)
		{
			_buttonsContainer.gameObject.AddComponent<RectMask2D>();
		}

		var contentGo = new GameObject("Content", typeof(RectTransform));
		var contentRect = (RectTransform)contentGo.transform;
		contentRect.SetParent(_buttonsContainer, false);
		contentRect.anchorMin = new Vector2(0, 1);
		contentRect.anchorMax = new Vector2(1, 1);
		contentRect.pivot = new Vector2(0.5f, 1);
		contentRect.anchoredPosition = Vector2.zero;
		// A freshly created RectTransform defaults to sizeDelta (100, 100). Left as-is, that adds on top of
		// the stretch-anchor width (parent width + 100), overflowing the viewport by 50px on each side —
		// the "negative margins" pushing children out left/right. Must be zeroed explicitly for a clean
		// full-width stretch; the height component is irrelevant, ContentSizeFitter overwrites it below.
		contentRect.sizeDelta = Vector2.zero;

		var layout = contentGo.AddComponent<VerticalLayoutGroup>();
		layout.padding = new RectOffset(20, 20, 20, 20);
		layout.spacing = 10;
		layout.childAlignment = TextAnchor.UpperCenter;
		layout.childControlWidth = true;
		layout.childControlHeight = false;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = false;

		var fitter = contentGo.AddComponent<ContentSizeFitter>();
		fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		var scrollRect = _buttonsContainer.gameObject.AddComponent<ScrollRect>();
		scrollRect.content = contentRect;
		scrollRect.viewport = viewportRect;
		scrollRect.horizontal = false;
		scrollRect.vertical = true;
		scrollRect.movementType = ScrollRect.MovementType.Clamped;
		scrollRect.scrollSensitivity = 20f;

		_contentRoot = contentRect;
	}

	private void BuildLoginSection()
	{
		CreateSectionHeader("Login");
		CreateLabel(_contentRoot, "Enter any Id to (re)connect as that player. A brand-new Id gets a random guest name.",
			fontSize: 13, height: 44, style: FontStyles.Italic);

		var row = CreateRow(44);
		_loginIdInput = CreateInputField(row, "Player Id");
		SetFlexibleWidth(_loginIdInput.GetComponent<RectTransform>(), flexibleWidth: 1);
		var loginButton = CreateButton(row, "Login", OnLoginClicked);
		SetFlexibleWidth(loginButton, flexibleWidth: 0, preferredWidth: 90);
	}

	private void BuildProfileSection()
	{
		CreateSectionHeader("Profile");

		_profileInfoText = CreateLabel(_contentRoot, "Not logged in.", fontSize: 16, height: 130);
		_profileInfoText.alignment = TextAlignmentOptions.TopLeft;

		var nameRow = CreateRow(44);
		_playerNameInput = CreateInputField(nameRow, "New display name");
		SetFlexibleWidth(_playerNameInput.GetComponent<RectTransform>(), flexibleWidth: 1);
		var saveNameButton = CreateButton(nameRow, "Save", OnSaveNameClicked);
		SetFlexibleWidth(saveNameButton, flexibleWidth: 0, preferredWidth: 90);

		var xpRow = CreateRow(50, equalWidths: true);
		CreateButton(xpRow, "+10 XP", () => OnAddXpClicked(10));
		CreateButton(xpRow, "+50 XP", () => OnAddXpClicked(50));
		CreateButton(xpRow, "+100 XP", () => OnAddXpClicked(100));
	}

	private void BuildLeaderboardSection()
	{
		CreateSectionHeader("Leaderboard (XP)");

		_leaderboardText = CreateLabel(_contentRoot, "-", fontSize: 20, height: 160);
		_leaderboardText.alignment = TextAlignmentOptions.TopLeft;

		_myRankText = CreateLabel(_contentRoot, "", fontSize: 13, height: 22, style: FontStyles.Italic);

		var row = CreateRow(50, equalWidths: true);
		CreateButton(row, "Submit My Score", OnSubmitScoreClicked);
		CreateButton(row, "Refresh", RefreshLeaderboardView);
		CreateButton(row, "Clear", OnClearLeaderboardClicked);
	}

	private void BuildUtilitySection()
	{
		CreateSectionHeader("Utilities");
		CreateButton("Sync (Clear Queue)", Sync);
		CreateButton("Clear Client Data (keep server)", ClearClientData);
		CreateButton("Clear All (Client + Server)", ResetAll);
	}

	private void CreateSectionHeader(string text)
	{
		var label = CreateLabel(_contentRoot, text, fontSize: 22, height: 34, style: FontStyles.Bold);
		label.alignment = TextAlignmentOptions.Midline;
	}

	private TextMeshProUGUI CreateLabel(Transform parent, string text, int fontSize = 18, float height = 30,
		FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
	{
		var go = new GameObject("Label", typeof(RectTransform));
		go.transform.SetParent(parent, false);
		((RectTransform)go.transform).sizeDelta = new Vector2(0, height);

		var label = go.AddComponent<TextMeshProUGUI>();
		label.text = text;
		label.fontSize = fontSize;
		label.fontStyle = style;
		label.alignment = align;
		label.color = Color.white;
		return label;
	}

	private TMP_InputField CreateInputField(Transform parent, string placeholder, float height = 44)
	{
		var go = new GameObject("InputField (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
		go.transform.SetParent(parent, false);
		((RectTransform)go.transform).sizeDelta = new Vector2(0, height);
		go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

		var text = CreateStretchedText(go.transform, string.Empty, Color.white, FontStyles.Normal);
		var placeholderText = CreateStretchedText(go.transform, placeholder, new Color(1f, 1f, 1f, 0.5f), FontStyles.Italic);

		var inputField = go.GetComponent<TMP_InputField>();
		inputField.textComponent = text;
		inputField.placeholder = placeholderText;
		inputField.text = string.Empty;
		return inputField;
	}

	private TextMeshProUGUI CreateStretchedText(Transform parent, string text, Color color, FontStyles style)
	{
		var go = new GameObject("Text", typeof(RectTransform));
		go.transform.SetParent(parent, false);
		var rect = (RectTransform)go.transform;
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = new Vector2(12, 6);
		rect.offsetMax = new Vector2(-12, -6);

		var tmp = go.AddComponent<TextMeshProUGUI>();
		tmp.text = text;
		tmp.fontSize = 20;
		tmp.color = color;
		tmp.fontStyle = style;
		tmp.alignment = TextAlignmentOptions.MidlineLeft;
		return tmp;
	}

	private RectTransform CreateRow(float height, bool equalWidths = false)
	{
		var go = new GameObject("Row", typeof(RectTransform));
		go.transform.SetParent(_contentRoot, false);
		((RectTransform)go.transform).sizeDelta = new Vector2(0, height);

		var layout = go.AddComponent<HorizontalLayoutGroup>();
		layout.spacing = 10;
		layout.childAlignment = TextAnchor.MiddleLeft;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = equalWidths;
		layout.childForceExpandHeight = true;

		return (RectTransform)go.transform;
	}

	private void SetFlexibleWidth(RectTransform rect, float flexibleWidth, float preferredWidth = -1)
	{
		var layoutElement = rect.GetComponent<LayoutElement>();
		if (layoutElement == null)
		{
			layoutElement = rect.gameObject.AddComponent<LayoutElement>();
		}
		layoutElement.flexibleWidth = flexibleWidth;
		if (preferredWidth >= 0)
		{
			layoutElement.preferredWidth = preferredWidth;
		}
	}

	private void CreateButton(string label, Action onClick) => CreateButton(_contentRoot, label, onClick);

	private RectTransform CreateButton(Transform parent, string label, Action onClick)
	{
		var buttonObj = Instantiate(_buttonPrefab, parent);
		var button = buttonObj.GetComponent<Button>();
		var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
		text.text = label;
		button.onClick.AddListener(() => onClick());
		return (RectTransform)buttonObj.transform;
	}

	private void CreateButton(string label, Func<Task> onClickAsync)
	{
		var buttonObj = Instantiate(_buttonPrefab, _contentRoot);
		var button = buttonObj.GetComponent<Button>();
		var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
		text.text = label;
		// Fire-and-forget: without this try/catch, an exception thrown inside onClickAsync would just
		// fault the discarded Task silently — the button would appear to do nothing, with no console log
		// at all to explain why.
		button.onClick.AddListener(() => _ = RunAndLogErrors(onClickAsync));
	}

	private async Task RunAndLogErrors(Func<Task> action)
	{
		try
		{
			await action();
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}
}
