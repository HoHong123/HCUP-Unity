# HAudio Changelog

## [1.0.0] - 2026-05-05

### 주요 변경

- HCUP.HGame 패키지에서 Audio/Sound 코드를 분리해 독립 어셈블리 (`HCUP.HAudio`, `HCUP.HAudio.Editor`, `HCUP.HAudio.Odin`) 로 구성했습니다.
- namespace 를 `HGame.Audio.*` / `HGame.Sound.*` 에서 `HAudio.*` 단일 트리로 평탄화했습니다.
- 폴더 구조를 `Runtime/New` (활성) 와 `Runtime/Legacy` (호환 partial + 구 SoundManager) 로 이분했습니다.
- HCUP.HGame 의 dead `Editor/`, `Odin/` asmdef 를 audio 분리 후 동시 정리했습니다.

### 마이그레이션 / 주의

- caller 측 `using HGame.Audio.*` / `using HGame.Sound.*` 를 `using HAudio.*` / `using HAudio.{Catalog,Repository,Core,AddOn,Load,Enum,Editor}` 로 일괄 교체하십시오.
- ScriptableObject asset (SoundCatalogSO 등) 의 `m_Script` 바인딩은 GUID 기반이라 영향이 없습니다.
- Samples~ 는 참고용이며 실제 배포 빌드에는 포함하지 않습니다.

### 검증 체크리스트

- Unity 컴파일 에러가 0 인지 확인하십시오.
- 외부 caller (Astar 씬의 SoundManager prefab 참조 등) 에서 broken reference (분홍색) 가 발생하지 않았는지 확인하십시오.
- legacy partial 의 namespace 가 활성 partial 과 동일한지 (예: AudioManager partial 모두 `HAudio` namespace) 확인하십시오.
