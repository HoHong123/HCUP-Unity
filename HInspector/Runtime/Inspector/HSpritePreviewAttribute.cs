#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * string 또는 Object 필드에 부착해 인스펙터 우측 인라인 Sprite 미리보기를 활성화합니다.
 *
 * 사용 예 ::
 * [HSpritePreview]         public string Icon;   // 기본 48px
 * [HSpritePreview(64f)]    public string Icon;   // 커스텀 크기
 *
 * 특징 ::
 * - string 필드 : Resources 경로 또는 Addressables 키로 탐색
 * - Object 필드 : Sprite / Texture2D 순으로 대응
 *
 * 주의사항 ::
 * - 적용부는 [Conditional] 로 빌드에서 제거 — 클래스 정의는 런타임 어셈블리에 잔류
 * - HInspectorAttribute 파생 제외 — HInspectorPropertyDrawer 처리 대상에서 분리
 * =========================================================
 */
#endif

using UnityEngine;

namespace HInspector {
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class HSpritePreviewAttribute : PropertyAttribute {
        public float Size { get; }
        public HSpritePreviewAttribute(float size = 48f) => Size = size;
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.13 (수정) :: 클래스 본체 #if UNITY_EDITOR 가드 해제
 *
 * # 변경
 * - 헤더 주석 가드를 클래스 선언 앞에서 닫아, 클래스 본체를 가드 밖으로 되돌림
 * - 헤더 "주의사항" 의 가드 서술 정정
 *
 * # 이유
 * - [Conditional("UNITY_EDITOR")] 는 적용부 IL 만 제거할 뿐, 타입 이름 해석은 컴파일 타임에
 *   여전히 필요하다. 클래스를 #if 로 감싸면 런타임 어셈블리의 사용처가 CS0246 으로 깨진다
 * - 실제로 PortraitPose.SpriteKey(HDialogue 런타임)가 [HSpritePreview] 를 쓰고 있어
 *   플레이어 빌드 전체가 실패했다. 에디터 컴파일은 UNITY_EDITOR 가 정의되어 통과하므로
 *   21일간 잠복
 * - 형제 attribute(HTitle / HButton / HShowInInspector)는 모두 클래스가 가드 밖이다.
 *   이 파일만 2026-05-13 에 헤더와 본체를 단일 블록으로 통합하며 어긋났다
 *
 * # 결과
 * - 런타임 어셈블리에 빈 attribute 클래스 1개가 잔류한다(float 프로퍼티 1개, 실행 코드 없음)
 * - 적용부는 [Conditional] 로 계속 제거되므로 빌드 IL 증가는 클래스 정의분뿐이다
 *
 * # 주의
 * - 이 파일의 가드를 다시 클래스까지 확장하지 말 것. 런타임 사용처가 있는 한 빌드가 깨진다
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 클래스 본체 #if UNITY_EDITOR 가드 적용
 *
 * # 변경
 * - 헤더 주석과 클래스 본체를 단일 #if UNITY_EDITOR 블록으로 통합
 *
 * # 이유
 * - [Conditional("UNITY_EDITOR")] 는 사용처(call site) IL 제거 — 클래스 정의는 런타임 빌드 잔류
 * - #if UNITY_EDITOR 가드로 클래스 자체를 런타임 바이너리에서 완전히 제거
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 HSpritePreviewAttribute 베이스 코드 생성
 *
 * # 목적
 * - HData.NPOI.Core.Editor 의 SpritePreviewAttribute 를 HInspector 패키지로 이전
 * - PropertyAttribute 기반 인라인 미리보기는 NPOI 도메인 종속이 아닌 범용 인스펙터 유틸리티
 *
 * # 구현 결정
 * - PropertyAttribute 직접 상속 유지 (HInspectorAttribute 파생 시 HInspectorPropertyDrawer
 *   의 Field-only 사전 검증 경로로 충돌 가능 — HTitleAttribute 와 동일 우회 패턴)
 * - [Conditional("UNITY_EDITOR")] : 빌드에서 사용처(call site) IL 추가 제거
 *
 * =============================================================================
 */
#endif
