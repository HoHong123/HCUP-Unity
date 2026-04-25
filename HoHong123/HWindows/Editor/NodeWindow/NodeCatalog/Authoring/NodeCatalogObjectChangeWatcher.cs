using UnityEditor;
using UnityEngine;
using HWindows.NodeWindow;

namespace HWindows.Editor.NodeWindow.Authoring {
    [InitializeOnLoad]
    internal static class NodeCatalogObjectChangeWatcher {
        static NodeCatalogObjectChangeWatcher() {
            ObjectChangeEvents.changesPublished += _ChangesPublished;
        }

        private static void _ChangesPublished(ref ObjectChangeEventStream stream) {
            for (int k = 0; k < stream.length; k++) {
                ObjectChangeKind kind = stream.GetEventType(k);
                if (kind != ObjectChangeKind.ChangeAssetObjectProperties) continue;

                stream.GetChangeAssetObjectPropertiesEvent(k, out ChangeAssetObjectPropertiesEventArgs data);
                Object obj = EditorUtility.InstanceIDToObject(data.instanceId);
                if (obj is NodeCatalogSO catalog) {
                    NodeCatalogAuthor.NotifyExternalMutation(catalog);
                }
            }
        }
    }
}

#if UNITY_EDITOR
// =============================================================================
// Dev Log
// =============================================================================
// @Jason - PKH 2026-04-25 NodeCatalogObjectChangeWatcher - Inspector 직접 수정 감지
//
//   [존재 이유]
//   - NodeCatalogSO 본체에 OnValidate / 정적 이벤트를 두지 않으면서, Inspector 에서 catalog 의
//     RootUID 등을 직접 수정해도 시각 레이어 (HGraphCanvas) 가 자동 새로고침되도록 보장.
//   - Author 의 mutation 메서드 호출은 이미 NodeCatalogAuthor.CatalogMutated 발송. 이 watcher 는
//     Author 우회 경로 (Inspector SerializedProperty 직접 수정) 만 보강.
//
//   [Unity API]
//   - ObjectChangeEvents.changesPublished (Unity 2022+) 가 SerializedProperty Apply 시점에 발송.
//   - ChangeAssetObjectProperties 이벤트는 .asset 파일의 프로퍼티 변경 시 트리거.
//   - 더블 발송 우려: Author 가 SetDirty + SaveAssets 호출 시에도 이벤트 발생할 수 있음.
//     _Populate idempotent 라 결과는 동일. 비효율 미미. 깜빡임 관찰 시 디바운스 도입.
//
//   [InitializeOnLoad]
//   - Editor 어셈블리 로드 시 한 번 실행. 정적 이벤트 구독 영구 유지.
//   - Domain reload 후에도 자동 재구독.
//
//   [필터]
//   - obj is NodeCatalogSO 만 통과. 다른 asset 변경은 무시 -> 성능 부담 0.
// =============================================================================
#endif
