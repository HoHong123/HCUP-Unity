# AddressableSequence 샘플

이 샘플은 `HUtil.AssetHandler` 기반 Addressables 로드 흐름을 테스트하기 위한 UGUI 샘플입니다.

`AddressableLoadSequenceTester`는 씬에 미리 배치된 UGUI 참조를 사용합니다.  
Address 단건 로드는 `AssetProvider`를 사용하고, Label 조회는 `AddressableLabelLoader`를 사용합니다.

현재 씬 UI는 버튼 3개만 사용합니다.
- 좌측 버튼: 모드 순환
- 중앙 버튼: 현재 모드 Load
- 우측 버튼: 현재 모드 Release

## 포함 파일

- `DemoAddressable.unity`
- `Scripts/AddressableLoadSequenceTester.cs`

## 테스트 가능한 항목

- Address 로드 / 릴리즈
- Label All 로드 / 릴리즈
- Label First 로드 / 릴리즈
- Label Single 로드 / 릴리즈
- Label Index 로드 / 릴리즈
- 자동 릴리즈 옵션
- 로그 출력 옵션
- 로드 기록 확인

## 사용 방법

1. `DemoAddressable.unity`를 엽니다.
2. Play Mode로 진입합니다.
3. 씬에 배치된 UGUI 테스트 패널에서 Address 또는 Label 값을 입력합니다.
4. 좌측 버튼으로 모드를 순환합니다.
5. 중앙 버튼으로 현재 모드의 Load를 실행합니다.
6. 우측 버튼으로 현재 모드의 Release를 실행합니다.

모드 순서는 다음과 같습니다.
- `Address`
- `Label All`
- `Label First`
- `Label Single`
- `Label Index`
- `Release All`

## 참고 사항

- 현재 샘플은 씬에 배치된 UGUI와 스크립트 참조 연결이 완료되어 있어야 합니다.
- 개별 레코드 Ping 버튼 대신, 로드 기록은 스크롤 텍스트로 요약 표시합니다.
- 실제 동작을 확인하려면 Addressables에 대응하는 주소나 라벨이 프로젝트에 등록되어 있어야 합니다.
