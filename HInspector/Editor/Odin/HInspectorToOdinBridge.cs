#if ODIN_INSPECTOR
/* =========================================================
 * @Jason - PKH
 * HInspector 속성을 Odin 파이프라인이 이해하도록 매핑하는 브릿지입니다.
 *
 * 동작 ::
 * OdinAttributeProcessor는 DefaultOdinAttributeProcessorLocator에 의해 자동 수집됩니다.
 * 별도 [assembly: ...] 등록 코드 없이 클래스 정의만으로 활성화됩니다.
 * 속성 수집 단계에서 HInspector 속성이 발견되면 동등한 Odin 속성을 List<Attribute>에
 * 추가하여 Odin 렌더러가 자연스럽게 그리도록 합니다.
 *
 * 컴파일 조건 ::
 * 어셈블리 HCUP.HInspector.Odin.Editor는 defineConstraints로 ODIN_INSPECTOR를 요구합니다.
 * Odin 미설치 환경에서는 이 어셈블리 자체가 컴파일되지 않아 본체 HInspector에 영향이 없습니다.
 *
 * 중복 방지 ::
 * 동일한 Odin 속성이 이미 붙어 있으면 재추가하지 않습니다. 점진 마이그레이션 중
 * HTitle과 Title이 한 필드에 함께 있어도 이중 렌더가 발생하지 않습니다.
 * =========================================================
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace HInspector.Odin.Editor {
    public sealed class HInspectorToOdinBridge : OdinAttributeProcessor {
        public override bool CanProcessSelfAttributes(InspectorProperty property) => true;

        public override bool CanProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member) => true;

        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes) {
            _MapAll(attributes);
        }

        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes) {
            _MapAll(attributes);
        }

        private static void _MapAll(List<Attribute> attributes) {
            _MapHTitle(attributes);
            _MapHOnValueChanged(attributes);
            _MapHHideIf(attributes);  // HHideIf가 HShowIf 파생이라 먼저 처리해 OfType<HShowIf>에서 중복 잡힘을 피한다.
            _MapHShowIf(attributes);
            _MapHEnableIf(attributes);
            _MapHReadOnly(attributes);
            _MapHRequired(attributes);
            _MapHLabelText(attributes);
            _MapHHideLabel(attributes);
            _MapHMin(attributes);
            _MapHMax(attributes);
            _MapHMinMaxSlider(attributes);
            _MapHBoxGroup(attributes);
            _MapHHorizontalGroup(attributes);
            _MapHVerticalGroup(attributes);
            _MapHButton(attributes);
            _MapHShowInInspector(attributes);
            _MapHListDrawer(attributes);
        }

        private static void _MapHTitle(List<Attribute> attributes) {
            // 이미 Odin [Title]이 붙어 있으면 중복 추가 금지. 점진 마이그레이션 혼재 상태를 안전하게 처리한다.
            if (attributes.OfType<TitleAttribute>().Any()) return;

            HTitleAttribute hTitle = attributes.OfType<HTitleAttribute>().FirstOrDefault();
            if (hTitle == null) return;

            attributes.Add(new TitleAttribute(hTitle.Title));
        }

        private static void _MapHOnValueChanged(List<Attribute> attributes) {
            // Odin OnValueChanged가 이미 있으면 중복 호출 방지.
            if (attributes.OfType<OnValueChangedAttribute>().Any()) return;

            HOnValueChangedAttribute hAttr = attributes.OfType<HOnValueChangedAttribute>().FirstOrDefault();
            if (hAttr == null) return;
            if (string.IsNullOrEmpty(hAttr.MethodName)) return;

            attributes.Add(new OnValueChangedAttribute(hAttr.MethodName, hAttr.IncludeChildren));
        }

        private static void _MapHShowIf(List<Attribute> attributes) {
            if (attributes.OfType<ShowIfAttribute>().Any()) return;

            // HHideIf는 HShowIf 파생이지만 별도 매핑되므로 여기서는 제외한다.
            HShowIfAttribute h = attributes.OfType<HShowIfAttribute>().FirstOrDefault(a => !(a is HHideIfAttribute));
            if (h == null) return;

            if (h.IsExpression) {
                attributes.Add(new ShowIfAttribute(h.Expression));
                return;
            }
            if (string.IsNullOrEmpty(h.MemberName)) return;

            if (h.HasCompareValue) {
                attributes.Add(new ShowIfAttribute(h.MemberName, h.CompareValue));
            }
            else {
                attributes.Add(new ShowIfAttribute(h.MemberName));
            }
        }

        private static void _MapHHideIf(List<Attribute> attributes) {
            if (attributes.OfType<HideIfAttribute>().Any()) return;

            HHideIfAttribute h = attributes.OfType<HHideIfAttribute>().FirstOrDefault();
            if (h == null) return;

            if (h.IsExpression) {
                attributes.Add(new HideIfAttribute(h.Expression));
                return;
            }
            if (string.IsNullOrEmpty(h.MemberName)) return;

            if (h.HasCompareValue) {
                attributes.Add(new HideIfAttribute(h.MemberName, h.CompareValue));
            }
            else {
                attributes.Add(new HideIfAttribute(h.MemberName));
            }
        }

        private static void _MapHEnableIf(List<Attribute> attributes) {
            if (attributes.OfType<EnableIfAttribute>().Any()) return;

            HEnableIfAttribute h = attributes.OfType<HEnableIfAttribute>().FirstOrDefault();
            if (h == null) return;

            string condition = h.IsExpression ? h.Expression : h.Condition;
            if (string.IsNullOrEmpty(condition)) return;

            attributes.Add(new EnableIfAttribute(condition));
        }

        private static void _MapHReadOnly(List<Attribute> attributes) {
            // 조건 없는 HReadOnly는 Odin ReadOnly, 조건 있는 쪽은 DisableIf/EnableIf로 논리 변환한다.
            HReadOnlyAttribute h = attributes.OfType<HReadOnlyAttribute>().FirstOrDefault();
            if (h == null) return;

            if (string.IsNullOrEmpty(h.ConditionMemberName)) {
                if (attributes.OfType<ReadOnlyAttribute>().Any()) return;
                attributes.Add(new ReadOnlyAttribute());
                return;
            }

            // Inverse = true: condition이 false일 때 ReadOnly → EnableIf(condition)과 동치
            // Inverse = false: condition이 true일 때 ReadOnly → DisableIf(condition)과 동치
            if (h.Inverse) {
                if (attributes.OfType<EnableIfAttribute>().Any()) return;
                attributes.Add(new EnableIfAttribute(h.ConditionMemberName));
            }
            else {
                if (attributes.OfType<DisableIfAttribute>().Any()) return;
                attributes.Add(new DisableIfAttribute(h.ConditionMemberName));
            }
        }

        private static void _MapHRequired(List<Attribute> attributes) {
            if (attributes.OfType<RequiredAttribute>().Any()) return;

            HRequiredAttribute h = attributes.OfType<HRequiredAttribute>().FirstOrDefault();
            if (h == null) return;

            if (string.IsNullOrEmpty(h.Message)) {
                attributes.Add(new RequiredAttribute());
            }
            else {
                attributes.Add(new RequiredAttribute(h.Message));
            }
        }

        private static void _MapHLabelText(List<Attribute> attributes) {
            if (attributes.OfType<LabelTextAttribute>().Any()) return;

            HLabelTextAttribute h = attributes.OfType<HLabelTextAttribute>().FirstOrDefault();
            if (h == null) return;
            if (string.IsNullOrEmpty(h.Text)) return;

            attributes.Add(new LabelTextAttribute(h.Text));
        }

        private static void _MapHHideLabel(List<Attribute> attributes) {
            if (attributes.OfType<HideLabelAttribute>().Any()) return;

            HHideLabelAttribute h = attributes.OfType<HHideLabelAttribute>().FirstOrDefault();
            if (h == null) return;

            attributes.Add(new HideLabelAttribute());
        }

        private static void _MapHMin(List<Attribute> attributes) {
            if (attributes.OfType<MinValueAttribute>().Any()) return;

            HMinAttribute h = attributes.OfType<HMinAttribute>().FirstOrDefault();
            if (h == null) return;

            attributes.Add(new MinValueAttribute(h.Min));
        }

        private static void _MapHMax(List<Attribute> attributes) {
            if (attributes.OfType<MaxValueAttribute>().Any()) return;

            HMaxAttribute h = attributes.OfType<HMaxAttribute>().FirstOrDefault();
            if (h == null) return;

            attributes.Add(new MaxValueAttribute(h.Max));
        }

        private static void _MapHMinMaxSlider(List<Attribute> attributes) {
            if (attributes.OfType<MinMaxSliderAttribute>().Any()) return;

            HMinMaxSliderAttribute h = attributes.OfType<HMinMaxSliderAttribute>().FirstOrDefault();
            if (h == null) return;

            // showFields = true로 두면 Odin도 min/max 편집 필드를 함께 노출해 HInspector 동작과 일치한다.
            attributes.Add(new MinMaxSliderAttribute(h.Min, h.Max, true));
        }

        private static void _MapHBoxGroup(List<Attribute> attributes) {
            if (attributes.OfType<BoxGroupAttribute>().Any()) return;

            HBoxGroupAttribute h = attributes.OfType<HBoxGroupAttribute>().FirstOrDefault();
            if (h == null) return;
            if (string.IsNullOrEmpty(h.GroupName)) return;

            attributes.Add(new BoxGroupAttribute(h.GroupName));
        }

        private static void _MapHHorizontalGroup(List<Attribute> attributes) {
            if (attributes.OfType<HorizontalGroupAttribute>().Any()) return;

            HHorizontalGroupAttribute h = attributes.OfType<HHorizontalGroupAttribute>().FirstOrDefault();
            if (h == null) return;
            if (string.IsNullOrEmpty(h.GroupName)) return;

            attributes.Add(new HorizontalGroupAttribute(h.GroupName));
        }

        private static void _MapHVerticalGroup(List<Attribute> attributes) {
            if (attributes.OfType<VerticalGroupAttribute>().Any()) return;

            HVerticalGroupAttribute h = attributes.OfType<HVerticalGroupAttribute>().FirstOrDefault();
            if (h == null) return;
            if (string.IsNullOrEmpty(h.GroupName)) return;

            attributes.Add(new VerticalGroupAttribute(h.GroupName));
        }

        private static void _MapHButton(List<Attribute> attributes) {
            if (attributes.OfType<ButtonAttribute>().Any()) return;

            HButtonAttribute h = attributes.OfType<HButtonAttribute>().FirstOrDefault();
            if (h == null) return;

            if (string.IsNullOrEmpty(h.Label)) {
                attributes.Add(new ButtonAttribute());
            }
            else {
                attributes.Add(new ButtonAttribute(h.Label));
            }
        }

        private static void _MapHShowInInspector(List<Attribute> attributes) {
            HShowInInspectorAttribute h = attributes.OfType<HShowInInspectorAttribute>().FirstOrDefault();
            if (h == null) return;

            if (!attributes.OfType<ShowInInspectorAttribute>().Any()) {
                attributes.Add(new ShowInInspectorAttribute());
            }

            // HShowInInspector의 Label 파라미터는 Odin LabelText로 별도 전달한다.
            if (!string.IsNullOrEmpty(h.Label) && !attributes.OfType<LabelTextAttribute>().Any()) {
                attributes.Add(new LabelTextAttribute(h.Label));
            }
        }

        private static void _MapHListDrawer(List<Attribute> attributes) {
            if (attributes.OfType<ListDrawerSettingsAttribute>().Any()) return;

            HListDrawerAttribute h = attributes.OfType<HListDrawerAttribute>().FirstOrDefault();
            if (h == null) return;

            ListDrawerSettingsAttribute odinAttr = new ListDrawerSettingsAttribute {
                DefaultExpandedState = h.DefaultExpandedState,
                IsReadOnly = h.IsReadOnly
            };

            // Phase 2 옵션은 HListDrawer에서 [Obsolete] 표식이 붙어 있어 경고를 발생시킨다.
            // 어댑터는 이 필드들의 합법적 소비자이므로 범위를 한정해 경고를 억제한다.
#pragma warning disable 0612, 0618
            odinAttr.DraggableItems = h.DraggableItems;
            odinAttr.ShowIndexLabels = h.ShowIndexLabels;
            odinAttr.HideAddButton = h.HideAddButton;
            odinAttr.HideRemoveButton = h.HideRemoveButton;
            odinAttr.NumberOfItemsPerPage = h.NumberOfItemsPerPage;
#pragma warning restore 0612, 0618

            attributes.Add(odinAttr);
        }
    }
}
#endif
