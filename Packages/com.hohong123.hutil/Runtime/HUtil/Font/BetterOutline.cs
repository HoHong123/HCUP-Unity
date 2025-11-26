using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HUtil.Font {
    [RequireComponent(typeof(Text))]
    public class BetterOutline : Shadow {
        private List<UIVertex> m_Verts = new List<UIVertex>();


#if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();
        }
#endif

        public override void ModifyMesh(VertexHelper vh) {
            if (!IsActive()) {
                return;
            }

            vh.GetUIVertexStream(m_Verts);

            int initialVertexCount = m_Verts.Count;

            var start = 0;
            var end = 0;

            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if ((i != 0) && (j != 0)) {
                        start = end;
                        end = m_Verts.Count;
                        ApplyShadowZeroAlloc(m_Verts, effectColor, start, m_Verts.Count, i * effectDistance.x * 0.707f, j * effectDistance.y * 0.707f);
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


            if (GetComponent<Text>().material.shader == Shader.Find("Text Effects/Fancy Text")) {
                for (int i = 0; i < m_Verts.Count - initialVertexCount; i++) {
                    UIVertex vert = m_Verts[i];
                    vert.uv1 = new Vector2(0, 0);
                    m_Verts[i] = vert;
                }
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(m_Verts);
        }
    }
}
