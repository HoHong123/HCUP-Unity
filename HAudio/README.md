# HAudio 시스템 요약

----

`HAudio` 는 오디오 도메인 패키지입니다.

token 기반 `AudioManager` 와 카탈로그/리포지토리 계층을 제공하며, 마이그레이션 중인 레거시 `SoundManager` API 를 partial 로 함께 보존합니다.

## 1.0.0 신규

- HCUP.HGame 의 Audio/Sound 코드를 분리해 독립 어셈블리로 구성했습니다.
- namespace 를 `HGame.Audio.*` / `HGame.Sound.*` 에서 `HAudio.*` 단일 트리로 평탄화했습니다.
- 폴더는 `Runtime/New` (활성) 와 `Runtime/Legacy` (마이그레이션 호환) 로 이분해 cut line 을 명시했습니다.

## 디렉토리 구성

### `Runtime/New`

- 활성 코드 영역입니다.
- token 기반 `AudioManager`, `AudioClipRepository`, `AudioCatalogRegistry`, AddOn, Load, Core, Enum.

### `Runtime/Legacy`

- 마이그레이션 호환 영역입니다.
- 구 `SoundManager` 클래스와 신규 클래스의 legacy partial 들이 모여 있습니다.

### `Editor`

- 카탈로그 편집 / 생성 / 미리보기 / 디버그 윈도우.

### `Samples~`

- token 기반 재생 샘플.

## 추천 확인 순서

1. `Runtime/New/AudioManager.cs`
2. `Runtime/New/Repository/AudioClipRepository.cs`
3. `Runtime/New/Catalog/AudioCatalogRegistry.cs`
4. `Editor/SoundCatalogEditorWindow.cs`
5. `Samples~/Sound`

## 기술 전제

- Unity 2021.3+
- UniTask
- `HCUP.HUtil`, `HCUP.HUI`, `HCUP.HCore`, `HCUP.HInspector`, `HCUP.HDiagnosis` 패키지 의존

## UPM 설치 경로 (HCUP-Unity)

- `https://github.com/HoHong123/HCUP-Unity.git?path=/HoHong123/HAudio`
- `https://github.com/HoHong123/HCUP-Unity.git?path=/HoHong123/HAudio#v{목표버전}`
