using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using HDiagnosis.Logger;
using HResource.Data;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Addressable 단일 asset 로더 구현 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 성공한 handle은 반드시 release 경로와 짝을 맞춰야 합니다.
 * 2. key 정규화 규칙이 addressable 주소 규칙과 맞아야 합니다.
 *
 * 주의 :: 이 로더는 SharedAssetLoadGate 밖에서 동시 호출되면 안 된다.
 * handleTable 조회가 await 앞, 등록이 await 뒤라 동시 진입 시 LoadAssetAsync 가 두 번 불려
 * Addressables refcount 가 2 로 오르고 Release 1회로는 0 에 도달하지 못한다.
 * 진입 직렬화는 AssetProvider 의 게이트가 담당한다.
 * =========================================================
 */
#endif

namespace HResource.Load {
    public sealed class AddressableAssetLoader<TAsset> : IAssetReleasableLoader<string, TAsset>
        where TAsset : Object {
        #region Fields
        readonly Dictionary<string, AsyncOperationHandle<TAsset>> handleTable = new();
        #endregion

        #region Properties
        public AssetLoadMode LoadMode => AssetLoadMode.Addressable;
        #endregion

        #region Public - Load
        public async UniTask<TAsset> LoadAsync(string key) {
            var normalizedKey = _NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey)) return null;

            if (handleTable.TryGetValue(normalizedKey, out var cachedHandle)) {
                if (cachedHandle.IsValid()) return cachedHandle.Result;
                handleTable.Remove(normalizedKey);
            }

            var handle = Addressables.LoadAssetAsync<TAsset>(normalizedKey);
            try {
                // 실패한 handle 의 await 는 예외를 throw 한다 (UniTask) - 사후 Status 검사는 도달 불가.
                await handle.ToUniTask();
            }
            catch (System.Exception e) {
                if (handle.IsValid()) Addressables.Release(handle);
                HLogger.Error($"[AddressableAssetLoader] Load failed. Key='{normalizedKey}' :: {e.Message}");
                return null;
            }

            handleTable[normalizedKey] = handle;
            return handle.Result;
        }
        #endregion

        #region Public - Release
        public bool Release(string key) {
            var normalizedKey = _NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey)) {
                return false;
            }

            if (!handleTable.TryGetValue(normalizedKey, out var handle)) {
                return false;
            }

            if (handle.IsValid()) {
                Addressables.Release(handle);
            }

            handleTable.Remove(normalizedKey);
            return true;
        }

        public void ReleaseAll() {
            foreach (var handle in handleTable.Values) {
                if (handle.IsValid()) Addressables.Release(handle);
            }

            handleTable.Clear();
        }
        #endregion

        #region Private - Normalize
        private string _NormalizeKey(string key) {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            return key.Trim();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. 단일 key 기반 Addressable 로드를 수행합니다.
 * 2. 성공한 handle을 보관합니다.
 * 3. source release를 직접 처리합니다.
 *
 * 사용법 ::
 * 1. AssetProvider의 Addressable loader로 등록해 사용합니다.
 * 2. Addressable source release가 필요할 때 IAssetReleasableLoader 경로를 함께 사용합니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. provider release 흐름과 연결되어 handle release가 일어납니다.
 *
 * 기타 ::
 * 1. Addressable label 로드는 별도 loader로 분리되어 있습니다.
 * 2. source 책임만 맡고 cache 정책은 provider가 담당합니다.
 * =========================================================
 */
#endif

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-08 (수정) :: 게이트 밖 동시 호출 금지 명시
 *
 * 변경 ::
 * 헤더 주의 절에 SharedAssetLoadGate 의존을 적었다.
 *
 * 이유 ::
 * handleTable 조회가 await 앞, 등록이 await 뒤라 이 로더 단독으로는 동시 진입을 막지 못한다.
 * 게이트 없이 동시 요청 2건이 들어오면 Addressables refcount 가 2 로 오르고
 * Release 1회로는 0 에 도달하지 못한다.
 *
 * 결과 ::
 * 이 파일만 읽는 사람도 진입 직렬화 책임이 상위에 있음을 안다.
 *
 * 주의 ::
 * handleTable 은 완료된 핸들의 캐시다. 진행 중 상태를 표현하도록 바꾸려 하지 말 것.
 *
 * =========================================================
 */
#endif
