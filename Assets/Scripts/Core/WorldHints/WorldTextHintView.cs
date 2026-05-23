using Catsss.Core.Localization;
using TMPro;
using UnityEngine;

namespace Catsss.Core.WorldHints
{
    /// <summary>
    /// World-space TMP подсказка с billboard к камере владельца.
    /// Parent переносится на якорь; весь Canvas двигается целиком.
    /// </summary>
    public class WorldTextHintView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Vector3 localOffset = new(0f, 1.4f, 0f);

        private Transform _homeParent;
        private Vector3 _defaultLocalScale = Vector3.one;
        private Camera _worldCamera;
        private LocalizedTextReference _boundText;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.01f;
        public Vector3 DefaultLocalOffset => localOffset;

        private void Awake()
        {
            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>(true);
            }

            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();

                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            _homeParent = transform.parent;
            _defaultLocalScale = transform.localScale;
            SetVisible(false);
        }

        private void OnDestroy()
        {
            UnbindText();
        }

        public void SetWorldCamera(Camera camera)
        {
            _worldCamera = camera;

            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && camera != null)
            {
                canvas.worldCamera = camera;
            }
        }

        public void Bind(Transform anchor, Vector3 offset, LocalizedTextReference text)
        {
            if (anchor == null)
            {
                Clear();
                return;
            }

            transform.SetParent(anchor, false);
            transform.localPosition = offset;
            transform.localRotation = Quaternion.identity;
            transform.localScale = _defaultLocalScale;

            BindText(text);
            ApplyBillboardImmediate();
            SetVisible(true);
        }

        public void Clear()
        {
            UnbindText();
            SetVisible(false);

            if (_homeParent != null)
            {
                transform.SetParent(_homeParent, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = _defaultLocalScale;
            }
        }

        private void LateUpdate()
        {
            if (!IsVisible)
            {
                return;
            }

            ApplyBillboardImmediate();
        }

        private void ApplyBillboardImmediate()
        {
            if (_worldCamera == null)
            {
                return;
            }

            if (canvas != null && canvas.worldCamera != _worldCamera)
            {
                canvas.worldCamera = _worldCamera;
            }

            Vector3 faceCamera = transform.position - _worldCamera.transform.position;

            if (faceCamera.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(faceCamera, Vector3.up);
            }
        }

        private void BindText(LocalizedTextReference text)
        {
            if (ReferenceEquals(_boundText, text))
            {
                return;
            }

            UnbindText();
            _boundText = text;
            _boundText?.Bind(ApplyLabelText);
        }

        private void UnbindText()
        {
            _boundText?.Unbind();
            _boundText = null;
        }

        private void SetVisible(bool visible)
        {
            if (canvas != null)
            {
                canvas.enabled = visible;
            }

            if (label != null)
            {
                label.enabled = visible;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void ApplyLabelText(string text)
        {
            if (label != null)
            {
                label.text = text ?? string.Empty;
            }
        }
    }
}
