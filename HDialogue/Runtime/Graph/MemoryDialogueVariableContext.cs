#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- IDialogueVariableContext Dictionary 기반 기본 구현체.
 *
 * 특징 / 지원기능 ::
 * + bool / int / string 세 타입 별도 Dictionary 관리
 * + TryGet: 키 없으면 false 반환 (out 기본값 유지)
 * + AddInt: 키 없으면 0에서 누산 시작
 * + ToggleBool: 키 없으면 false → true
 *
 * 주의사항 ::
 * 메모리 한정 구현 — 씬 재로드 또는 인스턴스 소멸 시 데이터 초기화.
 * 영구 저장이 필요하면 IDialogueVariableContext를 직접 구현할 것.
 * =========================================================
 */
#endif

using System.Collections.Generic;

namespace HDialogue {
    public sealed class MemoryDialogueVariableContext : IDialogueVariableContext {
        #region Fields
        Dictionary<string, bool> boolVars = new Dictionary<string, bool>();
        Dictionary<string, int> intVars = new Dictionary<string, int>();
        Dictionary<string, string> stringVars = new Dictionary<string, string>();
        #endregion

        #region IDialogueVariableContext
        public bool TryGetBool(string key, out bool value) => boolVars.TryGetValue(key, out value);
        public bool TryGetInt(string key, out int value) => intVars.TryGetValue(key, out value);
        public bool TryGetString(string key, out string value) => stringVars.TryGetValue(key, out value);

        public void SetBool(string key, bool value) => boolVars[key] = value;
        public void SetInt(string key, int value) => intVars[key] = value;
        public void SetString(string key, string value) => stringVars[key] = value;

        public void AddInt(string key, int delta) {
            intVars.TryGetValue(key, out int current);
            intVars[key] = current + delta;
        }

        public void ToggleBool(string key) {
            boolVars.TryGetValue(key, out bool current);
            boolVars[key] = !current;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 MemoryDialogueVariableContext 베이스 코드 생성
 *
 * # 목적
 * - IDialogueVariableContext 기본 구현체 (Dictionary 기반, 세션 한정)
 * - HCUP-2.3.0 Phase 0
 *
 * # 설계 결정
 * - bool / int / string 타입별 Dictionary 분리: TryGetValue 타입 안전성 확보
 * - AddInt: 키 없으면 default(int)=0에서 시작 — Dictionary.TryGetValue 기본값 활용
 * - sealed: 구현체는 파생 불필요, 인터페이스로 교체 설계
 *
 * =============================================================================
 */
#endif
