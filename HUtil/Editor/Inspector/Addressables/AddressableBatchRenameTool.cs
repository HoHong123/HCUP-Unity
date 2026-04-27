#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 이 스크립트는 Addressables에 등록된 엔트리의 address를
 * 파일명 기반으로 일괄 재설정하는 에디터 유틸리티입니다.
 * 
 * 주의사항 ::
 * 1. 중복 파일명이 존재하면 address 충돌이 발생할 수 있습니다.
 * 2. 실행 전 버전관리 커밋 상태를 정리한 뒤 사용하는 것이 안전합니다.
 * =========================================================
 */
#endif

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using HDiagnosis.Logger;

namespace HUtil.Editor.Addressables {
    public static class AddressableBatchRenameTool {
        #region Public Functions
        [MenuItem("HCUP/Addressables/Rename All Addresses To File Name")]
        public static void RenameAllAddressesToFileName() {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) {
                HLogger.Log("[AddressableBatchRenameTool] AddressableAssetSettings is null.");
                return;
            }

            var duplicatedAddressTable = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var changedCount = 0;

            foreach (var group in settings.groups) {
                if (group == null) continue;

                foreach (var entry in group.entries) {
                    if (entry == null) continue;

                    var assetPath = entry.AssetPath;
                    if (string.IsNullOrWhiteSpace(assetPath)) continue;

                    var newAddress = _BuildAddressFromPath(assetPath);
                    if (string.IsNullOrWhiteSpace(newAddress)) continue;

                    if (!duplicatedAddressTable.TryGetValue(newAddress, out var paths)) {
                        paths = new List<string>();
                        duplicatedAddressTable.Add(newAddress, paths);
                    }

                    paths.Add(assetPath);

                    if (string.Equals(entry.address, newAddress, StringComparison.Ordinal)) continue;

                    entry.SetAddress(newAddress, false);
                    changedCount++;
                }
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, null, true);
            AssetDatabase.SaveAssets();

            _LogDuplicateAddresses(duplicatedAddressTable);

            HLogger.Log($"[AddressableBatchRenameTool] Rename complete. Changed={changedCount}");
        }
        #endregion

        #region Private Functions
        private static string _BuildAddressFromPath(string assetPath) {
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            if (string.IsNullOrWhiteSpace(fileName)) return string.Empty;
            return _NormalizeAddress(fileName);
        }

        private static string _NormalizeAddress(string raw) {
            var normalized = raw.Trim().Replace(" ", "_").Replace("-", "_");
            return normalized;
        }

        private static void _LogDuplicateAddresses(Dictionary<string, List<string>> duplicatedAddressTable) {
            foreach (var pair in duplicatedAddressTable) {
                if (pair.Value.Count <= 1) continue;
                HLogger.Warning(
                    $"[AddressableBatchRenameTool] Duplicate address detected: '{pair.Key}'\n" +
                    $"{string.Join("\n", pair.Value)}");
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 :: 
 * 1. 현재 Addressables 엔트리 전체를 순회합니다.
 * 2. 각 엔트리의 Address를 파일명 기준으로 재설정합니다.
 * 3. 중복 Address 후보를 로그로 출력합니다.
 * 
 * 사용법 :: 
 * 1. Addressables에 에셋을 먼저 등록합니다.
 * 2. 상단 메뉴 Tools/DR2/Addressables/Rename All Addresses To File Name 를 실행합니다.
 * 3. 콘솔의 중복 경고를 확인하고 필요한 경우 수동 수정합니다.
 * 
 * 변수 설명 ::
 * X
 * XX
 * XXX
 * 
 * 이벤트 ::
 * 없음
 * 
 * 기타 ::
 * 1. 파일명만 사용하므로 동일 파일명 자산이 많다면 prefix 규칙을 추가하는 편이 안전합니다.
 * 2. 필요하면 _BuildAddressFromPath()에서 폴더명 prefix를 붙이도록 확장할 수 있습니다.
 * =========================================================
 */
#endif