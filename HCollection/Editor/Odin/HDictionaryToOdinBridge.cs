#if ODIN_INSPECTOR
/* =========================================================
 * @Jason - PKH
 * HDictionary<TK, TV> 필드가 Odin 내장 Dictionary drawer 대신 Unity 의 [CustomPropertyDrawer(typeof(HDictionary<,>), true)]
 * 즉 HDictionaryDrawer 로 그려지도록 강제하는 브릿지입니다.
 *
 * 동작 ::
 * OdinAttributeProcessor 는 DefaultOdinAttributeProcessorLocator 에 의해 자동 수집됩니다.
 * 별도 [assembly: ...] 등록 코드 없이 클래스 정의만으로 활성화됩니다.
 * 프로퍼티 타입이 HDictionary<,> 의 인스턴스이면 [DrawWithUnity] 속성을 자동 주입하여 Odin 렌더를 우회합니다.
 *
 * 강제하는 이유 ::
 * - HDictionaryDrawer 는 중복 키 시 붉은 오버레이, Sort by Key 버튼, ReorderableList 행 한 줄 [Key | Value | X] 같은
 *   특수 시각을 제공합니다. Odin 의 generic Dictionary drawer 는 이 기능을 모르기 때문에 중복 키 감지
 *   (HDictionaryValidator 가 Play/Build/Save 차단하는 조건) 를 사용자에게 시각으로 보여주지 못합니다.
 * - HDictionary 는 List+Dict 싱크 + 빌드 메모리 최적화 + 중복 키 Validator 등 자체 계약이 있고,
 *   이 계약 전반을 이해하는 drawer 는 HDictionaryDrawer 뿐입니다.
 *
 * 컴파일 조건 ::
 * 어셈블리 HCUP.HCollection.Odin.Editor 는 defineConstraints 로 ODIN_INSPECTOR 를 요구합니다.
 * Odin 미설치 환경에서는 이 어셈블리 자체가 컴파일되지 않아 HCollection 본체에 영향이 없습니다.
 *
 * 중복 방지 ::
 * 동일한 [DrawWithUnity] 속성이 이미 붙어 있으면 재추가하지 않습니다.
 * 사용자가 필드에 직접 [DrawWithUnity] 를 달아 둔 경우에도 정상 동작합니다.
 * =========================================================
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HCollection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace HCollection.Odin.Editor {
    public sealed class HDictionaryToOdinBridge : OdinAttributeProcessor {
        public override bool CanProcessSelfAttributes(InspectorProperty property) {
            return _IsHDictionary(property.Info.TypeOfValue);
        }

        public override bool CanProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member) {
            return false;
        }

        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes) {
            if (attributes.OfType<DrawWithUnityAttribute>().Any()) return;
            attributes.Add(new DrawWithUnityAttribute());
        }

        private static bool _IsHDictionary(Type type) {
            if (type == null) return false;
            for (Type t = type; t != null; t = t.BaseType) {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(HDictionary<,>)) return true;
            }
            return false;
        }
    }
}
#endif
