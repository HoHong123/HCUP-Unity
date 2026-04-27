# HCollection 1.0.2 릴리즈 노트

----

어셈블리 링크 : https://github.com/HoHong123/HCUP-Unity.git?path=/HCollection#HCollection-1.0.2

## 개요
컬렉션 보조 자료구조 어셈블리입니다.
HDictionary(Unity 직렬화 가능 사전), IHDictionary, EnumArray, CircularList, CollectionUtil 을 제공합니다.
HDiagnosis 에 의존합니다.

## 1.0.2에서 변경된 점

----

- 1.0.1 에서 HUtil 로부터 어셈블리 분리 — HCUP.HCollection / HCUP.HCollection.Editor.
- 어셈블리 식별자 prefix 를 HCUP.* 로 통일했습니다 (이전: Hohong123.*).
- HCollection.Odin.Editor 어셈블리 신설 — HDictionaryToOdinBridge 가 Odin 환경에서 HDictionaryDrawer 를 강제하여 Odin 의 자동 dictionary drawer 를 우회합니다.
- HDictionary 사용자 정신 모델 적용 — 변경 API 와 OnBeforeSerialize 본문을 #if UNITY_EDITOR 가드로 감싸고, lazy 재할당 제거 + GetValueOrDefault 제거로 Runtime/Editor 책임을 명확히 분리.
- §11 헤더 형틀 정리 — HDictionary, IHDictionary, HDictionaryDrawer, HDictionaryValidator 4 클래스.

## 폴더 맵 (Runtime)

----

- Collection: HDictionary, IHDictionary, EnumArray, CircularList, CollectionUtil

## 폴더 맵 (Editor)

----

- Collection: HDictionaryDrawer, HDictionaryValidator
- Odin (선택 의존): HDictionaryToOdinBridge

## 주의사항

----

- HCollection 은 별도 UPM 패키지가 아닌 HCUP-Unity repo 안의 sibling 어셈블리입니다.
- HDictionary 의 변경 API 와 OnBeforeSerialize 는 Editor-only 가드되어 있어 Runtime 에서는 직렬화된 데이터를 읽기 전용으로 사용해야 합니다.
- HDictionaryDrawer (Editor) 는 자동으로 Odin Drawer 를 우회합니다 (HCollection.Odin.Editor 어셈블리, ODIN_INSPECTOR define gate).

Full Changelog: https://github.com/HoHong123/HCUP-Unity/compare/v1.0.0...HCollection-1.0.2
