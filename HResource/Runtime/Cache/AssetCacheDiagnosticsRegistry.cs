#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 살아있는 캐시를 모아 에디터 진단 창에 넘기고, 사라진 캐시의 누수를 판정합니다.
 *
 * 특징 / 지원기능 ::
 * 캐시가 생성자에서 스스로 등록하고, 창은 Collect 로 살아있는 것만 받아 갑니다.
 * + 캐시 본체는 약한 참조, 진단 손잡이는 강한 참조로 잡습니다
 * + CollectLeakSuspects 는 GC 되었는데 점유가 남아 있던 캐시를 찾아냅니다
 *
 * 주의사항 ::
 * 에디터 진단 전용이라 통째로 #if UNITY_EDITOR 로 감쌉니다.
 * 캐시를 강한 참조로 담으면 레지스트리 자체가 누수가 됩니다. 진단 도구가 누수를 만드는 셈입니다.
 * 반대로 손잡이까지 약하게 잡으면 캐시가 사라진 순간 판정 근거도 함께 사라집니다.
 *
 * 사용 ::
 * // 캐시 생성자에서
 * diagnosticsHandle = AssetCacheDiagnosticsRegistry.Register(this, typeof(TKey), typeof(TAsset));
 * =========================================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HResource.Cache {
    public static class AssetCacheDiagnosticsRegistry {
        #region Types
        sealed class Record {
            public AssetCacheDiagnosticsHandle Handle;
            public WeakReference<IAssetCacheDiagnostics> CacheRef;
        }
        #endregion

        #region Fields
        static readonly List<Record> records = new();
        static int nextSerial;
        #endregion

        #region Getter/Setter
        public static int RegisteredCount => records.Count;
        #endregion

        #region 일반 함수
        /// <summary> 캐시를 등록하고 그 캐시가 값을 쓸 진단 손잡이를 돌려준다 </summary>
        public static AssetCacheDiagnosticsHandle Register(IAssetCacheDiagnostics diagnostics, Type keyType, Type assetType) {
            nextSerial++;

            string keyName = keyType != null ? keyType.Name : "?";
            string assetName = assetType != null ? assetType.Name : "?";
            AssetCacheDiagnosticsHandle handle = new AssetCacheDiagnosticsHandle(
                $"#{nextSerial} MemoryAssetCache<{keyName}, {assetName}>");

            records.Add(new Record {
                Handle = handle,
                CacheRef = new WeakReference<IAssetCacheDiagnostics>(diagnostics),
            });
            return handle;
        }

        /// <summary> 살아있는 캐시로 버퍼를 채운다 </summary>
        public static void Collect(List<IAssetCacheDiagnostics> buffer) {
            if (buffer == null) return;
            buffer.Clear();

            for (int k = 0; k < records.Count; k++) {
                if (records[k].CacheRef.TryGetTarget(out IAssetCacheDiagnostics diagnostics)) {
                    buffer.Add(diagnostics);
                }
            }
        }

        /// <summary> GC 되었는데 점유가 남아 있던 캐시를 찾는다. Dispose 누락의 확정 증거다 </summary>
        public static void CollectLeakSuspects(List<string> buffer) {
            if (buffer == null) return;
            buffer.Clear();

            for (int k = 0; k < records.Count; k++) {
                Record record = records[k];
                if (record.CacheRef.TryGetTarget(out _)) continue;
                if (record.Handle.EntryCount < 1) continue;

                buffer.Add($"{record.Handle.Label} : {record.Handle.EntryCount} entrie(s) still held when it was collected");
            }
        }

        /// <summary> 아직 살아있으면서 점유를 들고 있는 캐시를 찾는다 </summary>
        public static void CollectLiveHolders(List<string> buffer) {
            if (buffer == null) return;
            buffer.Clear();

            for (int k = 0; k < records.Count; k++) {
                Record record = records[k];
                if (!record.CacheRef.TryGetTarget(out _)) continue;
                if (record.Handle.EntryCount < 1) continue;

                buffer.Add($"{record.Handle.Label} : {record.Handle.EntryCount} entrie(s) still held");
            }
        }
        #endregion

        #region Private
        // 플레이 세션을 넘겨 이전 세션의 기록이 쌓이는 것을 막는다.
        // AssetOwnerIdGenerator._ResetStatics 와 같은 시점을 쓴다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void _ResetStatics() {
            records.Clear();
            nextSerial = 0;
        }
        #endregion
    }
}

/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.09.04 누수 판정 기능 추가 (USR-4)
 *
 * # 변경
 * - 약한 참조 목록을 Record 구조로 바꿔 진단 손잡이를 강하게 함께 잡는다.
 * - CollectLeakSuspects 추가. 캐시는 GC 되었는데 손잡이의 EntryCount 가 0 이 아닌 기록을 찾는다.
 * - CollectLiveHolders 추가. 아직 살아있으면서 점유를 들고 있는 캐시를 찾는다.
 * - Collect 에서 죽은 참조를 지우지 않는다. 그 기록이 누수 판정의 근거이기 때문이다.
 *
 * # 이유
 * - 케이스 리포트 USR-4. provider 를 Dispose 없이 버리면 Addressable 핸들이 영구 잔존하는데,
 *   캐시가 GC 되면서 진단 대상에서도 사라져 관측 자체가 불가능했다.
 * - 캐시보다 오래 사는 손잡이를 두면 "사라질 때 몇 개를 들고 있었나" 가 남는다.
 *
 * # 주의
 * - 죽은 참조를 더 이상 정리하지 않으므로 기록이 플레이 세션 동안 단조 증가한다.
 *   provider 수가 많지 않고 세션 시작마다 비우므로 실무상 문제되지 않는다.
 *
 * =============================================================================
 * @Jason - PKH 2026.09.04 AssetCacheDiagnosticsRegistry 베이스 코드 생성
 *
 * # 목적
 * - 캐시는 provider 마다 new 로 만들어져 어디에도 등록되지 않는다.
 *   에디터 창이 살아있는 캐시에 닿을 유일한 연결 고리를 만든다.
 *
 * =============================================================================
 */
#endif
