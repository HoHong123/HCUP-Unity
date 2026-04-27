#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * DataOwnerIdGenerator는 데이터 로드/캐시 시스템에서 사용되는 "Owner 식별자"를
 * 생성하는 전역 발급기 클래스입니다.
 *
 * 주의사항 ::
 * 1. 발급된 ID는 해당 owner의 생명주기 동안 유지해야 합니다.
 * 2. 매 호출마다 새로 발급하면 Dependency 관리가 깨집니다.
 * 3. 반드시 Load/Release에서 동일한 ownerId를 사용해야 합니다.
 *
 * 권장 패턴 ::
 * - Awake에서 1회 발급
 * - OnDestroy에서 ReleaseOwner 호출
 * =========================================================
 */
#endif

using System.Threading;
using HUtil.Data.Subscription;

public static class DataOwnerIdGenerator {
    #region Fields
    static int nextId = 0;
    #endregion

    #region Events
    public static event System.Action<DataOwnerId, object> OnIdCreated;
    public static event System.Action<DataOwnerId> OnIdReleased;
    #endregion

    #region Public - Generate
    /// 고유한 Owner ID를 생성
    /// - 1부터 시작하는 증가 값 반환
    /// - 0은 유효하지 않은 값으로 예약됨
    /// - Thread-safe 보장
    public static DataOwnerId NewId(object owner = null) {
        DataOwnerId ownerId = Interlocked.Increment(ref nextId);
        OnIdCreated?.Invoke(ownerId, owner);
        return ownerId;
    }

    public static void NotifyReleased(DataOwnerId ownerId) {
        if (!ownerId.IsValid) return;
        OnIdReleased?.Invoke(ownerId);
    }
    #endregion
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Owner 식별자 생성
 * 2. 전역 단일 증가값 기반 ID 발급
 * 3. 멀티스레드 안전 보장
 * 3-1. Interlocked.Increment를 사용하여 멀티스레드 환경에서도 안전하게 증가합니다.
 * 3-2. 동시에 여러 요청이 들어와도 중복 ID가 발생하지 않습니다.
 *
 * 사용법 ::
 * 1. 객체 생성 시 1회 발급
 *    DataOwnerId ownerId = DataOwnerIdGenerator.NewId();
 *
 * 2. Load 시 전달
 *    await manager.GetOrLoadAsync(id, ownerId);
 *
 * 3. Release 시 동일 ID 사용
 *    manager.ReleaseId(id, ownerId);
 *
 * 4. 객체 종료 시 일괄 해제
 *    manager.ReleaseOwner(ownerId);
 *
 * 주의사항 ::
 * 1. 매 프레임 NewId 호출 금지
 * 2. 임시 변수로 발급 후 재사용하지 않으면 memory leak 발생
 * 3. ownerId는 반드시 "소유권 단위" 기준으로 관리해야 함
 *
 * 기타 ::
 * 1. 기존 object owner 방식 대비 GC 및 Unity lifecycle 의존성 제거
 * 2. BaseDataCache와 결합되어 Dependency 기반 자동 제거 구조를 형성함
 * =========================================================
 */
#endif