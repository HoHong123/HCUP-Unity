#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 배포 도구 공용 로그 버퍼 클래스입니다.
 *
 * 특징 / 지원기능 ::
 * 배포 진행 로그를 시간순으로 축적하고 창(EditorWindow)에 표시할 엔트리를 제공한다.
 * + Info / Warning / Error 3단계 심각도 (DeployLogSeverity)
 * + 모든 엔트리는 Unity Console(Debug.*)에도 미러 출력
 *
 * 사용 ::
 * 서비스 계층이 Info/Warning/Error를 호출하고, 창은 Entries를 읽어 그린다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HDeploy.Deploy {
    public sealed class DeployLog {
        #region 상수
        const string TIMESTAMP_FORMAT = "HH:mm:ss";
        const string MIRROR_PREFIX = "[HDeploy]";
        #endregion

        #region 변수
        readonly List<DeployLogEntry> entries = new List<DeployLogEntry>();
        #endregion

        #region Events
        public event Action OnChanged;
        #endregion

        #region Getter/Setter
        public IReadOnlyList<DeployLogEntry> Entries => entries;
        #endregion

        #region 일반 함수
        public void Info(string message) {
            _Append(DeployLogSeverity.Info, message);
            Debug.Log($"{MIRROR_PREFIX} {message}");
        }

        public void Warning(string message) {
            _Append(DeployLogSeverity.Warning, message);
            Debug.LogWarning($"{MIRROR_PREFIX} {message}");
        }

        public void Error(string message) {
            _Append(DeployLogSeverity.Error, message);
            Debug.LogError($"{MIRROR_PREFIX} {message}");
        }

        public void Clear() {
            entries.Clear();
            OnChanged?.Invoke();
        }

        private void _Append(DeployLogSeverity level, string message) {
            string stamped = $"[{DateTime.Now.ToString(TIMESTAMP_FORMAT)}] {message}";
            entries.Add(new DeployLogEntry(level, stamped));
            OnChanged?.Invoke();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.03 Deploy 디렉토리 재배치
 *
 * # 코드 리펙토링
 * - Editor 루트 → Editor/Deploy/ 이동, namespace HDeploy → HDeploy.Deploy (디렉토리 기반 네임스페이스 컨벤션).
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 중첩 타입 파일 분리
 *
 * # 코드 리펙토링
 * - 중첩 Severity/Entry를 DeployLogSeverity/DeployLogEntry 파일로 분리 (public 타입 파일 분리 컨벤션).
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 DeployLog 베이스 코드 생성
 *
 * # 목적
 * - HDeploy 패키지 공용 로그 버퍼. 창 표시용 엔트리 축적 + Unity Console 미러.
 *
 * # 사용 흐름
 * - 서비스 계층이 Info/Warning/Error 호출 → OnChanged로 창이 Repaint.
 *
 * =============================================================================
 */
#endif
