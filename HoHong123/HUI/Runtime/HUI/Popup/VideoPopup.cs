using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Sirenix.OdinInspector;

namespace HUI.Popup {
    public class VideoPopup : BasePopupUi {
        [Title("Video")]
        [SerializeField]
        VideoPlayer video;
        [SerializeField]
        RenderTexture render;

        [Title("Button")]
        [SerializeField]
        Button panelBtn;

        public event Action OnClickPanel;


        protected override void Start() {
            base.Start();
            panelBtn.onClick.AddListener(_HandlePanelClicked);
            video.Stop();
        }

        protected override void OnDestroy() {
            if (panelBtn != null) {
                panelBtn.onClick.RemoveListener(_HandlePanelClicked);
            }
            OnClickPanel = null;
            base.OnDestroy();
        }

        private void OnDisable() {
            video.Stop();
        }

        private void _HandlePanelClicked() {
            OnClickPanel?.Invoke();
        }


        public void SetVideo(string url, int width = 0, int height = 0) {
            video.Stop();
            video.url = url;
            video.Play();

            if (width > 0) render.width = width;
            if (height > 0) render.height = height;
        }
    }
}