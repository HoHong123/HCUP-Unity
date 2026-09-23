#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 벤치마크 측정 한 회의 결과입니다.
 *
 * 특징 / 지원기능 ::
 * 시나리오 이름, 규모(오브젝트 수 또는 key 수), 리소스 크기, 반복 순번과 측정값을 담습니다.
 * + IssueMs : 요청을 거는 동기 구간. 그 프레임에 HResource 가 더하는 비용입니다
 * + IssueGcBytesPerUnit : 이슈 프레임의 GC 할당을 요청 수로 나눈 값입니다
 * + TeardownMs : 파괴 또는 일괄 반납의 동기 구간입니다
 *
 * 주의사항 ::
 * 측정하지 않은 값은 NaN 입니다. 보고서는 NaN 을 "-" 로 씁니다.
 * =========================================================
 */
#endif

namespace HResource.Benchmark {
    public sealed class HResourceBenchmarkSample {
        #region Fields
        public string Scenario;
        public int Count;
        public int PayloadKB;
        public int RepeatIndex;
        public double IssueMs = double.NaN;
        public double CompleteMs = double.NaN;
        public int Frames;
        public double MaxFrameMs = double.NaN;
        public double IssueFrameGcKB = double.NaN;
        public double IssueGcBytesPerUnit = double.NaN;
        public double TeardownMs = double.NaN;
        public double ManagedBytesPerOwner = double.NaN;
        public double NativeBytesPerOwner = double.NaN;
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-23 (최초 설계) :: 측정 결과 한 행
 *
 * 변경 ::
 * 시나리오마다 쓰는 값이 달라 한 행에 모든 지표를 두고, 쓰지 않는 값은 NaN 으로 남겼다.
 *
 * 이유 ::
 * CSV 한 장으로 모든 시나리오를 비교하려면 열이 같아야 한다.
 * =========================================================
 */
#endif
