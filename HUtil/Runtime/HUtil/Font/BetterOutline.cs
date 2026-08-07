#if UNITY_EDITOR
/* =========================================================
 * Unity UI Text 컴포넌트에 고품질 외곽선을 적용하기 위한 커스텀 Outline 컴포넌트입니다.
 *
 * 특징 ::
 * Unity 기본 Outline은 4방향 Shadow만 사용합니다.
 *
 * 본 구현은 대각선 4방향 + 상하좌우 4방향, 총 8개 Shadow를 사용하여 외곽선을 생성합니다.
 *
 * 참고 ::
 * 본 코드는 외부 소스에서 가져온 코드입니다.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HUtil.Font {
    [RequireComponent(typeof(Text))]
    public class BetterOutline : Shadow {
        #region Fields
        private static readonly Shader fancyTextShader = Shader.Find("Text Effects/Fancy Text");

        private List<UIVertex> m_Verts = new List<UIVertex>();
        private Text cachedText;
        #endregion

#if UNITY_EDITOR
        #region UI Validation
        protected override void OnValidate() {
            base.OnValidate();
        }
        #endregion
#endif

        #region Public - Modify Mesh
        public override void ModifyMesh(VertexHelper vh) {
            if (!IsActive()) {
                return;
            }

            vh.GetUIVertexStream(m_Verts);

            int initialVertexCount = m_Verts.Count;

            var start = 0;
            var end = 0;

            for (int k = -1; k <= 1; k++) {
                for (int j = -1; j <= 1; j++) {
                    if ((k != 0) && (j != 0)) {
                        start = end;
                        end = m_Verts.Count;
                        ApplyShadowZeroAlloc(m_Verts, effectColor, start, m_Verts.Count, k * effectDistance.x * 0.707f, j * effectDistance.y * 0.707f);
                    }
                }
            }

            start = end;
            end = m_Verts.Count;
            ApplyShadowZeroAlloc(m_Verts, effectColor, start, m_Verts.Count, -effectDistance.x, 0);

            start = end;
            end = m_Verts.Count;
            ApplyShadowZeroAlloc(m_Verts, effectColor, start, m_Verts.Count, effectDistance.x, 0);


            start = end;
            end = m_Verts.Count;
            ApplyShadowZeroAlloc(m_Verts, effectColor, start, m_Verts.Count, 0, -effectDistance.y);

            start = end;
            end = m_Verts.Count;
            ApplyShadowZeroAlloc(m_Verts, effectColor, start, m_Verts.Count, 0, effectDistance.y);


            if (!cachedText) cachedText = GetComponent<Text>();
            if (cachedText.material.shader == fancyTextShader) {
                for (int k = 0; k < m_Verts.Count - initialVertexCount; k++) {
                    UIVertex vert = m_Verts[k];
                    vert.uv1 = new Vector2(0, 0);
                    m_Verts[k] = vert;
                }
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(m_Verts);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.08.07 UI 리빌드 비용 절감 + 헤더 Shadow 개수 정정
 *
 * # 변경
 * - Shader.Find("Text Effects/Fancy Text") 를 static readonly 필드로 1회 캐싱
 * - GetComponent<Text>() 를 cachedText 필드로 캐싱 (최초 1회만 조회)
 * - 헤더 주석 "12개 Shadow" → 실제 개수인 "8개 Shadow" 로 정정
 *
 * =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * ModifyMesh
 *  + Text Vertex 복제
 *  + 다방향 Shadow 생성
 *
 * Shader 대응 ::
 * Fancy Text Shader 사용 시
 * uv1 초기화 처리
 *
 * 사용법 ::
 * Text 컴포넌트에 추가하여 사용
 *
 * 기타 ::
 * 기본 Outline보다 높은 렌더 품질 제공
 * =========================================================
 */
#endif