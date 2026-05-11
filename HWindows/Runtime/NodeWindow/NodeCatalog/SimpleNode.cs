namespace HWindows.NodeWindow {
    // 검증용 최소 concrete. 도메인 필드 없음.
    public sealed class SimpleNode : BaseNode {
        // 도메인별 클립보드 식별자 — Cut/Copy 의 wrapper magic.
        public override string ClipboardMagic => "HGRAPH_SIMPLE_NODE_V1";
    }
}
