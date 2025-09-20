namespace Util.OdinCompat {
// Title vs Header  (한 줄로 통일: [HeaderOrTitle("Target")])
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;

    public sealed class HeaderOrTitleAttribute : TitleAttribute {
        public HeaderOrTitleAttribute(string title) : base(title) { }
        public HeaderOrTitleAttribute(
            string title,
            string subtitle = null,
            TitleAlignments alignments = TitleAlignments.Left,
            bool horizontalLine = true,
            bool bold = true
        ) : base(title, subtitle, alignments, horizontalLine, bold) { }
    }
#else
    using System;

    // Odin 없을 때는 동일한 이름의 더미 속성을 제공(효과 없음)
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public sealed class HeaderOrTitleAttribute : UnityEngine.HeaderAttribute {
        public HeaderOrTitleAttribute(string title) : base(title) { }
        public HeaderOrTitleAttribute(
            string title,
            string subtitle = null,
            int alignments = 0,
            bool horizontalLine = true,
            bool bold = true
        ) : base(title) {}
    }
#endif
    // 필요하면 계속 추가: e.g. Foldout, InlineEditor, ReadOnly 등
    // 아래 패턴을 복붙해서 확장하면 된다.
}
