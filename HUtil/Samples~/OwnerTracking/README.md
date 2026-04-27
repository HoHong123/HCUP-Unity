# Addressable Owner Tracking Sample

## 먼저 짚고 넘어갈 점
이 샘플은 **실제 Unity Addressables API 샘플이 아닙니다.** 현재 `HUtil.Data.Sequence.AddressableLoadSequence<TData>` 는 아직 `TODO` 상태이므로, 이 예제는 `DataLoadType.Addressable` 과 `AssetProvider<TAsset>` + `BaseDataCache<TKey, TData>` 의 **Owner 추적 규칙**을 검증하는 목적의 샘플입니다.

## 포함 파일
- `Scripts/AddressableOwnerTrackingSample.cs`

## 샘플이 보여주는 것
- 같은 Owner가 같은 Key를 여러 번 로드해도 `Dependency` 는 중복 증가하지 않습니다.
- 다른 Owner가 같은 Key를 로드하면 `Dependency` 가 증가합니다.
- 익명 로드(`GetOrLoadAsync(key)`)는 `AnonymousDependency` 와 전체 `Dependency` 를 증가시킵니다.
- `ReleaseOwner(owner)` 는 해당 Owner가 점유한 Key를 일괄 해제합니다.
- 익명 의존성은 `ReleaseId(key)` 로 해제해야 합니다.

## 사용 방법
1. 빈 GameObject를 만든 뒤 `AddressableOwnerTrackingSample` 컴포넌트를 붙입니다.
2. `Entries` 에 Key/Sprite 쌍을 하나 이상 등록합니다.
3. `Sample Key` 를 등록한 Key와 동일하게 맞춥니다.
4. `Owner A`, `Owner B` 에 서로 다른 Unity Object를 넣습니다.
   - 비워두면 둘 다 `this` 로 대체되므로 Owner 분리 검증이 되지 않습니다.
5. 아래 중 하나로 실행합니다.
   - `Run Scenario On Start` 활성화 후 Play.
   - 컴포넌트 우클릭 Context Menu에서 `Run Owner Tracking Scenario` 실행.

## 기대 로그 순서
기본 시나리오는 아래 순서입니다.
1. Owner A Load
2. Owner A Load (중복 Owner, Dependency 증가 없음)
3. Owner B Load
4. Anonymous Load
5. Owner A ReleaseOwner
6. Anonymous ReleaseId
7. Owner B ReleaseOwner

Unity Editor에서는 각 단계마다 다음 값이 로그로 출력됩니다.
- `Dependency`
- `OwnerCount`
- `Asset`

## 권장 검증 포인트
- 2번째 `Owner A Load` 이후에도 `Dependency` 가 그대로인지 확인합니다.
- `Owner A ReleaseOwner` 후에도 `Owner B` 와 익명 의존성 때문에 캐시가 유지되는지 확인합니다.
- 마지막 `Owner B ReleaseOwner` 이후 캐시가 제거되는지 확인합니다.
